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
        [Required]
        public Target Target { get; set; }
        public string FileDateFormat { get; set; }

        public bool IsValid()
        {
            if (Target == null)
                return false;
            if (!Target.IsValid())
                return false;

            if (FileDateFormat != null) {
                try { DateTime.Now.ToString(FileDateFormat); }
                catch (Exception e) {
                    if (e is FormatException || e is ArgumentOutOfRangeException)
                        return false;
                    throw;
                }
            }

            return true;
        }
    }
}
