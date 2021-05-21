using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.C
{
    public static class RegexBasedCMethodBodyRemover
    {
        //matches functions having open curly braces, like: "fun <T> `my f_u-nc`(a:B<C[]>, b:C, d:()->E?):Z<T>? {"
        //group 2 matches spaces before the last curly
        //group 3 matches the last curly
        /*
         raw regex:
            \w+(\s*\[\s*\])?\s+\w+\s*\([\w,\,\s,\[,\]]*\)(\s*)(\{)
         test cases for regex:
            void f(){
            void f(s[] a, b c){
            int a ( x x, y y) {
            int[] a ( x x, y y) {
        */
        static readonly Regex CFunctionDeclarationRegex = new Regex(
            "\\w+(\\s*\\[\\s*\\])?\\s+\\w+\\s*\\([\\w,\\,\\s,\\[,\\]]*\\)(\\s*)(\\{)");

        /// <summary>
        /// Identifies a function or method body that is defined with opening and closing curly braces, and removes the
        /// source code from within the braces. In the <see cref="MethodBodyRemovalResult"/>, both the shortened source
        /// code and also a dictionary for the indices of the removed source code is returned to later be re-inserted
        /// into the <see cref="SourceCodeSnippet"/>s within <see cref="MethodInfo"/>s.
        /// </summary>
        public static ImmutableList<Tuple<int, int>> FindBlocksToRemove(string source)
        {
            ImmutableList<Tuple<int,int>> blocksToRemove = CFunctionDeclarationRegex.Matches(source).Select(currentMatch =>
            {
                int openedCurlyPosition = currentMatch.Groups[3].Index;
                int closedCurlyPosition = StringUtil.ClosedCurlyPosition(source, openedCurlyPosition);

                int afterMethodDeclarationPosition = currentMatch.Groups[2].Index;

                return new Tuple<int, int>(afterMethodDeclarationPosition, closedCurlyPosition);
            }).ToImmutableList();

            return blocksToRemove;
        }
    }
}