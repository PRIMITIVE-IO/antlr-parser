using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.JavaScript
{
    public static class RegexBasedJavaScriptMethodBodyRemover
    {
        //matches functions having open curly braces, like: "function f(x, y) {"
        //group 3 matches spaces before the last curly
        //group 4 matches the last curly
        /*
         test cases for regex:
            function f(_x_y){
            function* fff(x, y, z =) {
            f(){
            f (a:{b,c,[d,...rest]}){
            function generateAssets(repo, outputName, commit = """", diffs = [], iGenerateDB = false, createDBcallback = null) {
        */
        static readonly Regex KotlinFunctionDeclarationRegex = new(
            @"(function\*?)?\s*(\w)+\s*\([\w,\s,\,:,\[,\],\{,\},\.,=""]*\)(\s*)(\{)");

        /// <summary>
        /// Identifies a function or method body that is defined with opening and closing curly braces, and removes the
        /// source code from within the braces. In the <see cref="MethodBodyRemovalResult"/>, both the shortened source
        /// code and also a dictionary for the indices of the removed source code is returned to later be re-inserted
        /// into the <see cref="SourceCodeSnippet"/>s within <see cref="MethodInfo"/>s.
        /// </summary>
        public static List<Tuple<int, int>> FindBlocksToRemove(string source)
        {
            IEnumerable<Match> matches = KotlinFunctionDeclarationRegex.Matches(source).Cast<Match>();

            List<Tuple<int, int>> blocksToRemove = matches.Select(currentMatch =>
            {
                int openedCurlyPosition = currentMatch.Groups[4].Index;
                int closedCurlyPosition = -1;
                try
                {
                    closedCurlyPosition = StringUtil.ClosedCurlyPosition(source, openedCurlyPosition);
                }
                catch (Exception e)
                {
                    PrimitiveLogger.Logger.Instance().Error("could not find closed curly position");
                    return new Tuple<int, int>(0, 0);
                }

                int afterMethodDeclarationPosition = currentMatch.Groups[3].Index;

                return new Tuple<int, int>(afterMethodDeclarationPosition, closedCurlyPosition);
            }).ToList();

            return blocksToRemove;
        }
    }
}