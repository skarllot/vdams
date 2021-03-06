﻿// EventId.cs
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

using System;

namespace vdams
{
    class EventId : SklLib.Diagnostics.EventId
    {
        // Service related codes (0-9)

        // Configuration file related codes (10-29)
        public static readonly EventId ConfigFileInvalid = new EventId(14);

        // Assort operation related codes (30-49)
        //public static readonly EventId AssortPathValidationError = new EventId(30);
        public static readonly EventId AssortCompleted = new EventId(31);
        public static readonly EventId AssortErrorCreateHardLink = new EventId(32);
        public static readonly EventId AssortFileAccessError = new EventId(33);

        public EventId(ushort value) : base(value) { }

    }
}
