using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace vdams
{
    public static class EncodingParser
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

        public static int? GetCodePage(string name)
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

        public static Encoding GetEncodingInstance(string name, bool bom)
        {
            if (string.IsNullOrWhiteSpace(name))
                return System.Text.Encoding.GetEncoding(0);

            int codePage = 0;

            if (!int.TryParse(name, out codePage))
                codePage = GetCodePage(name) ?? -1;
            if (codePage == -1)
                return null;

            return GetEncodingInstance(codePage, bom);
        }

        public static Encoding GetEncodingInstance(int codepage, bool bom)
        {
            System.Text.Encoding result = null;
            switch (codepage) {
                case 1200:
                    result = new System.Text.UnicodeEncoding(false, bom);
                    break;
                case 1201:
                    result = new System.Text.UnicodeEncoding(true, bom);
                    break;
                case 12000:
                    result = new System.Text.UTF32Encoding(false, bom);
                    break;
                case 12001:
                    result = new System.Text.UTF32Encoding(true, bom);
                    break;
                case 65001:
                    result = new System.Text.UTF8Encoding(bom);
                    break;
                default:
                    try { result = System.Text.Encoding.GetEncoding(codepage); }
                    catch { }
                    break;
            }

            return result;
        }
    }
}
