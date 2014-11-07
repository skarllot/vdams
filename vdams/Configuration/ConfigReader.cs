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

using SklLib.Configuration;
using SklLib.IO;
using System;
using System.Collections.ObjectModel;

namespace vdams.Configuration
{
    class ConfigReader : DynamicIniReaderBase
    {
        const string CFG_MAIN = "MAIN";
        const string CFG_FILELIST = "FILELIST";
        static readonly string[] sSections =
            new string[] { CFG_MAIN, CFG_FILELIST };
        static readonly string[] mSections =
            new string[] { CFG_MAIN };

        public ConfigReader(string path)
        {
            base.filename = path;
        }

        public ConfigMainSection MainSection { get { return GetSectionByName(CFG_MAIN) as ConfigMainSection; } }
        
        public ConfigFilelistSection FilelistSection
        {
            get { return GetSectionByName(CFG_FILELIST) as ConfigFilelistSection; }
        }

        public int PathCount { get { return dynSections.Length; } }

        protected override string[] StaticNamedSections
        {
            get { return (string[])sSections.Clone(); }
        }

        protected override string[] MandatorySections
        {
            get { return (string[])mSections.Clone(); }
        }

        public ConfigPathSection GetPath(int index)
        {
            return GetDynamicSection(index) as ConfigPathSection;
        }

        protected override IniSectionReaderBase GetSectionInstance(string section)
        {
            if (section == CFG_MAIN)
                return new ConfigMainSection(cfgreader, CFG_MAIN);
            else if (section == CFG_FILELIST)
                return new ConfigFilelistSection(cfgreader, CFG_FILELIST);
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
