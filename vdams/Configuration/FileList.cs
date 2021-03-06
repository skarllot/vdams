﻿// FileList.cs
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
    class FileList : IValidatable
    {
        int dateDepth = 1;

        [YamlAlias("bom")]
        public bool BOM { get; set; }
        [YamlAlias("directory")]
        public string DirPath { get; set; }
        public string Encoding { get; set; }
        public Time? ScheduleTime { get; set; }
        public int DateDepth { get { return dateDepth; } set { dateDepth = value; } }

        public bool HasPermissionDirPath()
        {
            return new FileInfo(DirPath).HasPermission(FileIOPermissionAccess.Write);
        }

        public System.Text.Encoding GetEncodingInstance()
        {
            return EncodingParser.GetEncodingInstance(Encoding, BOM);
        }

        public bool Validate(System.Action<InvalidEventArgs> action)
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
            else if (!HasPermissionDirPath()){
                action(new InvalidEventArgs(
                    string.Format("The current user doesn't has write permission on specified directory '{0}'", DirPath),
                    "DirPath", DirPath));
                result = false;
            }

            if (GetEncodingInstance() == null) {
                action(new InvalidEventArgs(
                    string.Format("The specified encoding '{0}' for file-list file is invalid", Encoding ?? string.Empty),
                    "Encoding", Encoding));
                result = false;
            }

            if (ScheduleTime == null) {
                action(new InvalidEventArgs(
                    "The schedule time is required",
                    "ScheduleTime", null));
                result = false;
            }

            return result;
        }
    }
}
