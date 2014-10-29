// DelayedFileDelete.cs
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
using System.Runtime.InteropServices;
using System.Threading;

namespace vdams.IO
{
    class DelayedFileDelete
    {
        const int DEFAULT_RETRY_INTERVAL = 5 * 60 * 1000;
        static readonly DelayedFileDelete _default = new DelayedFileDelete();

        Thread thWatcher;
        int retryInterval = DEFAULT_RETRY_INTERVAL;
        List<string> fileList = new List<string>();
        List<string> dirList = new List<string>();
        ManualResetEvent stopEvent = new ManualResetEvent(false);

        public DelayedFileDelete()
        {
            thWatcher = new Thread(StartFileWatch);
            thWatcher.Priority = ThreadPriority.Lowest;
        }

        ~DelayedFileDelete()
        {
            stopEvent.Set();
            thWatcher.Join(DEFAULT_RETRY_INTERVAL);
            thWatcher.Abort();
        }

        public static DelayedFileDelete Default
        {
            get { return _default; }
        }

        public int RetryInterval
        {
            get { return retryInterval; }
            set { retryInterval = value; }
        }

        public void AddFileToList(string path)
        {
            lock (this) {
                if (fileList.Count == 0)
                    thWatcher.Start();

                fileList.Add(path);
            }
        }

        public DeleteDirectoryResult DeleteDirectory(string path, bool recursive)
        {
            if (!recursive) {
                Directory.Delete(path, false);
                return new DeleteDirectoryResult(true, new string[0], new string[0]);
            }

            bool result = true;
            List<string> deletedList = new List<string>();
            List<string> delayedList = new List<string>();

            string[] files = Directory.GetFiles(path, "*",
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            foreach (string item in files) {
                if (DeleteFile(item)) {
                    deletedList.Add(item);
                }
                else {
                    delayedList.Add(item);
                    result = false;
                }
            }

            if (result)
                Directory.Delete(path, true);
            else {
                lock (this) { dirList.Add(path); }
            }

            return new DeleteDirectoryResult(result, deletedList.ToArray(), delayedList.ToArray());
        }

        public bool DeleteFile(string path)
        {
            try { File.Delete(path); }
            catch (IOException) {
                MoveFileEx(path, null, MoveFileFlags.MOVEFILE_DELAY_UNTIL_REBOOT);
                AddFileToList(path);
                return false;
            }
            return true;
        }

        private void StartFileWatch()
        {
            do {
                lock (this) {
                    List<int> deletedItems = new List<int>();
                    for (int i = 0; i < fileList.Count; i++) {
                        try {
                            File.Delete(fileList[i]);
                            deletedItems.Add(i);
                        }
                        catch { }
                    }
                    foreach (int item in deletedItems)
                        fileList.RemoveAt(item);

                    deletedItems.Clear();
                    for (int i = 0; i < dirList.Count; i++) {
                        try {
                            Directory.Delete(dirList[i], true);
                            deletedItems.Add(i);
                        }
                        catch { }
                    }
                    foreach (int item in deletedItems)
                        dirList.RemoveAt(item);
                }
            } while (!stopEvent.WaitOne(retryInterval));
        }

        public class DeleteDirectoryResult
        {
            private bool result;
            private string[] deletedFiles;
            private string[] delayedFiles;

            public DeleteDirectoryResult(bool result,
                string[] deletedFiles,
                string[] delayedFiles)
            {
                this.result = result;
                this.deletedFiles = deletedFiles;
                this.delayedFiles = delayedFiles;
            }

            public bool Result { get { return result; } }
            public string[] DeletedFiles { get { return deletedFiles; } }
            public string[] DelayedFiles { get { return delayedFiles; } }
        }

        [Flags]
        enum MoveFileFlags
        {
            MOVEFILE_REPLACE_EXISTING = 0x00000001,
            MOVEFILE_COPY_ALLOWED = 0x00000002,
            MOVEFILE_DELAY_UNTIL_REBOOT = 0x00000004,
            MOVEFILE_WRITE_THROUGH = 0x00000008,
            MOVEFILE_CREATE_HARDLINK = 0x00000010,
            MOVEFILE_FAIL_IF_NOT_TRACKABLE = 0x00000020
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool MoveFileEx(string lpExistingFileName, string lpNewFileName,
           MoveFileFlags dwFlags);
    }
}
