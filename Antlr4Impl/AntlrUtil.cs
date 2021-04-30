using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace antlr_parser.Antlr4Impl
{
    public class ErrorListener : BaseErrorListener
    {
        public void SyntaxError(IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine,
            string msg, RecognitionException e)
        {
            Console.WriteLine("{0}: line {1}/column {2} {3}", e, line, charPositionInLine, msg);
        }
    }

    public static class OriginalText
    {
        /// <summary>
        /// https://stackoverflow.com/questions/26524302/how-to-preserve-whitespace-when-we-use-text-attribute-in-antlr4
        ///
        /// Explanation: Get the first token, get the last token, and get the text from the input stream between the
        /// first char of the first token and the last char of the last token.
        /// </summary>
        /// <param name="context">The context that contains the tokens</param>
        /// <returns>Original new-lined and indented text</returns>
        public static string GetFullText(this ParserRuleContext context)
        {
            if (context.Start == null ||
                context.Stop == null ||
                context.Start.StartIndex < 0 ||
                context.Stop.StopIndex < 0)
            {
                // Fallback
                return context.GetText();
            }

            return context.Start.InputStream.GetText(Interval.Of(
                context.Start.StartIndex,
                context.Stop.StopIndex));
        }
    }

    public static class StringUtil
    {
        public static string TrimIndent(string s)
        {
            string[] lines = s.Split('\n');

            IEnumerable<int> firstNonWhitespaceIndices = lines
                .Skip(1)
                .Where(it => it.Trim().Length > 0)
                .Select(IndexOfFirstNonWhitespace);

            int firstNonWhitespaceIndex;

            if (firstNonWhitespaceIndices.Any()) firstNonWhitespaceIndex = firstNonWhitespaceIndices.Min();
            else firstNonWhitespaceIndex = -1;

            if (firstNonWhitespaceIndex == -1) return s;

            IEnumerable<string> unindentedLines = lines.Select(it => UnindentLine(it, firstNonWhitespaceIndex));
            return String.Join("\n", unindentedLines);
        }

        static string UnindentLine(string line, int firstNonWhitespaceIndex)
        {
            if (firstNonWhitespaceIndex < line.Length)
            {
                if (line.Substring(0, firstNonWhitespaceIndex).Trim().Length != 0)
                {
                    //indentation contains some chars (if this is first line)
                    return line;
                }

                return line.Substring(firstNonWhitespaceIndex, line.Length - firstNonWhitespaceIndex);
            }

            return line.Trim().Length == 0 ? "" : line;
        }

        static int IndexOfFirstNonWhitespace(string s)
        {
            char[] chars = s.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] != ' ' && chars[i] != '\t') return i;
            }

            return -1;
        }
    }
}