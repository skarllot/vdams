// MonitorTransaction.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace vdams.Monitoring
{
    class MonitorTransaction
    {
        object locker = new object();
        object internalLocker = new object();
        bool isRunning;
        Dictionary<string, CameraInfo> cameraList;

        public MonitorTransaction()
        {
            this.isRunning = true;
            this.cameraList = new Dictionary<string, CameraInfo>();
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

        public CameraInfo this[string name]
        {
            get
            {
                CameraInfo result;
                if (!cameraList.TryGetValue(name, out result)) {
                    result = new CameraInfo { Name = name };
                    cameraList.Add(name, result);
                }

                return result;
            }
            set
            {
                if (!cameraList.ContainsKey(name))
                    cameraList.Add(name, value);
                else
                    cameraList[name] = value;
            }
        }

        public void Terminate()
        {
            lock (internalLocker) {
                isRunning = false;
                locker = null;
            }
        }
    }
}
