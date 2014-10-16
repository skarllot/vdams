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
using System.Collections.ObjectModel;

namespace cftv_bkp_prep.IO
{
    class ConfigReader : ConfigReaderBase
    {
        const string CFG_MAIN = "MAIN";
        int idxMain;
        ConfigPathItem[] paths;

        public ConfigReader(string path)
        {
            base.filename = path;
        }

        public TimeSpan ScheduleTime { get { return GetTimeSpan(CFG_MAIN, "ScheduleTime"); } }
        public int Depth { get { return GetInteger(CFG_MAIN, "Depth"); } }
        public int PathCount { get { return paths.Length; } }

        public ConfigPathItem GetPath(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException("Parameter index cannot be less than zero.");
            if (index >= paths.Length)
                throw new ArgumentOutOfRangeException("Parameter index is out of array bounds.");

            return paths[index];
        }

        private ConfigPathItem GetNewPathItem(int index)
        {
            if (index >= idxMain)
                index++;
            return new ConfigPathItem(cfgreader, sections[index]);
        }

        new public void LoadFile()
        {
            base.LoadFile();
            idxMain = Array.IndexOf<string>(sections, CFG_MAIN);

            if (idxMain == -1)
                paths = new ConfigPathItem[sections.Length];
            else
                paths = new ConfigPathItem[sections.Length - 1];

            for (int i = 0; i < paths.Length; i++) {
                paths[i] = GetNewPathItem(i);
            }
        }

        public override bool IsValid()
        {
            if (idxMain < 0)
                return false;

            foreach (ConfigPathItem item in paths) {
                if (!item.IsValid())
                    return false;
            }

            return true;
        }
    }
}
