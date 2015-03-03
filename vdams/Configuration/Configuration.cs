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
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace vdams.Configuration
{
    class Configuration : IValidatable
    {
        public List<Assort> Assort { get; set; }
        public FileList FileList { get; set; }
        public List<Monitor> Monitor { get; set; }
        public List<Target> Targets { get; set; }

        public Configuration()
        {
            Assort = new List<Assort>();
            Monitor = new List<Monitor>();
            Targets = new List<Target>();
        }

        public IEnumerable<Assorting.DirectoryAssorter> GetDirectoryAssorters()
        {
            foreach (var item in Assort) {
                yield return new Assorting.DirectoryAssorter(item);
            }
        }

        public IEnumerable<Monitoring.DirectoryMonitor> GetDirectoryMonitors()
        {
            foreach (var item in Monitor) {
                yield return new Monitoring.DirectoryMonitor(item);
            }
        }

        public IEnumerable<Targeting> GetTargets()
        {
            foreach (var item in Targets) {
                yield return new Targeting(item);
            }
        }

        public static Configuration LoadFile(string file)
        {
            Configuration result = null;
            var reader = new System.IO.StreamReader(file, true);
            var deserializer = new Deserializer(namingConvention: new CamelCaseNamingConvention());
            try { result = deserializer.Deserialize<Configuration>(reader); }
            catch (Exception e) { throw e; }
            finally {
                reader.Close();
                reader.Dispose();
            }

            return result;
        }

        public bool Validate(Action<InvalidEventArgs> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            bool result = true;

            if (Targets == null || Targets.Count == 0) {
                action(new InvalidEventArgs(
                    "At least one target should be defined",
                    "Targets", null));
                result = false;
            }
            else {
                foreach (var item in Targets) {
                    if (!item.Validate(action))
                        result = false;
                }

                var uniqueness =
                    from a in Targets
                    group a by a.Name into grp
                    where grp.Count() > 1
                    select grp.Key;
                if (uniqueness.Count() != 0) {
                    foreach (var item in uniqueness) {
                        action(new InvalidEventArgs(
                            string.Format("The target name '{0}' is not unique", item),
                            "Targets", item));
                    }
                    result = false;
                }
            }
            if (Assort != null && Assort.Count > 0) {
                if (FileList == null) {
                    action(new InvalidEventArgs(
                        "The file-list configuration was not found, and is required when assorting is defined",
                        "FileList", null));
                    result = false;
                }

                foreach (var item in Assort) {
                    if (!item.Validate(action))
                        result = false;

                    if (!ValidateTarget(item, action))
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

                    if (!ValidateTarget(item, action))
                        result = false;
                }
            }

            return result;
        }

        private bool ValidateTarget(
            ITargetDependency source,
            Action<InvalidEventArgs> action)
        {
            bool result = true;
            if (Targets.Count(a => a.Name == source.Target) == 0) {
                action(new InvalidEventArgs(
                    string.Format("The target name '{0}' does not exist", source.Target),
                    "Target", source.Target));
                result = false;
            }

            return result;
        }
    }
}
