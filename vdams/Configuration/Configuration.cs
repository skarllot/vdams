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
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace vdams.Configuration
{
    class Configuration : IValidatable
    {
        int dateDepth = 1;

        public List<Assort> Assort { get; set; }
        public int DateDepth { get { return dateDepth; } set { dateDepth = value; } }
        public FileList FileList { get; set; }
        public List<Monitor> Monitor { get; set; }
        public Time? ScheduleTime { get; set; }
        public List<Target> Targets { get; set; }

        public IEnumerable<Assorting.DirectoryAssorter> GetDirectoryAssorters()
        {
            foreach (var item in Assort) {
                yield return new Assorting.DirectoryAssorter(item);
            }
        }

        public static Configuration LoadFile(string file)
        {
            Configuration result = null;
            var reader = new System.IO.StreamReader(file, true);
            var deserializer = new Deserializer(namingConvention: new CamelCaseNamingConvention());
            try { result = deserializer.Deserialize<Configuration>(reader); }
            catch { }
            reader.Close();
            reader.Dispose();

            return result;
        }

        public bool Validate(Action<InvalidEventArgs> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            bool result = true;

            if (Assort != null && Assort.Count > 0) {
                if (FileList == null) {
                    action(new InvalidEventArgs(
                        "The file-list configuration was not found, and is required when assorting is defined",
                        "FileList", null));
                    result = false;
                }

                foreach (var item in Assort) {
                    if (!item.IsValid())
                        result = false;
                }
            }

            if (FileList != null) {
                if (Assort == null || Assort.Count < 1) {
                    action(new InvalidEventArgs(
                        "The file-list configuration is defined but assorting targets not",
                        "Assort", null));
                    result = false;
                }

                if (!FileList.Validate(action))
                    result = false;
            }

            if (Monitor != null && Monitor.Count > 0) {
                foreach (var item in Monitor) {
                    if (!item.Validate(action))
                        result = false;
                }
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
