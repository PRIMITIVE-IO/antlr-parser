using System;
using System.Collections.Generic;
using System.Linq;

namespace antlr_parser.Antlr4Impl
{
    /// <summary>
    /// Removes everything in curly braces except class bodies.
    /// for this input:
    /// class A {
    ///     { REMOVE }
    ///     f(x) { REMOVE }
    ///     class B {
    ///         g(y) { REMOVE }
    ///     }
    /// }
    /// 
    /// will produce list of start and end positions for all `{ REMOVE }` blocks
    /// </summary>
    public static class ClassBasedMethodBodyRemover
    {
        public static List<Tuple<int, int>> FindBlocksToRemove(string s)
        {
            List<Tuple<int, int>> removeFromTo = new List<Tuple<int, int>>();
            bool keepNextCurly = false;
            int lastNonEmptyCharIdx = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '{')
                {
                    if (keepNextCurly)
                    {
                        keepNextCurly = false;
                    }
                    else
                    {
                        int closedCurlyPosition = StringUtil.ClosedCurlyPosition(s, i);
                        removeFromTo.Add(new Tuple<int, int>(lastNonEmptyCharIdx + 1, closedCurlyPosition));
                        i = closedCurlyPosition; //jump to the end of removed block
                    }
                }

                if (IsCompletedClassWord(s, i))
                {
                    keepNextCurly = true;
                }

                if (!IsEmptyChar(s[i])) lastNonEmptyCharIdx = i;
            }

            return removeFromTo.ToList();
        }

        static bool IsCompletedClassWord(string s, int i)
        {
            if ( 5 <= i && i + 1 < s.Length)
            {
                char nextChar = s[i + 1];
                if (5 < i)
                {
                    char prevChar = s[i - 5];
                    if (!IsEmptyChar(prevChar)) return false;
                }
                return (nextChar == ' ' || nextChar == '\t' || nextChar == '\n' || nextChar == '\r' || nextChar == '{')
                       && s[i] == 's' && s.Substring(i - 4, 5) == "class";
            }

            return false;
        }

        static bool IsEmptyChar(char c)
        {
            return c == ' ' || c == '\t' || c == '\n' || c == '\r';
        }
    }
}