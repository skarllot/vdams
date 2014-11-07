// ConfigPathSection.cs
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
using System.IO;
using System.Security.Permissions;
using System.Text.RegularExpressions;

namespace vdams.Configuration
{
    class ConfigPathSection : IniSectionReaderBase
    {
        public ConfigPathSection(IniFileReader reader, string section)
            : base(reader, section)
        {
        }

        public Regex CameraNameRegex { get { return GetRegex("CameraNameRegex"); } }
        public string FileDateFormat { get { return GetString("FileDateFormat"); } }
        public string DirPath { get { return GetString("Directory"); } }

        public bool HasPermissionSourcePath()
        {
            return new FileInfo(DirPath).HasPermission(FileIOPermissionAccess.Read);
        }

        public override bool IsValid()
        {
            try {
                Path.GetFullPath(DirPath);
                if (!Directory.Exists(DirPath))
                    return false;
                if (!HasPermissionSourcePath())
                    return false;
            }
            catch { return false; }

            return true;
        }
    }
}
