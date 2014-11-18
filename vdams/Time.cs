// Time.cs
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
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace vdams
{
    public struct Time : IYamlSerializable, IComparable<DateTime>, IComparable<Time>
    {
        #region Fields

        const string FORMAT_BASIC = "HH:mm";
        const string FORMAT_COMPLETE = "HH:mm:ss";
        const string FORMAT_DETAILED = "HH:mm:ss.fff";
        DateTime dt;

        #endregion

        #region Constructors

        public Time(int hour, int minute, int second)
        {
            dt = new DateTime(1, 1, 1, hour, minute, second);
        }

        public Time(int hour, int minute, int second, int millisecond)
        {
            dt = new DateTime(1, 1, 1, hour, minute, second, millisecond);
        }

        #endregion

        #region Operators

        public static bool operator !=(Time t1, Time t2)
        {
            return t1.dt != t2.dt;
        }

        public static bool operator <(Time t1, Time t2)
        {
            return t1.dt < t2.dt;
        }

        public static bool operator <=(Time t1, Time t2)
        {
            return t1.dt <= t2.dt;
        }

        public static bool operator ==(Time t1, Time t2)
        {
            return t1.dt == t2.dt;
        }

        public static bool operator >(Time t1, Time t2)
        {
            return t1.dt > t2.dt;
        }

        public static bool operator >=(Time t1, Time t2)
        {
            return t1.dt >= t2.dt;
        }

        #endregion

        #region Properties

        public int Hour { get { return dt.Hour; } set { dt = new DateTime(1, 1, 1, value, dt.Minute, dt.Second, dt.Millisecond); } }
        public int Millisecond { get { return dt.Millisecond; } set { dt = new DateTime(1, 1, 1, dt.Hour, dt.Minute, dt.Second, value); } }
        public int Minute { get { return dt.Minute; } set { dt = new DateTime(1, 1, 1, dt.Hour, value, dt.Second, dt.Millisecond); } }
        public int Second { get { return dt.Second; } set { dt = new DateTime(1, 1, 1, dt.Hour, dt.Minute, value, dt.Millisecond); } }

        #endregion

        #region Methods

        public int CompareTo(DateTime dt, TimeFields fields)
        {
            if ((fields & TimeFields.Millisecond) == 0
                && (fields & TimeFields.Second) == 0
                && (fields & TimeFields.Minute) == 0
                && (fields & TimeFields.Hour) == 0) {
                    throw new ArgumentException("There is no time field to compare");
            }

            int ms = 0, sec = 0, min = 0, h = 0;
            if ((fields & TimeFields.Millisecond) > 0)
                ms = this.dt.Millisecond.CompareTo(dt.Millisecond);
            if ((fields & TimeFields.Second) > 0)
                sec = this.dt.Second.CompareTo(dt.Second);
            if ((fields & TimeFields.Minute) > 0)
                min = this.dt.Minute.CompareTo(dt.Minute);
            if ((fields & TimeFields.Hour) > 0)
                h = this.dt.Hour.CompareTo(dt.Hour);

            if (h != 0)
                return h;
            if (min != 0)
                return min;
            if (sec != 0)
                return sec;
            return ms;
        }

        public int CompareTo(Time t, TimeFields fields)
        {
            return CompareTo(t.dt, fields);
        }

        private void ParseFromString(string s)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            if (string.IsNullOrWhiteSpace(s))
                throw new ArgumentException("The parameter s is empty", "s");

            dt = DateTime.ParseExact(s,
                new string[] { FORMAT_DETAILED, FORMAT_COMPLETE, FORMAT_BASIC },
                null, System.Globalization.DateTimeStyles.None);
            dt = new DateTime(1, 1, 1, dt.Hour, dt.Minute, dt.Second, dt.Millisecond);
        }

        public static Time Parse(string s)
        {
            Time result = new Time();
            result.ParseFromString(s);
            return result;
        }

        #endregion

        #region Object

        public override int GetHashCode()
        {
            return ((dt.Hour * 1000 * 60 * 60)
                + (dt.Minute * 1000 * 60)
                + (dt.Second * 1000)
                + dt.Millisecond);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Time))
                return false;

            return (this == (Time)obj);
        }

        public override string ToString()
        {
            if (dt.Millisecond > 0)
                return dt.ToString(FORMAT_DETAILED);
            else if (dt.Second > 0)
                return dt.ToString(FORMAT_COMPLETE);
            else
                return dt.ToString(FORMAT_BASIC);
        }

        #endregion

        #region IYamlSerializable

        public void ReadYaml(YamlDotNet.Core.IParser parser)
        {
            var reader = new EventReader(parser);
            var scalar = reader.Expect<Scalar>();
            this.ParseFromString(scalar.Value);
        }

        public void WriteYaml(YamlDotNet.Core.IEmitter emitter)
        {
            emitter.Emit(new Scalar(this.ToString()));
        }

        #endregion

        #region IComparable

        int IComparable<DateTime>.CompareTo(DateTime other)
        {
            return CompareTo(other, TimeFields.HourMinuteSecondMillisecond);
        }

        int IComparable<Time>.CompareTo(Time other)
        {
            return CompareTo(other.dt, TimeFields.HourMinuteSecondMillisecond);
        }

        #endregion
    }
}
