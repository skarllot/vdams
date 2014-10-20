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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace cftv_bkp_prep
{
    class DirectoryAssorter
    {
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

            var logTransaction = MainClass.Logger.BeginWriteEntry();
            logTransaction.AppendLine(string.Format("Initializing assorting to {0}", cfgPath.SectionName));

            List<string> nodesSource = new List<string>();
            GetFileList(cfgPath.SourceFullPath, nodesSource);
            logTransaction.AppendLine(string.Format("Found {0} total files", nodesSource.Count));
            /*DirectoryInfo dirSource = new DirectoryInfo(cfgPath.SourceFullPath);
            List<FileSystemInfo> nodesSource = new List<FileSystemInfo>();
            foreach (var item in dirSource.EnumerateFileSystemInfos("*", SearchOption.AllDirectories)) {
                try {
                    if ((item.Attributes & FileAttributes.Directory) != 0)
                        continue;
                    if (item.FullName.IndexOf(cfgPath.TargetFullPath) == 0)
                        continue;

                    nodesSource.Add(item);
                }
                catch (UnauthorizedAccessException) { }
            }
            //FileSystemInfo[] nodesSource = dirSource.GetFileSystemInfos("*", SearchOption.AllDirectories);
            int counter = 1;

            while (counter <= depth) {
                DateTime dt = DateTime.Today.AddDays(-1 * counter);
                List<FileSystemInfo> pickedList = new List<FileSystemInfo>();
                long totalBytes = 0;

                foreach (FileSystemInfo item in nodesSource) {
                    if ((item.Attributes & FileAttributes.Directory) != 0)
                        continue;

                    if (item.LastWriteTime.Date == dt.Date) {
                        pickedList.Add(item);
                        totalBytes += new FileInfo(item.FullName).Length;
                    }
                }

                MainClass.Logger.WriteEntry(string.Format("[{0}] Found {1} files from date {2} and size {3}",
                    cfgPath.SectionName, pickedList.Count, dt.ToShortDateString(), new SklLib.DataSize(totalBytes).ToString("N2")),
                    System.Diagnostics.EventLogEntryType.Information, EventId.AssortFoundFiles);

                counter++;
            }*/

            return true;
        }

        private void GetFileList(string path, IList<string> files)
        {
            // Avoid target path listing case it is a subdirectory from source path.
            if (cfgPath.TargetFullPath == path)
                return;

            try {
                Directory.GetFiles(path)
                    .ToList()
                    .ForEach(s => files.Add(s));

                Directory.GetDirectories(path)
                    .ToList()
                    .ForEach(s => GetFileList(s, files));
            }
            catch (UnauthorizedAccessException) {
                var tst = 0;
            }
        }

        [System.Runtime.InteropServices.DllImport("Kernel32.dll",
            CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);
    }
}
