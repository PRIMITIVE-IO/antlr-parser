using System;
using System.Linq;

namespace antlr_parser.Antlr4Impl.Kotlin
{
    public static class StringUtil
    {
        public static string TrimIndent(string s)
        {
            string[] lines = s.Split("\n");

            var firstNonWhitespaceIndices = lines
                .Skip(1)
                .Where(it => it.Trim().Length > 0)
                .Select(it => IndexOfFirstNonWhitespace(it));

            int firstNonWhitespaceIndex;

            if (firstNonWhitespaceIndices.Any()) firstNonWhitespaceIndex = firstNonWhitespaceIndices.Min();
            else firstNonWhitespaceIndex = -1;

            if (firstNonWhitespaceIndex == -1) return s;

            var unindentedLines = lines.Select(it => UnindentLine(it, firstNonWhitespaceIndex));
            return String.Join("\n", unindentedLines);
        }

        static string UnindentLine(string line, int firstNonWhitespaceIndex)
        {
            if (firstNonWhitespaceIndex < line.Length)
            {
                if (line[..firstNonWhitespaceIndex].Trim().Length != 0) return line; //indentation contains some chars (if this is first line)
                return line.Substring(firstNonWhitespaceIndex, line.Length - firstNonWhitespaceIndex);
            }
            return line.Trim().Length == 0 ? "" : line;
        }

        static int IndexOfFirstNonWhitespace(string s)
        {
            char[] chars = s.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                if (chars[i] != ' ' && chars[i] != '\t') return i;
            }

            return -1;
        }
    }
}