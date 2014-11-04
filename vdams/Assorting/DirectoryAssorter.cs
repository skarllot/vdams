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

using vdams.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace vdams.Assorting
{
    class DirectoryAssorter
    {
        const string DEFAULT_FILELIST_DATE_FORMAT = "yyyy-MM-dd";
        const string DEFAULT_FILELIST_NAME = "{0}.txt";
        Configuration.ConfigPathSection cfgPath;
        int depth;

        public DirectoryAssorter(Configuration.ConfigPathSection cfgPath, int depth)
        {
            this.cfgPath = cfgPath;
            this.depth = depth;
        }

        public static AssortTransaction BeginTransaction(string path, int depth)
        {
            int counter = 1;
            while (counter <= depth) {
                DateTime dt = DateTime.Today.AddDays(-1 * counter);
                string fileName = Path.Combine(path, string.Format(DEFAULT_FILELIST_NAME,
                    dt.ToString(DEFAULT_FILELIST_DATE_FORMAT)));
                if (File.Exists(fileName))
                    File.Delete(fileName);

                counter++;
            }

            return new AssortTransaction(path);
        }

        public bool Assort(AssortTransaction transaction)
        {
            if (transaction == null)
                throw new ArgumentNullException("transaction");

            lock (transaction.Locker) {
                if (!transaction.IsRunning)
                    throw new InvalidOperationException("No assorting transaction is running");

                if (!cfgPath.IsValid()) {
                    MainClass.Logger.WriteEntry(string.Format("The path \"{0}\" becomes invalid", cfgPath.SectionName),
                                System.Diagnostics.EventLogEntryType.Error, EventId.AssortPathValidationError);
                    return false;
                }

                var logTransaction = MainClass.Logger.BeginWriteEntry();
                logTransaction.AppendLine(string.Format("Initializing assorting to {0}", cfgPath.SectionName));

                IList<string> fileList = GetFileList(cfgPath.SourcePath);
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

                    logTransaction.AppendLine(string.Format("Writing file list of files modified on {0}", dt.ToShortDateString()));
                    string fileName = Path.Combine(transaction.Path,
                        string.Format(DEFAULT_FILELIST_NAME,
                        dt.ToString(DEFAULT_FILELIST_DATE_FORMAT)));

                    StreamWriter writer = new StreamWriter(fileName, true, System.Text.Encoding.UTF8);
                    foreach (string item in pickedList)
                        writer.WriteLine(item);
                    writer.Flush();
                    writer.Close();
                    writer.Dispose();

                    logTransaction.AppendLine(string.Format("Assort of {0} ended", dt.ToShortDateString()));
                    counter++;
                }
                fileList.Clear();
                fileList = null;

                logTransaction.Commit(new SklLib.Diagnostics.LogEventArgs(
                    System.Diagnostics.EventLogEntryType.Information, EventId.AssortCompleted));
                return true;
            }
        }

        public static void EndTransaction(AssortTransaction transaction)
        {
            if (transaction == null)
                throw new ArgumentNullException("transaction");

            transaction.Terminate();
        }

        private static IList<FileInfo> GetFileInfoFromList(IList<string> fileList)
        {
            List<FileInfo> fileInfoList = new List<FileInfo>(fileList.Count);

            foreach (string item in fileList) {
                fileInfoList.Add(new FileInfo(item));
            }
            return fileInfoList;
        }

        private static IList<string> GetFileList(string path)
        {
            List<string> fileList = new List<string>();
            Action<string, IList<string>> recursiveGetFileList = null;

            recursiveGetFileList = delegate(string p, IList<string> files) {
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
