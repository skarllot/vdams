// ConfigPathItem.cs
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
using System.IO;

namespace cftv_bkp_prep.IO
{
    class ConfigPathItem : ConfigReaderBase
    {
        public ConfigPathItem(ConfigFileReader reader, string section)
        {
            this.cfgreader = reader;
            this.sections = new string[] { section };
        }

        public string Drive { get { return GetString(sections[0], "Drive"); } }
        public string SourcePath { get { return GetString(sections[0], "SourcePath"); } }
        public string TargetPath { get { return GetString(sections[0], "TargetPath"); } }

        public string FullSourcePath { get { return Path.Combine(Drive, SourcePath); } }
        public string FullTargetPath { get { return Path.Combine(Drive, TargetPath); } }

        public override bool IsValid()
        {
            throw new NotImplementedException();
        }
    }
}
