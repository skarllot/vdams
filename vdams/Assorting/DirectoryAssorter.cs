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

using SklLib.Collections;
using SklLib.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using vdams.IO;

namespace vdams.Assorting
{
    class DirectoryAssorter
    {
        const string FILELIST_DATE_FORMAT = "yyyy-MM-dd";
        const string FILELIST_NAME = "{0}.txt";
        const string FILELIST_REGEX_EXPRESSION = @"^([1-9][0-9]{3})-(0[0-9]|1[0-2])-([0-2][0-9]|3[0-1])[.]txt$";
        const string FILELIST_LATEST_NAME = "latest.txt";
        static readonly Regex FILELIST_REGEX = new Regex(FILELIST_REGEX_EXPRESSION, RegexOptions.Compiled);
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
                string fileName = Path.Combine(path, string.Format(FILELIST_NAME,
                    dt.ToString(FILELIST_DATE_FORMAT)));
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
                    if (isFileDateFormatDefined) {
                        fileList
                            .Where(s => s.IndexOf(strDt) != -1)
                            .ForEach(s => {
                                pickedList.Add(s);
                                totalBytes += new FileInfo(s).Length;
                            });
                    }
                    else {
                        fileList
                            .ConvertAll(s => new FileInfo(s))
                            .Where(s => s.LastWriteTime.Date == dt.Date)
                            .ForEach(s => {
                                pickedList.Add(s.FullName);
                                totalBytes += s.Length;
                            });
                    }

                    logTransaction.AppendLine(string.Format("Found {0} files, total size of {1}",
                        pickedList.Count, new SklLib.DataSize(totalBytes).ToString("N2")));

                    logTransaction.AppendLine(string.Format("Writing file list of files modified on {0}", dt.ToShortDateString()));
                    string fileName = Path.Combine(transaction.Path,
                        string.Format(FILELIST_NAME,
                        dt.ToString(FILELIST_DATE_FORMAT)));

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

            lock (transaction.Locker) {
                string latestPath = Path.Combine(
                    transaction.Path, FILELIST_LATEST_NAME);
                if (File.Exists(latestPath))
                    File.Delete(latestPath);

                FileInfo[] files = new DirectoryInfo(transaction.Path)
                    .GetFiles()
                    .Where(s => FILELIST_REGEX.IsMatch(s.Name))
                    .OrderByDescending(s => DateTime.ParseExact(
                        Path.GetFileNameWithoutExtension(s.Name),
                        FILELIST_DATE_FORMAT, null).Ticks)
                    .ToArray();
                if (files.Length > 0) {
                    files[0].CreateHardLink(latestPath);
                }

                transaction.Terminate();
            }
        }

        private static IList<string> GetFileList(string path)
        {
            List<string> fileList = new List<string>();
            Action<string, IList<string>> recursiveGetFileList = null;

            recursiveGetFileList = delegate(string p, IList<string> files) {
                try {
                    Directory.GetFiles(p)
                        .ForEach(s => files.Add(s));

                    Directory.GetDirectories(p)
                        .ForEach(s => recursiveGetFileList(s, files));
                }
                catch (UnauthorizedAccessException) { }
            };

            recursiveGetFileList(path, fileList);
            return fileList;
        }
    }
}
