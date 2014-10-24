// DirectoryAssorter.cs
//
// Copyright (C) 2014 Fabrício Godoy
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using cftv_bkp_prep.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace cftv_bkp_prep
{
    class DirectoryAssorter
    {
        const string DEFAULT_DIRECTORY_NAME = "yyyy-MM-dd";
        IO.ConfigPathItem cfgPath;
        int depth;

        public DirectoryAssorter(IO.ConfigPathItem cfgPath, int depth)
        {
            this.cfgPath = cfgPath;
            this.depth = depth;
        }

        public bool Assort()
        {
            if (!cfgPath.IsValid()) {
                MainClass.Logger.WriteEntry(string.Format("The path \"{0}\" becomes invalid", cfgPath.SectionName),
                            System.Diagnostics.EventLogEntryType.Error, EventId.AssortPathValidationError);
                return false;
            }

            DirectoryInfo dirTarget = new DirectoryInfo(cfgPath.TargetFullPath);
            foreach (DirectoryInfo item in dirTarget.GetDirectories("*", SearchOption.TopDirectoryOnly)) {
                try { item.Delete(true); }
                catch (IOException ex) {
                    string fileName = ex.GetIoExceptionFilePath();
                    if (fileName != null) {
                        fileName = item.GetFile(fileName).FullName;
                        MainClass.Logger.WriteEntry(string.Format("The file \"{0}\" cannot be deleted", fileName),
                            System.Diagnostics.EventLogEntryType.Warning, EventId.AssortFileAccessError);
                    }
                    else
                        throw ex;
                }
            }
            foreach (FileInfo item in dirTarget.GetFiles("*", SearchOption.TopDirectoryOnly)) {
                try { item.Delete(); }
                catch (IOException ex) {
                    string fileName = ex.GetIoExceptionFilePath();
                    if (fileName != null) {
                        fileName = item.FullName;
                        MainClass.Logger.WriteEntry(string.Format("The file \"{0}\" cannot be deleted", fileName),
                            System.Diagnostics.EventLogEntryType.Warning, EventId.AssortFileAccessError);
                    }
                    else
                        throw ex;
                }
            }

            var logTransaction = MainClass.Logger.BeginWriteEntry();
            logTransaction.AppendLine(string.Format("Initializing assorting to {0}", cfgPath.SectionName));

            List<string> fileList = (List<string>)GetFileList(cfgPath.SourceFullPath);
            logTransaction.AppendLine(string.Format("Found {0} total files", fileList.Count));

            // If a file date format was not defined into configuration then assort files based on modification date;
            // If not, assort based on file name.
            bool isFileDateFormatDefined = !string.IsNullOrWhiteSpace(cfgPath.FileDateFormat);

            int counter = 1;
            while (counter <= depth) {
                DateTime dt = DateTime.Today.AddDays(-1 * counter);
                List<string> pickedList = new List<string>();
                long totalBytes = 0;
                string strDt = dt.ToString(cfgPath.FileDateFormat);

                logTransaction.AppendLine(string.Format("Looking for file modified on {0}", dt.ToShortDateString()));
                foreach (string item in fileList) {
                    if (isFileDateFormatDefined) {
                        if (item.IndexOf(strDt) != -1) {
                            pickedList.Add(item);
                            totalBytes += new FileInfo(item).Length;
                        }
                    }
                    else {
                        FileInfo fInfo = new FileInfo(item);
                        if (fInfo.LastWriteTime.Date == dt.Date) {
                            pickedList.Add(item);
                            totalBytes += fInfo.Length;
                        }
                    }
                }

                logTransaction.AppendLine(string.Format("Found {0} files, total size of {1}",
                    pickedList.Count, new SklLib.DataSize(totalBytes).ToString("N2")));

                logTransaction.AppendLine(string.Format("Initializing assorting for date {0}", dt.ToShortDateString()));
                string dirName = Path.Combine(cfgPath.TargetFullPath, dt.ToString(DEFAULT_DIRECTORY_NAME));
                if (!Directory.Exists(dirName))
                    Directory.CreateDirectory(dirName);

                foreach (string item in pickedList) {
                    string innerDir = Path.GetDirectoryName(item).Remove(0, cfgPath.SourceFullPath.Length);
                    if (innerDir.Length > 0 && innerDir[0] == Path.DirectorySeparatorChar)
                        innerDir = innerDir.Remove(0, 1);

                    string targetDir = Path.Combine(dirName, innerDir);
                    string targetFile = Path.Combine(targetDir, Path.GetFileName(item));

                    if (!Directory.Exists(targetDir))
                        Directory.CreateDirectory(targetDir);
                    if (!File.Exists(targetFile)) {
                        if (!CreateHardLink(targetFile, item, IntPtr.Zero)) {
                            System.ComponentModel.Win32Exception ex = new System.ComponentModel.Win32Exception(
                                System.Runtime.InteropServices.Marshal.GetLastWin32Error());
                            MainClass.Logger.WriteEntry(
                                string.Format("Could not create a hard link from \"{0}\" to \"{1}\"\n\nError: {2}", item, targetFile, ex.Message),
                                System.Diagnostics.EventLogEntryType.Error, EventId.AssortErrorCreateHardLink);
                        }
                    }
                }
                logTransaction.AppendLine(string.Format("Assort of {0} ended", dt.ToShortDateString()));
                counter++;
            }
            fileList.Clear();
            fileList = null;

            logTransaction.Commit(new SklLib.Diagnostics.LogEventArgs(
                System.Diagnostics.EventLogEntryType.Information, EventId.AssortCompleted));
            return true;
        }

        private IList<FileInfo> GetFileInfoFromList(IList<string> fileList)
        {
            List<FileInfo> fileInfoList = new List<FileInfo>(fileList.Count);

            foreach (string item in fileList) {
                fileInfoList.Add(new FileInfo(item));
            }
            return fileInfoList;
        }

        private IList<string> GetFileList(string path)
        {
            List<string> fileList = new List<string>();
            Action<string, IList<string>> recursiveGetFileList = null;

            recursiveGetFileList = delegate(string p, IList<string> files) {
                // Avoid target path listing case it is a subdirectory from source path.
                if (cfgPath.TargetFullPath == p)
                    return;

                try {
                    Directory.GetFiles(p)
                        .ToList()
                        .ForEach(s => files.Add(s));

                    Directory.GetDirectories(p)
                        .ToList()
                        .ForEach(s => recursiveGetFileList(s, files));
                }
                catch (UnauthorizedAccessException) { }
            };

            recursiveGetFileList(path, fileList);
            return fileList;
        }

        [System.Runtime.InteropServices.DllImport("Kernel32.dll",
            CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);
    }
}
