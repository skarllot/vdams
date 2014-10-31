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
using System.Security.Permissions;

namespace vdams.IO
{
    class ConfigMainSection : ConfigSectionReaderBase
    {
        public TimeSpan? ScheduleTime { get { return GetTimeSpan("ScheduleTime"); } }
        public int DateDepth { get { return GetInteger("DateDepth") ?? 1; } }
        public string FileListPath { get { return GetString("FileListPath"); } }

        public ConfigMainSection(ConfigFileReader reader, string section)
            : base(reader, section)
        {
        }

        public bool HasPermissionFileListPath()
        {
            return new FileInfo(FileListPath).HasPermission(FileIOPermissionAccess.Write);
        }

        public override bool IsValid()
        {
            if (ScheduleTime == null
                || ScheduleTime > new TimeSpan(23, 59, 59)) {
                    return false;
            }

            if (FileListPath != null) {
                try {
                    Path.GetFullPath(FileListPath);
                    if (!Directory.Exists(FileListPath))
                        return false;
                    if (!HasPermissionFileListPath())
                        return false;
                }
                catch { return false; }
            }

            return true;
        }
    }
}
