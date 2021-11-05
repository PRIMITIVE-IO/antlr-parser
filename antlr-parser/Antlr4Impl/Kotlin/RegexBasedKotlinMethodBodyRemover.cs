using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.Kotlin
{
    public static class RegexBasedKotlinMethodBodyRemover
    {
        //matches functions having open curly braces, like: "fun <T> `my f_u-nc`(a:B<C[]>, b:C, d:()->E?):Z<T>? {"
        //group 4 matches spaces before the last curly
        //group 5 matches the last curly
        /*
         raw regex:
            fun\s+[\w, <, >, \[,\],\-,_,`,\.]*\s*\((\s*(vararg\s*)?\w*\s*:\s*[\w, <, >, \[, \],\?,=,\(,\),->,\.]*\s*,?)*\)(\s*:\s*[<,>,\[,\],\w,\?,\., ]*[\w,>,?])?(\s*)({)
         test cases for regex:
            fun x(a:Int, b:I , c:x ) : Y {
            fun x(a:In):Y{
            fun a a a`(s:Yn,b:Y){
            fun a(){
            fun a(s:Y , x:Y):Y{
            fun <T[]> f(x:T<E[]>):F<>{
            fun <T,z,> `my f_un-c`(a:B<C[]>?, b:C=10, e:()->E):Z<T>?    {
            fun `cleanup`() {
            fun all(vararg r: () -> Unit) {
            fun Array<String>.extract(param: String): List<String> {
            fun analyzeAndWriteToDb(    projectRoot: Path,    mode: Mode = Mode.AUTO) {
            fun f():A<B.c>? {
            fun f(): A<B, C> {
        */
        static readonly Regex KotlinFunctionDeclarationRegex = new Regex(
            "fun\\s+[\\w, <, >, \\[,\\],\\-,_,`,\\.]*\\s*\\((\\s*(vararg\\s*)?\\w*\\s*:\\s*[\\w, <, >, \\[, \\],\\?,=,\\(,\\),->,\\.]*\\s*,?)*\\)(\\s*:\\s*[<,>,\\[,\\],\\w,\\?,\\., ]*[\\w,>,?])?(\\s*)({)");

        /// <summary>
        /// Identifies a function or method body that is defined with opening and closing curly braces, and removes the
        /// source code from within the braces. In the <see cref="MethodBodyRemovalResult"/>, both the shortened source
        /// code and also a dictionary for the indices of the removed source code is returned to later be re-inserted
        /// into the <see cref="SourceCodeSnippet"/>s within <see cref="MethodInfo"/>s.
        /// </summary>
        public static List<Tuple<int, int>> FindBlocksToRemove(string source)
        {
            IEnumerable<Match> matches = KotlinFunctionDeclarationRegex.Matches(source).Cast<Match>();

            List<Tuple<int,int>> blocksToRemove = matches.Select(currentMatch =>
            {
                int openedCurlyPosition = currentMatch.Groups[5].Index;
                int closedCurlyPosition = StringUtil.ClosedCurlyPosition(source, openedCurlyPosition);

                int afterMethodDeclarationPosition = currentMatch.Groups[4].Index;

                return new Tuple<int, int>(afterMethodDeclarationPosition, closedCurlyPosition);
            }).ToList();

            return blocksToRemove;
        }
    }
}