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

using SklLib;
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
        Configuration.Assort cfgAssort;

        public DirectoryAssorter(Configuration.Assort cfgAssort)
        {
            this.cfgAssort = cfgAssort;
        }

        public string Target { get { return cfgAssort.Target; } }

        public static AssortTransaction BeginTransaction(Configuration.FileList cfgFilelist)
        {
            int counter = 1;
            while (counter <= cfgFilelist.DateDepth) {
                DateTime dt = DateTime.Today.AddDays(-1 * counter);
                string fileName = Path.Combine(
                    cfgFilelist.DirPath, string.Format(FILELIST_NAME,
                    dt.ToString(FILELIST_DATE_FORMAT)));
                if (File.Exists(fileName))
                    File.Delete(fileName);

                counter++;
            }

            return new AssortTransaction(cfgFilelist);
        }

        public bool Assort(AssortTransaction transaction, Targeting target)
        {
            if (transaction == null)
                throw new ArgumentNullException("transaction");

            lock (transaction.Locker) {
                if (!transaction.IsRunning)
                    throw new InvalidOperationException("No assorting transaction is running");

                var logTransaction = MainClass.Logger.BeginWriteEntry();
                logTransaction.AppendLine(string.Format("Initializing assorting to '{0}'", target.DirPath));

                var fileList = target.FileList;
                logTransaction.AppendLine(string.Format("Found {0} total files", fileList.Count()));

                // If a file date format was not defined into configuration then assort files based on modification date;
                // If not, assort based on file name.
                bool isFileDateFormatDefined = !string.IsNullOrWhiteSpace(target.FileDateFormat);

                int counter = 1;
                while (counter <= transaction.Configuration.DateDepth) {
                    DateTime dt = DateTime.Today.AddDays(-1 * counter);
                    List<string> pickedList = new List<string>();
                    long totalBytes = 0;
                    string strDt = dt.ToString(target.FileDateFormat);

                    logTransaction.AppendLine(string.Format("Looking for file modified on {0}", dt.ToShortDateString()));
                    IEnumerable<FileInfo> selectedFiles;
                    if (isFileDateFormatDefined) {
                        selectedFiles =
                            from a in fileList
                            where a.Name.IndexOf(strDt) != -1
                            select a;
                    }
                    else {
                        selectedFiles =
                            from a in fileList
                            where a.LastWriteTime.Date == dt.Date
                            select a;
                    }
                    foreach (FileInfo item in selectedFiles) {
                        pickedList.Add(item.FullName);
                        totalBytes += item.Length;
                    }

                    logTransaction.AppendLine(string.Format("Found {0} files, total size of {1}",
                        pickedList.Count, new SklLib.Measurement.InformationSize((ulong)totalBytes).ToString("N2")));

                    logTransaction.AppendLine(string.Format("Writing file list of files modified on {0}", dt.ToShortDateString()));
                    string fileName = Path.Combine(transaction.Configuration.DirPath,
                        string.Format(FILELIST_NAME,
                        dt.ToString(FILELIST_DATE_FORMAT)));

                    StreamWriter writer = new StreamWriter(fileName, true,
                        transaction.Configuration.GetEncodingInstance());
                    foreach (string item in pickedList)
                        writer.WriteLine(item);
                    writer.Flush();
                    writer.Close();
                    writer.Dispose();

                    logTransaction.AppendLine(string.Format("Assort of {0} ended", dt.ToShortDateString()));
                    counter++;
                }
                fileList = null;

                logTransaction.EntryType = System.Diagnostics.EventLogEntryType.Information;
                logTransaction.EventLogId = EventId.AssortCompleted;
                logTransaction.Commit();
                return true;
            }
        }

        public static void EndTransaction(AssortTransaction transaction)
        {
            if (transaction == null)
                throw new ArgumentNullException("transaction");

            lock (transaction.Locker) {
                string latestPath = Path.Combine(
                    transaction.Configuration.DirPath, FILELIST_LATEST_NAME);
                if (File.Exists(latestPath))
                    File.Delete(latestPath);

                FileInfo fileLatest = new DirectoryInfo(transaction.Configuration.DirPath)
                    .GetFiles()
                    .Where(s => FILELIST_REGEX.IsMatch(s.Name))
                    .Max(s => (IComparable)DateTime.ParseExact(
                        Path.GetFileNameWithoutExtension(s.Name),
                        FILELIST_DATE_FORMAT, null).Ticks);
                if (fileLatest != null) {
                    fileLatest.CreateHardLink(latestPath);
                }

                transaction.Terminate();
            }
        }
    }
}
