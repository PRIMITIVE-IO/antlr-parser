using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.C
{
    public static class RegexBasedCppMethodBodyRemover
    {
        //matches functions having open curly braces, like: "fun <T> `my f_u-nc`(a:B<C[]>, b:C, d:()->E?):Z<T>? {"
        //group 4 matches spaces before the last curly
        //group 5 matches the last curly
        /*
         raw regex:
            \w+(\s*\[\s*\])?\s+(\w+::)?\w+\s*\([\w,\,\s,\[,\],\*,\&,:]*\)(\s+const)?(\s*)(\{)
         test cases for regex:
            void f(){
            void f(s[] a, b c){
            int a ( x x, y y) {
            int[] a ( x x, y y) {
            fsw_status_t fsw_mount(void *host_data, struct fsw_host_table **host_table) {
            void X::y() {
            void PSBTOutput::FillSignatureData(SignatureData& sigdata) const{
            bool PSBTInput::IsNull() const{
            bool DecodeRawPSBT(PartiallySignedTransaction& psbt, const std::string& tx_data, std::string& error){
        */
        static readonly Regex CFunctionDeclarationRegex = new Regex(
            "\\w+(\\s*\\[\\s*\\])?\\s+(\\w+::)?\\w+\\s*\\([\\w,\\,\\s,\\[,\\],\\*,\\&,:]*\\)(\\s+const)?(\\s*)(\\{)");

        /// <summary>
        /// Identifies a function or method body that is defined with opening and closing curly braces, and removes the
        /// source code from within the braces (but keeps curly braces themselves). In the
        /// <see cref="MethodBodyRemovalResult"/>, both the shortened source code and also a dictionary for the indices
        /// of the removed source code is returned to later be re-inserted into the <see cref="SourceCodeSnippet"/>s
        /// within <see cref="MethodInfo"/>s.
        /// </summary>
        public static List<Tuple<int, int>> FindBlocksToRemove(string source)
        {
            IEnumerable<Match> matches = CFunctionDeclarationRegex.Matches(source).Cast<Match>();

            List<Tuple<int, int>> blocksToRemove = matches.Select(currentMatch =>
            {
                int openedCurlyPosition = currentMatch.Groups[5].Index;
                try
                {
                    int closedCurlyPosition = StringUtil.ClosedCurlyPosition(source, openedCurlyPosition);
                    return new Tuple<int, int>(openedCurlyPosition + 1, closedCurlyPosition - 1);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return null;
                }
            })
                .Where(it => it != null)
                .ToList();

            return blocksToRemove;
        }
    }
}