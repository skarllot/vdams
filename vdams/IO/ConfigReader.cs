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

namespace vdams.IO
{
    class ConfigReader : ConfigDynamicReaderBase
    {
        const string CFG_MAIN = "MAIN";

        public ConfigReader(string path)
        {
            base.filename = path;
        }

        public ConfigMainSection MainSection { get { return GetSectionByName(CFG_MAIN) as ConfigMainSection; } }
        public int PathCount { get { return dynSections.Length; } }

        protected override string[] StaticNamedSections
        {
            get { return new string[] { CFG_MAIN }; }
        }

        protected override string[] MandatorySections
        {
            get { return StaticNamedSections; }
        }

        public ConfigPathSection GetPath(int index)
        {
            return GetDynamicSection(index) as ConfigPathSection;
        }

        protected override ConfigSectionReaderBase GetSectionReader(string section)
        {
            if (section == CFG_MAIN)
                return new ConfigMainSection(cfgreader, CFG_MAIN);
            else
                return new ConfigPathSection(cfgreader, section);
        }

        public override bool IsValid()
        {
            if (!base.IsValid())
                return false;

            if (dynSections.Length < 1)
                return false;

            return true;
        }
    }
}
