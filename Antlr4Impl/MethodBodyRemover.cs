using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl
{
    public static class MethodBodyRemover
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
        public static MethodBodyRemovalResult RemoveMethodBodyWithBraces(string source, SourceCodeLanguage language)
        {
            Regex languageMethodBodyRegex;
            switch (language)
            {
                case SourceCodeLanguage.Java:
                case SourceCodeLanguage.JavaScript:
                case SourceCodeLanguage.TypeScript:
                case SourceCodeLanguage.Cpp:
                case SourceCodeLanguage.C:
                case SourceCodeLanguage.CWithClasses:
                case SourceCodeLanguage.ObjC:
                    // TODO implement regex for each language
                    return new MethodBodyRemovalResult(
                        source, 
                        ImmutableDictionary<int, string>.Empty);
                case SourceCodeLanguage.Kotlin:
                    languageMethodBodyRegex = KotlinFunctionDeclarationRegex;
                    break;
                default:
                    return new MethodBodyRemovalResult(
                        source, 
                        ImmutableDictionary<int, string>.Empty);
            }

            Match currentMatch = languageMethodBodyRegex.Match(source);
            string sourceAccumulator = source;
            Dictionary<int, string> indexToRemovedString = new Dictionary<int, string>();
            while (currentMatch.Success)
            {
                // TODO this is still specific to Kotlin code
                int openedCurlyPosition = currentMatch.Groups[5].Index;
                int closedCurlyPosition = ClosedCurlyPosition(sourceAccumulator, openedCurlyPosition);

                int afterMethodDeclarationPosition = currentMatch.Groups[4].Index;

                int lastMethodDeclarationPosition = afterMethodDeclarationPosition - 1;
                int bodyLength = closedCurlyPosition - afterMethodDeclarationPosition;

                string removedString = sourceAccumulator.Substring(
                    afterMethodDeclarationPosition,
                    bodyLength + 1);
                
                sourceAccumulator = sourceAccumulator.Remove(
                    afterMethodDeclarationPosition, 
                    bodyLength + 1);

                indexToRemovedString.Add(lastMethodDeclarationPosition, removedString);
                int startAt = currentMatch.Index + currentMatch.Length;
                
                if (startAt >= sourceAccumulator.Length) break;
                
                currentMatch = languageMethodBodyRegex.Match(sourceAccumulator, startAt);
            }

            return new MethodBodyRemovalResult(sourceAccumulator, indexToRemovedString.ToImmutableDictionary());
        }

        static int ClosedCurlyPosition(string source, int firstCurlyPosition)
        {
            int nestingCounter = 0;
            for (int i = firstCurlyPosition; i < source.Length; i++)
            {
                switch (source[i])
                {
                    case '{':
                        nestingCounter++;
                        break;
                    case '}':
                        nestingCounter--;
                        break;
                }

                if (nestingCounter == 0) return i;
            }

            throw new Exception($"Cannot find close curly brace starting from {firstCurlyPosition} for: {source}");
        }
    }

    public class MethodBodyRemovalResult
    {
        public readonly string Source;
        
        /// <summary>
        /// Index is a position of last symbol of a function declaration header
        /// It is 'Y' for: fun f(x:X):Y  { 
        /// and ')' for: fun f(x:X){ 
        /// </summary>
        public readonly ImmutableDictionary<int, string> IdxToRemovedMethodBody;

        public MethodBodyRemovalResult(string source, ImmutableDictionary<int, string> idxToRemovedMethodBody)
        {
            Source = source;
            IdxToRemovedMethodBody = idxToRemovedMethodBody;
        }
    }
}