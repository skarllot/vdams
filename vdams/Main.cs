// Main.cs
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
using System.ServiceProcess;

namespace vdams
{
    class MainClass
    {
        public const string PROGRAM_NAME = "VDAMS";
        // Latest release: 0.2.0.26
        // Major.Minor.Maintenance.Build
        public const string PROGRAM_VERSION = "0.3.0.29";
        public const string PROGRAM_VERSION_SIMPLE = "0.3";
        public const string PROGRAM_TITLE = PROGRAM_NAME + " " + PROGRAM_VERSION_SIMPLE;

        const string LOG_NAME = MainClass.PROGRAM_NAME;
        const string LOG_SOURCE = "VDAMS " + MainClass.PROGRAM_VERSION_SIMPLE;
        private static SklLib.Diagnostics.Logger log;

        public static readonly bool DEBUG = System.Diagnostics.Debugger.IsAttached;

        public static SklLib.Diagnostics.Logger Logger { get { return log; } }

        public static void Main(string[] args)
        {
            log = new SklLib.Diagnostics.Logger(LOG_SOURCE, LOG_NAME);
            Service svc = new Service();

            if (!DEBUG) {
                ServiceBase[] servicesToRun = new ServiceBase[] { svc };
                System.ServiceProcess.ServiceBase.Run(servicesToRun);
            }
            else {
                svc.StartDebug(args);
            }
        }
    }
}
