// Target.cs
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
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Security.Permissions;
using YamlDotNet.Serialization;

namespace vdams.Configuration
{
    class Target : IValidatable
    {
        [YamlAlias("directory")]
        public string DirPath { get; set; }

        private bool HasPermissionSourcePath()
        {
            return new FileInfo(DirPath).HasPermission(FileIOPermissionAccess.Read);
        }

        public bool Validate(Action<InvalidEventArgs> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            bool result = true;

            if (DirPath == null) {
                action(new InvalidEventArgs(
                    "The directory must be defined to target configuration",
                    "DirPath", null));
                result = false;
            }
            else if (!Directory.Exists(DirPath)) {
                action(new InvalidEventArgs(
                    string.Format("The specified path '{0}' for target directory doesn't exist", DirPath ?? string.Empty),
                    "DirPath", DirPath));
                result = false;
            }
            else if (!HasPermissionSourcePath()) {
                action(new InvalidEventArgs(
                    string.Format("The current user doesn't has read permission on specified target directory '{0}'", DirPath),
                    "DirPath", DirPath));
                result = false;
            }

            return result;
        }
    }
}
