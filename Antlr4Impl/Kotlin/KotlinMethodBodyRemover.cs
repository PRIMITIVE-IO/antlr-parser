using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace antlr_parser.Antlr4Impl.Kotlin
{
    public static class KotlinMethodBodyRemover
    {
        //matches functions having open curly braces: "fun f(a:B, b:C):Z {"
        private static Regex FunctionDeclarationRegex =
            new Regex("fun\\s+\\w*\\s*\\((\\s*\\w*\\s*:\\s*\\w*\\s*,?)*\\)\\s*(:\\s*\\w*\\s*)?{");

        public static string RemoveFunctionBodies(string source)
        {
            return FunctionDeclarationRegex
                .Matches(source)
                .Reverse() //process file from end. Otherwise each modification will shift the rest of the file
                .Aggregate(source, RemoveBodyForMatchedFunction);
        }
        
        private static string RemoveBodyForMatchedFunction(string source, Match match)
        {
            var openedCurlyPosition = match.Index + match.Length - 1;
            var closedCurlyPosition = ClosedCurlyPosition(source, openedCurlyPosition);

            return source.Remove(openedCurlyPosition, closedCurlyPosition - openedCurlyPosition + 1);
        }

        private static int ClosedCurlyPosition(string source, int firstCurlyPosition)
        {
            var nestingCounter = 0;
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
}