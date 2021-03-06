﻿// AssortTransaction.cs
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

namespace vdams.Assorting
{
    class AssortTransaction
    {
        object locker = new object();
        object internalLocker = new object();
        Configuration.FileList config;
        bool isRunning;

        public AssortTransaction(Configuration.FileList cfgFilelist)
        {
            this.config = cfgFilelist;
            this.isRunning = true;
        }

        public bool IsRunning
        {
            get
            {
                lock (internalLocker) {
                    if (locker == null)
                        return false;

                    return isRunning;
                }
            }
        }

        public object Locker { get { return locker; } }
        public Configuration.FileList Configuration { get { return config; } }

        public void Terminate()
        {
            lock (internalLocker) {
                isRunning = false;
                locker = null;
            }
        }
    }
}
