// Assort.cs
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

namespace vdams.Configuration
{
    class Assort: IValidatable
    {
        public Target Target { get; set; }
        public string FileDateFormat { get; set; }

        public bool Validate(Action<InvalidEventArgs> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            bool result = true;
            if (Target == null) {
                action(new InvalidEventArgs(
                    "The target directory was not defined for assorting operation",
                    "Target", null));
                result = false;
            }
            else if (!Target.Validate(action))
                result = false;

            if (FileDateFormat != null) {
                try { DateTime.Now.ToString(FileDateFormat); }
                catch (Exception e) {
                    if (e is FormatException || e is ArgumentOutOfRangeException) {
                        action(new InvalidEventArgs(
                            string.Format("The date format '{0}' defined for file name is invalid", FileDateFormat),
                            "FileDateFormat", FileDateFormat));
                        result = false;
                    }
                    throw;
                }
            }

            return result;
        }
    }
}
