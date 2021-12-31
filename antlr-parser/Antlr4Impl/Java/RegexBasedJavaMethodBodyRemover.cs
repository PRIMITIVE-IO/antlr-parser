using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.Java
{
    public class RegexBasedJavaMethodBodyRemover
    {
        //matches functions having open curly braces, like: "function f(x, y) {"
        //group 10 matches the last curly
        /*
         test cases:
            void main() {
            public static void main(X x, Yy) {
            public static <T> void main(X x, Yy) {
            final public string f(s[] x, List<Z> a){              
        */

        readonly static Regex MethodDeclarationRegex = new Regex(
            @"(public |private |protected |static |final )?(public |private |protected |static |final )?(public |private |protected |static |final )?(public |private |protected |static |final )?(<[A-Za-z0-9_]*> )?([A-Za-z0-9_\[\]<>]*) ([A-Za-z0-9_]*)(\s)*\(([A-Za-z\[\]<>,\s]*)\)\s*(\{)"
        );

        /// <summary>
        /// Identifies a function or method body that is defined with opening and closing curly braces, and removes the
        /// source code from within the braces. In the <see cref="MethodBodyRemovalResult"/>, both the shortened source
        /// code and also a dictionary for the indices of the removed source code is returned to later be re-inserted
        /// into the <see cref="SourceCodeSnippet"/>s within <see cref="MethodInfo"/>s.
        /// </summary>
        public static List<Tuple<int, int>> FindBlocksToRemove(string source)
        {
            IEnumerable<Match> matches = MethodDeclarationRegex.Matches(source).Cast<Match>();

            List<Tuple<int, int>> blocksToRemove = matches.Select(currentMatch =>
                {
                    int openedCurlyPosition = currentMatch.Groups[10].Index;
                    int closedCurlyPosition = StringUtil.ClosedCurlyPosition(source, openedCurlyPosition);


                    int start = openedCurlyPosition + 1;
                    int end = closedCurlyPosition - 1;

                    if (end < start) return null;

                    return new Tuple<int, int>(start, end);
                })
                .Where(it => it != null)
                .ToList();

            return blocksToRemove;
        }
    }
}