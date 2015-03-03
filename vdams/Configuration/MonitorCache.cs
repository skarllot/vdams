// MonitorCache.cs
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

using SklLib;
using SklLib.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using YamlDotNet.Serialization;

namespace vdams.Configuration
{
    class MonitorCache : IValidatable
    {
        TimeSpan _updateInterval = new TimeSpan(0, 1, 30);

        [YamlAlias("directory")]
        public string DirPath { get; set; }
        public TimeSpan UpdateInterval { get { return _updateInterval; } set { _updateInterval = value; } }

        public bool HasPermissionDirPath()
        {
            return new FileInfo(DirPath).HasPermission(FileIOPermissionAccess.Write);
        }

        public bool Validate(Action<InvalidEventArgs> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            bool result = true;

            if (DirPath == null) {
                action(new InvalidEventArgs(
                    "The directory must be defined for file-list configuration",
                    "DirPath", null));
                result = false;
            }
            else if (!Directory.Exists(DirPath)) {
                action(new InvalidEventArgs(
                    string.Format("The specified path '{0}' for file-list file doesn't exist", DirPath ?? string.Empty),
                    "DirPath", DirPath));
                result = false;
            }
            else if (!HasPermissionDirPath()) {
                action(new InvalidEventArgs(
                    string.Format("The current user doesn't has write permission on specified directory '{0}'", DirPath),
                    "DirPath", DirPath));
                result = false;
            }

            return result;
        }
    }
}
