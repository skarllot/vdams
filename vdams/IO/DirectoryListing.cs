// DirectoryListing.cs
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
using System.IO;
using System.Linq;
using System.Text;

namespace vdams.IO
{
    static class DirectoryListing
    {
        public static IEnumerable<string> GetFiles(string path)
        {
            string[] list = new string[0];
            try { list = Directory.GetFiles(path); }
            catch (UnauthorizedAccessException) { }
            foreach (var item in list) {
                yield return item;
            }

            list = new string[0];
            try { list = Directory.GetDirectories(path); }
            catch (UnauthorizedAccessException) { }
            foreach (var item in list) {
                foreach (var retItem in GetFiles(item)) {
                    yield return retItem;
                }
            }
        }

        public static long GetTotalSize(string path)
        {
            long size = 0;
            foreach (var item in GetFiles(path)) {
                size += new FileInfo(item).Length;
            }
            return size;
        }
    }
}
