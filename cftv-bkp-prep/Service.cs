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

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace cftv_bkp_prep
{
    public class Service : System.ServiceProcess.ServiceBase
    {
        const string DEFAULT_CFG_FILE_NAME = "config.ini";
        const string DEFAULT_TIME_FORMAT = "HH:mm";
        const int DEFAULT_REFRESH = 1 * MINUTE_TO_MILLISECONDS;
        const int MINUTE_TO_MILLISECONDS = 1000 * 60;

        SklLib.Diagnostics.Logger eventLog = MainClass.Logger;
        System.ComponentModel.Container components = null;
        Thread svcThread;
        Thread reloadThread;
        ManualResetEvent stopEvent;
        ManualResetEvent reloadEvent;

        public Service()
        {
            this.ServiceName = MainClass.PROGRAM_NAME;

            stopEvent = new ManualResetEvent(true);
            reloadEvent = new ManualResetEvent(false);
        }

        private void ConfigWatcher(object obj)
        {
            string cfgpath = (string)obj;

            DateTime dtCfg = File.GetLastWriteTime(cfgpath);
            DateTime dtCfg2;
            while (!stopEvent.WaitOne(0)) {
                dtCfg2 = File.GetLastWriteTime(cfgpath);
                if (dtCfg != dtCfg2) {
                    dtCfg = dtCfg2;
                    reloadEvent.Set();
                }

                if (stopEvent.WaitOne(DEFAULT_REFRESH))
                    break;
            }
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

            string cfgFile = GetConfigFileFullName(dir, DEFAULT_CFG_FILE_NAME);

            svcThread = new Thread(new ParameterizedThreadStart(StartThread));
            svcThread.Start(cfgFile);
            eventLog.WriteEntry(string.Format("{0} service started", MainClass.PROGRAM_NAME),
                EventLogEntryType.Information, EventId.ServiceStateChanged);

            reloadThread = new Thread(new ParameterizedThreadStart(ConfigWatcher));
            reloadThread.Start(cfgFile);
        }

        /// <summary>
        /// Stop this service.
        /// </summary>
        protected override void OnStop()
        {
            base.OnStop();

            if (svcThread != null && svcThread.IsAlive) {
                stopEvent.Set();
                svcThread.Join();
                reloadThread.Join();
            }

            eventLog.WriteEntry(string.Format("{0} service stopped", MainClass.PROGRAM_NAME),
                EventLogEntryType.Information, EventId.ServiceStateChanged);
        }

        /*protected override void OnContinue()
        {
            eventLog1.WriteEntry("my service is continuing in working");
        }*/

        private void StartThread(object obj)
        {
            stopEvent.Reset();
            string cfgpath = (string)obj;

            IO.ConfigReader config = new IO.ConfigReader(cfgpath);
            if (!ValidateConfiguration(config)) {
                eventLog.WriteEntry("Initial configuration file loading failed",
                    EventLogEntryType.Error, EventId.ConfigFileLoadError);
                return;
            }

            DirectoryAssorter dirAssort = new DirectoryAssorter();

            while (!stopEvent.WaitOne(0)) {
                if (DateTime.Now.ToString(DEFAULT_TIME_FORMAT) ==
                    config.ScheduleTime.ToString(DEFAULT_TIME_FORMAT)) {
                    for (int i = 0; i < config.PathCount; i++) {
                        IO.ConfigPathItem item = config.GetPath(i);
                        dirAssort.DoWork(item.SourceFullPath, item.TargetFullPath);

                        if (stopEvent.WaitOne(0))
                            break;
                    }
                }

                if (reloadEvent.WaitOne(0)) {
                    if (!ValidateConfiguration(config)) {
                        eventLog.WriteEntry("Configuration file was changed to invalid state",
                            EventLogEntryType.Error, EventId.ConfigFileReloadError);
                    }
                    else {
                        eventLog.WriteEntry("Configuration file reloaded",
                            EventLogEntryType.Information, EventId.ConfigFileReloaded);
                    }
                    reloadEvent.Reset();
                }

                if (stopEvent.WaitOne(DEFAULT_REFRESH))
                    break;
            }
        }

        private bool ValidateConfiguration(IO.ConfigReader config)
        {
            if (!ValidateConfigFile(config.FileName)) {
                eventLog.WriteEntry(string.Format("Error loading configuration file {0}", config.FileName),
                    EventLogEntryType.Error, EventId.ConfigFileLoadError);
                return false;
            }
            config.LoadFile();

            if (config.ScheduleTime > (new TimeSpan(23, 59, 59))) {
                eventLog.WriteEntry("Invalid scheduled time, must be a valid time of day",
                    EventLogEntryType.Error, EventId.ConfigFileInvalidSchedule);
                return false;
            }

            if (config.PathCount < 1) {
                eventLog.WriteEntry("At least one path must be defined",
                    EventLogEntryType.Error, EventId.ConfigFileZeroPath);
                return false;
            }

            for (int i = 0; i < config.PathCount; i++) {
                IO.ConfigPathItem item = config.GetPath(i);

                if (!Directory.Exists(item.SourceFullPath)) {
                    eventLog.WriteEntry(string.Format("The specified source path \"{0}\" doesn't exist", item.SourceFullPath),
                        EventLogEntryType.Error, EventId.ConfigFileInvalidPath);
                    return false;
                }

                if (!Directory.Exists(item.TargetFullPath)) {
                    eventLog.WriteEntry(string.Format("The specified target path \"{0}\" doesn't exist", item.TargetFullPath),
                        EventLogEntryType.Error, EventId.ConfigFileInvalidPath);
                    return false;
                }

                if (!item.HasReadPermissionToSourcePath()) {
                    eventLog.WriteEntry(string.Format(
                        "The current user doesn't has read permission on specified source path \"{0}\"", item.SourceFullPath),
                        EventLogEntryType.Error, EventId.ConfigFilePathPermissionError);
                    return false;
                }

                if (!item.HasWritePermissionToTargetPath()) {
                    eventLog.WriteEntry(string.Format(
                        "The current user doesn't has write permission on specified target path \"{0}\"", item.TargetFullPath),
                        EventLogEntryType.Error, EventId.ConfigFilePathPermissionError);
                    return false;
                }
            }

            return true;
        }

        internal void StartDebug(string[] args)
        {
            this.OnStart(args);
        }

        private bool ValidateConfigFile(string file)
        {
            try { SklLib.IO.ConfigFileReader reader = new SklLib.IO.ConfigFileReader(file); }
            catch (FileLoadException) { return false; }
            return true;
            // return reader.IsValidFile();
        }
    }
}
