using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.CSharp
{
    public static class CSharpMethodBodyRemover
    {
        //matches functions having open curly braces, like: "public void f(string x, int y) {"
        //group 8 matches the last curly
        /*
         test cases:
            public static List<Tuple<int, int>> FindBlocksToRemove(string source) {
            static void Swap<T>(ref T lhs, ref T rhs) {
            public static void TestSwap(){            
        */

        static readonly Regex MethodDeclarationRegex = new Regex(
            @"(public |private |protected |static |readonly )?(public |private |protected |static |readonly )?(public |private |protected |static |readonly )?([A-Za-z0-9_\[\]<>, ]*) ([A-Za-z0-9_]*)([A-Za-z0-9_\[\]<>, ]*)?(\s)*\(([A-Za-z\[\]<>,\s]*)\)\s*(\{)"
        );

        public static List<Tuple<int, int>> FindBlocksToRemove(string source)
        {
            return MethodDeclarationRegex.Matches(source)
                .SelectNotNull(currentMatch =>
                {
                    int openedCurlyPosition = currentMatch.Groups[9].Index;
                    int closedCurlyPosition = StringUtil.ClosedCurlyPosition(source, openedCurlyPosition);


                    int start = openedCurlyPosition + 1;
                    int end = closedCurlyPosition - 1;

                    if (end < start) return null;

                    return new Tuple<int, int>(start, end);
                })
                .ToList();
        }
    }
}