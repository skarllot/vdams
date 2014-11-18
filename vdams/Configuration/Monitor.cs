// Monitor.cs
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
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace vdams.Configuration
{
    class Monitor : IValidatable
    {
        public Target Target { get; set; }
        public string CameraNameRegex { get; set; }

        public Regex GetCameraRegexInstance()
        {
            if (string.IsNullOrEmpty(CameraNameRegex))
                return null;

            Regex result;
            try { result = new Regex(CameraNameRegex); }
            catch { return null; }

            return result;
        }

        public bool Validate(Action<InvalidEventArgs> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            bool result = true;
            if (Target == null) {
                action(new InvalidEventArgs(
                    "The target directory was not defined for monitoring operation",
                    "Target", null));
                result = false;
            }
            else if (!Target.Validate(action))
                result = false;

            if (!string.IsNullOrEmpty(CameraNameRegex)
                && GetCameraRegexInstance() == null) {
                action(new InvalidEventArgs(
                    string.Format("The regular expression '{0}' defined to get camera name is invalid", CameraNameRegex),
                    "CameraNameRegex", CameraNameRegex));
                result = false;
            }

            return result;
        }
    }
}
