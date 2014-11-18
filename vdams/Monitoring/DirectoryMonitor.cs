// DirectoryMonitor.cs
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using vdams.IO;

namespace vdams.Monitoring
{
    class DirectoryMonitor
    {
        Configuration.Monitor cfgMonitor;

        public DirectoryMonitor(Configuration.Monitor cfgMonitor)
        {
            this.cfgMonitor = cfgMonitor;
        }

        public static MonitorTransaction BeginTransaction()
        {
            return new MonitorTransaction();
        }

        public bool CollectInfo(MonitorTransaction transaction)
        {
            if (transaction == null)
                throw new ArgumentNullException("transaction");

            lock (transaction.Locker) {
                if (!transaction.IsRunning)
                    throw new InvalidOperationException("No monitoring transaction is running");

                if (!cfgMonitor.IsValid()) {
                    MainClass.Logger.WriteEntry(string.Format("The path '{0}' becomes invalid", cfgMonitor.Target.DirPath),
                                System.Diagnostics.EventLogEntryType.Error, EventId.AssortPathValidationError);
                    return false;
                }

                var logTransaction = MainClass.Logger.BeginWriteEntry();
                logTransaction.AppendLine(string.Format("Initializing monitoring to '{0}'", cfgMonitor.Target.DirPath));

                var camDirList =
                    from a in Directory.GetDirectories(cfgMonitor.Target.DirPath)
                    let r = cfgMonitor.GetCameraRegexInstance()
                    let dirInfo = new DirectoryInfo(a)
                    where r.IsMatch(dirInfo.Name)
                    select dirInfo;
                logTransaction.AppendLine(string.Format("Found {0} cameras", camDirList.Count()));
                foreach (var item in camDirList) {
                    var fileList = DirectoryListing.GetFiles(item.FullName);
                    long sumTotal = 0, sumToday = 0, sumYesterday = 0;
                    DateTime newestFile = DateTime.MinValue;
                    foreach (var f in fileList) {
                        FileInfo fInfo = new FileInfo(f);
                        sumTotal += fInfo.Length;
                        if (fInfo.LastWriteTime > newestFile)
                            newestFile = fInfo.LastWriteTime;
                        if (fInfo.LastWriteTime.Date == DateTime.Today)
                            sumToday += fInfo.Length;
                        else if (fInfo.LastWriteTime.Date == DateTime.Today.AddDays(-1D))
                            sumYesterday += fInfo.Length;
                    }
                    CameraInfo info = transaction[item.Name];
                    info.TotalSize += new SklLib.Measurement.InformationSize((ulong)sumTotal);
                    info.TotalSizeToday += new SklLib.Measurement.InformationSize((ulong)sumToday);
                    info.TotalSizeYesterday += new SklLib.Measurement.InformationSize((ulong)sumYesterday);
                    if (newestFile > info.LastModification)
                        info.LastModification = newestFile;
                    transaction[info.Name] = info;
                }
            }

            return true;
        }

        public static void EndTransaction(MonitorTransaction transaction)
        {
            throw new NotImplementedException();
        }
    }
}
