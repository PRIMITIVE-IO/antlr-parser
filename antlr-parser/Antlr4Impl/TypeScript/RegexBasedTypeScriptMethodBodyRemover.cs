using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.TypeScript
{
    public static class RegexBasedTypeScriptMethodBodyRemover
    {
        //matches functions having open curly braces, like: "function f(x, y) {"
        //group 3 matches spaces before the last curly
        //group 4 matches the last curly
        /*
         raw regex:
            (function\*?)?\s*(\w)+\s*\([\w,\s,\,:,\[,\],\{,\},\.,=]*\)(\s*)(\{)
         test cases for regex:
            function f(_x_y){
            function* fff(x, y, z =) {
            f(){
            f (a:{b,c,[d,...rest]}){
        */
        static readonly Regex TypescriptFunctionDeclarationRegex = new Regex(
            "(function\\*?)?\\s*(\\w)+\\s*\\([\\w,\\s,\\,:,\\[,\\],\\{,\\},\\.,=]*\\)(\\s*)(\\{)");

        /// <summary>
        /// Identifies a function or method body that is defined with opening and closing curly braces, and removes the
        /// source code from within the braces. In the <see cref="MethodBodyRemovalResult"/>, both the shortened source
        /// code and also a dictionary for the indices of the removed source code is returned to later be re-inserted
        /// into the <see cref="SourceCodeSnippet"/>s within <see cref="MethodInfo"/>s.
        /// </summary>
        public static List<Tuple<int, int>> FindBlocksToRemove(string source)
        {
            IEnumerable<Match> matches = TypescriptFunctionDeclarationRegex.Matches(source).Cast<Match>();

            List<Tuple<int, int>> blocksToRemove = matches.Select(currentMatch =>
            {
                int openedCurlyPosition = currentMatch.Groups[4].Index;
                int closedCurlyPosition = StringUtil.ClosedCurlyPosition(source, openedCurlyPosition);

                int openCurlyPosition = currentMatch.Groups[4].Index;

                int start = openCurlyPosition + 1;
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