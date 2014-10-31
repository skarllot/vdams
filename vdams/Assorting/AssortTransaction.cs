// AssortTransaction.cs
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
        string path;
        bool isRunning;

        public AssortTransaction(string path)
        {
            this.path = path;
            this.isRunning = true;
        }

        public bool IsRunning
        {
            get
            {
                if (locker == null)
                    return false;

                lock (locker) {
                    return isRunning;
                }
            }
        }

        public object Locker { get { return locker; } }
        public string Path { get { return path; } }

        public void Terminate()
        {
            lock (locker) {
                isRunning = false;
                locker = null;
            }
        }
    }
}
