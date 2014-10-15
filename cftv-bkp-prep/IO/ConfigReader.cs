// ConfigReader.cs
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

using SklLib.IO;
using System;

namespace cftv_bkp_prep.IO
{
    class ConfigReader : ConfigReaderBase
    {
        const string CFG_MAIN = "MAIN";
        int idxMain;

        public ConfigReader(string path)
        {
            base.filename = path;
        }

        public TimeSpan ScheduleTime { get { return GetTimeSpan(CFG_MAIN, "ScheduleTime"); } }
        public string[] SourcePath { get { return GetCsvString(CFG_MAIN, "SourcePath"); } }
        public string[] TargetPath { get { return GetCsvString(CFG_MAIN, "TargetPath"); } }
        public int Depth { get { return GetInteger(CFG_MAIN, "Depth"); } }

        new public void LoadFile()
        {
            base.LoadFile();
            idxMain = Array.IndexOf<string>(sections, CFG_MAIN);
        }

        public override bool IsValid()
        {
            return (idxMain == 0);
        }
    }
}
