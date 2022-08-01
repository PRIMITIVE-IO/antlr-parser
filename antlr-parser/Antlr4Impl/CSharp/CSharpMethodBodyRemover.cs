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
        //group 1 matches the last curly
        /*
         positive test cases:
            public static List<Tuple<int, int>> FindBlocksToRemove(string source) {
            static void Swap<T>(ref T lhs, ref T rhs) {
            public static void TestSwap(){
            f(x){
            
         negative test cases:
            if(x = y){
            for(int x = 0; x < 3; x++){
            foreach(int x in y){
            while(x){
        */

        static readonly Regex MethodDeclarationRegex = new Regex(
            @"(?<!if)(?<!for)(?<!foreach)(?<!while)\s*?\(.*?\)\s*?({)"
        );

        public static List<Tuple<int, int>> FindBlocksToRemove(string source)
        {
            return MethodDeclarationRegex.Matches(source)
                .SelectNotNull(currentMatch =>
                {
                    int openedCurlyPosition = currentMatch.Groups[1].Index;
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