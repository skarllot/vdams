// Configuration.cs
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
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace vdams.Configuration
{
    class Configuration : IValidatable
    {
        int dateDepth = 1;

        public List<Assort> Assort { get; set; }
        public int DateDepth { get { return dateDepth; } set { dateDepth = value; } }
        public FileList FileList { get; set; }
        public List<Monitor> Monitor { get; set; }
        [Required]
        public Time ScheduleTime { get; set; }
        [Required]
        public List<Target> Targets { get; set; }

        public bool IsValid()
        {
            if (Assort != null) {
                foreach (var item in Assort) {
                    if (!item.IsValid())
                        return false;
                }
            }
        }
    }
}
