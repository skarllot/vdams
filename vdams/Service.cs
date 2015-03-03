// Service.cs
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
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <http://www.gnu.org/licenses/>.

using SklLib;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Linq;

namespace vdams
{
    public class Service : System.ServiceProcess.ServiceBase
    {
        const string CFG_FILE_NAME = "config.yml";
        const int DEFAULT_REFRESH = 1 * MINUTE_TO_MILLISECONDS;
        const int MINUTE_TO_MILLISECONDS = 1000 * 60;

        SklLib.Diagnostics.Logger eventLog = MainClass.Logger;
        System.ComponentModel.Container components = null;
        Thread svcThread;
        FileSystemWatcher configWatcher;
        ManualResetEvent stopEvent;
        ManualResetEvent reloadEvent;
        object lckInstance;

        public Service()
        {
            this.ServiceName = MainClass.PROGRAM_NAME;

            stopEvent = new ManualResetEvent(true);
            reloadEvent = new ManualResetEvent(false);
            lckInstance = new object();
        }

        private string GetConfigFileFullName(string dir, string fileName)
        {
            string cfgFile = null;
            if (dir == null) {
                string cfgDir = Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.CommonApplicationData,
                    Environment.SpecialFolderOption.None), this.ServiceName);
                if (!Directory.Exists(cfgDir))
                    Directory.CreateDirectory(cfgDir);
                cfgFile = Path.Combine(cfgDir, fileName);
            }
            else
                cfgFile = Path.Combine(dir, fileName);
            if (!File.Exists(cfgFile)) {
                string msg = string.Format("The configuration file \"{0}\" does not exist.", cfgFile);
                eventLog.WriteEntry(msg, EventLogEntryType.Error, EventId.ConfigFileNotFound);
                // http://msdn.microsoft.com/en-us/library/ms681384%28v=vs.85%29
                this.ExitCode = 15010;
                throw new FileNotFoundException(msg, cfgFile);
            }

            return cfgFile;
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing) {
                if (components != null) {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Set things in motion so your service can do its work.
        /// </summary>
        protected override void OnStart(string[] args)
        {
            base.OnStart(args);

            string[] cmdArgs = Environment.GetCommandLineArgs();
            if (cmdArgs.Length > 1) {
                args = new string[cmdArgs.Length - 1];
                Array.Copy(cmdArgs, 1, args, 0, cmdArgs.Length - 1);
            }

            string dir = null;
            if (args.Length == 1)
                dir = args[0];

            string cfgFile = GetConfigFileFullName(dir, CFG_FILE_NAME);

            svcThread = new Thread(new ParameterizedThreadStart(StartThread));
            svcThread.Start(cfgFile);

            configWatcher = new FileSystemWatcher(Path.GetDirectoryName(cfgFile), Path.GetFileName(cfgFile));
            configWatcher.NotifyFilter = NotifyFilters.LastWrite;
            configWatcher.Changed += configWatcher_Changed;
            configWatcher.EnableRaisingEvents = true;

            eventLog.WriteEntry(string.Format("{0} service started", MainClass.PROGRAM_NAME),
                EventLogEntryType.Information, EventId.ServiceStateChanged);
        }

        void configWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            lock (lckInstance) {
                if (!reloadEvent.WaitOne(0)) {
                    if (e.ChangeType == WatcherChangeTypes.Changed) {
                        reloadEvent.Set();
                    }
                }
            }
        }

        /// <summary>
        /// Stop this service.
        /// </summary>
        protected override void OnStop()
        {
            base.OnStop();

            configWatcher.EnableRaisingEvents = false;
            if (svcThread != null && svcThread.IsAlive) {
                stopEvent.Set();
                svcThread.Join();
            }

            eventLog.WriteEntry(string.Format("{0} service stopped", MainClass.PROGRAM_NAME),
                EventLogEntryType.Information, EventId.ServiceStateChanged);
        }

        internal void StartDebug(string[] args)
        {
            this.OnStart(args);
        }

        private void StartThread(object obj)
        {
            stopEvent.Reset();
            string cfgpath = (string)obj;

            Configuration.Configuration config;
            try { config = Configuration.Configuration.LoadFile(cfgpath); }
            catch (Exception e) {
                eventLog.WriteEntry(string.Format(
                    "Error loading configuration file {0}\n\nDetail: {1}", cfgpath, e.Message),
                    EventLogEntryType.Error, EventId.ConfigFileLoadError);
                stopEvent.Set();
                return;
            }
            var transaction = eventLog.BeginWriteEntry();
            transaction.EntryType = EventLogEntryType.Error;
            transaction.EventLogId = EventId.ConfigFileInvalid;
            if (!config.Validate(arg => transaction.AppendLine(arg.Message))) {
                transaction.Commit();
                return;
            }
            else
                transaction.Rollback();

            while (!stopEvent.WaitOne(0)) {
                if (config.FileList.ScheduleTime.Value.CompareTo(DateTime.Now, TimeFields.HourMinute) == 0
                    || MainClass.DEBUG) {
                    var assorters = config.GetDirectoryAssorters();
                    var monitors = config.GetDirectoryMonitors();
                    var monitorTransaction = Monitoring.DirectoryMonitor.BeginTransaction();
                    var assortTransaction = Assorting.DirectoryAssorter.BeginTransaction(
                        config.FileList);

                    foreach (var itemTarget in config.GetTargets()) {
                        foreach (var m in monitors.Where(a => a.Target == itemTarget.Name)) {
                            try { m.CollectInfo(monitorTransaction, itemTarget); }
                            catch (Exception ex) {
                                eventLog.WriteEntry(ex.CreateDump(),
                                    EventLogEntryType.Error, EventId.UnexpectedError);
                                stopEvent.Set();
                                return;
                            }

                            if (stopEvent.WaitOne(0))
                                break;
                        }

                        foreach (var a in assorters.Where(b => b.Target == itemTarget.Name)) {
                            try { a.Assort(assortTransaction, itemTarget); }
                            catch (Exception ex) {
                                eventLog.WriteEntry(ex.CreateDump(),
                                    EventLogEntryType.Error, EventId.UnexpectedError);
                                stopEvent.Set();
                                return;
                            }

                            if (stopEvent.WaitOne(0))
                                break;
                        }
                    }
                    Assorting.DirectoryAssorter.EndTransaction(assortTransaction);
                    Monitoring.DirectoryMonitor.EndTransaction(monitorTransaction);
                }

                if (reloadEvent.WaitOne(0)) {
                    var tmpConfig = Configuration.Configuration.LoadFile(cfgpath);
                    transaction = eventLog.BeginWriteEntry();
                    transaction.EntryType = EventLogEntryType.Error;
                    transaction.EventLogId = EventId.ConfigFileReloadError;
                    transaction.AppendLine("Configuration file was changed to invalid state. Details:");
                    if (tmpConfig.Validate(args => transaction.AppendLine(args.Message))) {
                        transaction.Commit();
                    }
                    else {
                        transaction.Rollback();
                        config = tmpConfig;
                        eventLog.WriteEntry("Configuration file reloaded",
                            EventLogEntryType.Information, EventId.ConfigFileReloaded);
                    }
                    reloadEvent.Reset();
                }

                if (stopEvent.WaitOne(DEFAULT_REFRESH))
                    break;
            }
        }
    }
}
