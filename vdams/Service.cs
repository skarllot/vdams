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

namespace vdams
{
    public class Service : System.ServiceProcess.ServiceBase
    {
        const string CFG_FILE_NAME = "config.yml";
        const string CFG_FILE_NAME_LEGACY = "config.ini";
        const string TIME_FORMAT_DT = "HH:mm";
        const string TIME_FORMAT_TS = "hh\\:mm";
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

        private static Assorting.DirectoryAssorter[] GetAssorter(Configuration.Configuration config)
        {
            Assorting.DirectoryAssorter[] ret = new Assorting.DirectoryAssorter[config.Assort.Count];
            for (int i = 0; i < ret.Length; i++) {
                ret[i] = new Assorting.DirectoryAssorter(config.Assort[i]);
            }

            return ret;
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

            Configuration.ConfigReader config = new Configuration.ConfigReader(cfgpath);
            if (!TryLoadConfiguration(config)) {
                eventLog.WriteEntry("Initial configuration file loading failed",
                    EventLogEntryType.Error, EventId.ConfigFileLoadError);
                stopEvent.Set();
                return;
            }

            Assorting.DirectoryAssorter[] arrAssorter = GetAssorter(config);

            while (!stopEvent.WaitOne(0)) {
                if (DateTime.Now.ToString(TIME_FORMAT_DT) ==
                    config.MainSection.ScheduleTime.Value.ToString(TIME_FORMAT_TS)
                    || MainClass.DEBUG) {
                        if (config.FilelistSection != null) {
                            var transaction = Assorting.DirectoryAssorter.BeginTransaction(
                                config.FilelistSection, config.MainSection.DateDepth);
                            foreach (Assorting.DirectoryAssorter item in arrAssorter) {
                                try {
                                    item.Assort(transaction);
                                }
                                catch (Exception ex) {
                                    eventLog.WriteEntry(ex.CreateDump(),
                                        EventLogEntryType.Error, EventId.UnexpectedError);
                                    stopEvent.Set();
                                    return;
                                }

                                if (stopEvent.WaitOne(0))
                                    break;
                            }
                            Assorting.DirectoryAssorter.EndTransaction(transaction);
                        }
                }

                if (reloadEvent.WaitOne(0)) {
                    Configuration.ConfigReader tmpConfig = new Configuration.ConfigReader(config.FileName);
                    if (!TryLoadConfiguration(tmpConfig)) {
                        eventLog.WriteEntry("Configuration file was changed to invalid state",
                            EventLogEntryType.Error, EventId.ConfigFileReloadError);
                    }
                    else {
                        config = tmpConfig;
                        arrAssorter = GetAssorter(config);
                        eventLog.WriteEntry("Configuration file reloaded",
                            EventLogEntryType.Information, EventId.ConfigFileReloaded);
                    }
                    reloadEvent.Reset();
                }

                if (stopEvent.WaitOne(DEFAULT_REFRESH))
                    break;
            }
        }

        private bool TryLoadConfiguration(Configuration.ConfigReader config)
        {
            if (!ValidateConfigFile(config.FileName)) {
                eventLog.WriteEntry(string.Format("Error loading configuration file {0}", config.FileName),
                    EventLogEntryType.Error, EventId.ConfigFileLoadError);
                return false;
            }
            config.LoadFile();

            if (!config.IsValid()) {
                if (config.MainSection.ScheduleTime > (new TimeSpan(23, 59, 59))) {
                    eventLog.WriteEntry("Invalid scheduled time, must be a valid time of day",
                        EventLogEntryType.Error, EventId.ConfigFileInvalidSchedule);
                }

                if (config.FilelistSection != null) {
                    if (!Directory.Exists(config.FilelistSection.DirPath)) {
                        eventLog.WriteEntry(string.Format(
                            "The specified path for file-list file \"{0}\" doesn't exist", config.FilelistSection.DirPath ?? string.Empty),
                            EventLogEntryType.Error, EventId.ConfigFileInvalidPath);
                    }
                    else if (!config.FilelistSection.HasPermissionFileListPath()) {
                        eventLog.WriteEntry(string.Format(
                            "The current user doesn't has write permission on specified file-list file \"{0}\"",
                            config.FilelistSection.DirPath),
                            EventLogEntryType.Error, EventId.ConfigFilePathPermissionError);
                    }

                    if (config.FilelistSection.GetEncodingInstance() == null) {
                        eventLog.WriteEntry(string.Format(
                            "The specified text enconding for file-list file \"{0}\" is invalid", config.FilelistSection.Encoding),
                            EventLogEntryType.Error, EventId.ConfigFileInvalidEncoding);
                    }
                }

                if (config.PathCount < 1) {
                    eventLog.WriteEntry("At least one path must be defined",
                        EventLogEntryType.Error, EventId.ConfigFileZeroPath);
                }

                for (int i = 0; i < config.PathCount; i++) {
                    Configuration.ConfigPathSection item = config.GetPath(i);

                    if (!Directory.Exists(item.DirPath)) {
                        eventLog.WriteEntry(string.Format("The specified source path \"{0}\" doesn't exist", item.DirPath ?? string.Empty),
                            EventLogEntryType.Error, EventId.ConfigFileInvalidPath);
                    }
                    else if (!item.HasPermissionSourcePath()) {
                        eventLog.WriteEntry(string.Format(
                            "The current user doesn't has read permission on specified source path \"{0}\"", item.DirPath),
                            EventLogEntryType.Error, EventId.ConfigFilePathPermissionError);
                    }
                }

                return false;
            }

            return true;
        }

        private bool ValidateConfigFile(string file)
        {
            try {
                SklLib.IO.IniFileReader reader = new SklLib.IO.IniFileReader(file);
                reader.ReloadFile();
            }
            catch (FileNotFoundException) { return false; }
            catch (FileLoadException) { return false; }
            catch (Exception ex) {
                eventLog.WriteEntry(ex.CreateDump(),
                    EventLogEntryType.Error, EventId.UnexpectedError);
                stopEvent.Set();
                return false;
            }
            return true;
            // return reader.IsValidFile();
        }
    }
}
