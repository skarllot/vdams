// ConfigFilelistSection.cs
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

using SklLib.Configuration;
using SklLib.IO;
using System.Collections.Generic;
using System.IO;
using System.Security.Permissions;

namespace vdams.Configuration
{
    class ConfigFilelistSection : IniSectionReaderBase
    {
        static readonly KeyValuePair<string, int>[] dictEncoding =
            new KeyValuePair<string, int>[] {
                new KeyValuePair<string, int>("ascii", 20127),
                new KeyValuePair<string, int>("iso88591", 28591),
                new KeyValuePair<string, int>("iso88592", 28592),
                new KeyValuePair<string, int>("iso88593", 28593),
                new KeyValuePair<string, int>("iso88594", 28594),
                new KeyValuePair<string, int>("iso88595", 28595),
                new KeyValuePair<string, int>("iso88596", 28596),
                new KeyValuePair<string, int>("iso88597", 28597),
                new KeyValuePair<string, int>("iso88598", 28598),
                new KeyValuePair<string, int>("iso88599", 28599),
                new KeyValuePair<string, int>("iso885913", 28603),
                new KeyValuePair<string, int>("iso885915", 28605),
                new KeyValuePair<string, int>("unicode", 1200),
                new KeyValuePair<string, int>("unicodefffe", 1201),
                new KeyValuePair<string, int>("usascii", 20127),
                new KeyValuePair<string, int>("utf16", 1200),
                new KeyValuePair<string, int>("utf32", 12000),
                new KeyValuePair<string, int>("utf32be", 12001),
                new KeyValuePair<string, int>("utf7", 65000),
                new KeyValuePair<string, int>("utf8", 65001)
            };

        public ConfigFilelistSection(IniFileReader reader, string section)
            : base(reader, section)
        {
        }

        public bool? BOM { get { return GetBoolean("BOM"); } }
        public string DirPath { get { return GetString("Directory"); } }
        public string Encoding { get { return GetString("Encoding"); } }

        public int? GetCodePageFromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            name = name.Replace("-", "").ToLower();
            foreach (var item in dictEncoding) {
                if (item.Key == name)
                    return item.Value;
            }

            return null;
        }

        public System.Text.Encoding GetEncodingInstance()
        {
            string s = this.Encoding;
            if (string.IsNullOrWhiteSpace(s))
                return System.Text.Encoding.GetEncoding(0);

            int codePage = 0;

            if (!int.TryParse(s, out codePage))
                codePage = GetCodePageFromName(s) ?? -1;
            if (codePage == -1)
                return null;

            System.Text.Encoding result = null;
            switch (codePage) {
                case 1200:
                    if (BOM == null)
                        result = new System.Text.UnicodeEncoding(false, true);
                    else
                        result = new System.Text.UnicodeEncoding(false, BOM.Value);
                    break;
                case 1201:
                    if (BOM == null)
                        result = new System.Text.UnicodeEncoding(true, true);
                    else
                        result = new System.Text.UnicodeEncoding(true, BOM.Value);
                    break;
                case 12000:
                    if (BOM == null)
                        result = new System.Text.UTF32Encoding(false, true);
                    else
                        result = new System.Text.UTF32Encoding(false, BOM.Value);
                    break;
                case 12001:
                    if (BOM == null)
                        result = new System.Text.UTF32Encoding(true, true);
                    else
                        result = new System.Text.UTF32Encoding(true, BOM.Value);
                    break;
                case 65001:
                    if (BOM == null)
                        result = new System.Text.UTF8Encoding();
                    else
                        result = new System.Text.UTF8Encoding(BOM.Value);
                    break;
                default:
                    try { result = System.Text.Encoding.GetEncoding(codePage); }
                    catch { }
                    break;
            }

            return result;
        }

        public bool HasPermissionFileListPath()
        {
            return new FileInfo(DirPath).HasPermission(FileIOPermissionAccess.Write);
        }

        public override bool IsValid()
        {
            if (DirPath == null)
                return false;
            else {
                try {
                    Path.GetFullPath(DirPath);
                    if (!Directory.Exists(DirPath))
                        return false;
                    if (!HasPermissionFileListPath())
                        return false;
                }
                catch { return false; }
            }
            if (GetEncodingInstance() == null)
                return false;

            return true;
        }
    }
}
