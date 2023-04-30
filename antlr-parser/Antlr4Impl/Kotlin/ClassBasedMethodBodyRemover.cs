using System;
using System.Collections.Generic;
using System.Linq;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.Kotlin
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
            List<Tuple<int, int>> removeFromTo = new();
            bool keepNextCurly = false;
            int lastNonEmptyCharIdx = -1;
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
                        int removeFrom = lastNonEmptyCharIdx == -1 ? i : lastNonEmptyCharIdx + 1;// should remove spaces between `) {`. Or start removal from `{` if there is no symbols before
                        removeFromTo.Add(new Tuple<int, int>(removeFrom, closedCurlyPosition));
                        i = closedCurlyPosition; //jump to the end of removed block
                    }
                }

                if (IsCompletedClassWord(s, i) || IsCompletedObjectWord(s, i) || IsCompletedInterfaceWord(s, i))
                {
                    keepNextCurly = true;
                }

                if (!IsEmptyChar(s[i]))
                {
                    lastNonEmptyCharIdx = i;
                }
            }

            return removeFromTo.ToList();
        }

        static bool IsCompletedClassWord(string s, int i)
        {
            if (i < 4 || i == s.Length - 1) return false; // start of file or end of file
            if (5 <= i && !IsEmptyChar(s[i - 5])) return false; // 'class' is part of bigger name 

            char nextChar = s[i + 1];

            return (nextChar == ' ' || nextChar == '\t' || nextChar == '\n' || nextChar == '\r' || nextChar == '{')
                   && s[i] == 's' && s.Substring(i - 4, 5) == "class";
        }

        static bool IsCompletedObjectWord(string s, int i)
        {
            if (i < 5 || i == s.Length - 1) return false; // start of file or end of file
            if (6 <= i && !IsEmptyChar(s[i - 6])) return false; // 'class' is part of bigger name 

            char nextChar = s[i + 1];

            return (nextChar == ' ' || nextChar == '\t' || nextChar == '\n' || nextChar == '\r' || nextChar == '{')
                   && s[i] == 't' && s.Substring(i - 5, 6) == "object";
        }

        static bool IsCompletedInterfaceWord(string s, int i)
        {
            if (i < 8 || i == s.Length - 1) return false; // start of file or end of file
            if (9 <= i && !IsEmptyChar(s[i - 9])) return false; // 'class' is part of bigger name 

            char nextChar = s[i + 1];

            return (nextChar == ' ' || nextChar == '\t' || nextChar == '\n' || nextChar == '\r' || nextChar == '{')
                   && s[i] == 'e' && s.Substring(i - 8, 9) == "interface";
        }

        static bool IsEmptyChar(char c)
        {
            return c == ' ' || c == '\t' || c == '\n' || c == '\r';
        }
    }
}