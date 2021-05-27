using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.C
{
    public static class AntlrParseC
    {
        /// <summary>
        /// Unescaped regex: #if(.|\n)*?(\{|\})(.|\n)*?#endif
        /// Matches curly braces between #if and #endif preprocessor directives.
        /// False-positive match: #if #endif { #if #endif
        ///
        /// Reason: broken open-closed curly brace parity causes errors when we are trying to remove function bodies.
        /// That is why we are trying to identify such a files and do not apply body removal for them.
        /// </summary>
        static readonly Regex HasCurlyBetweenDirectives = new Regex("#if(.|\\n)*?(\\{|\\})(.|\\n)*?#endif"); 
        public static IEnumerable<ClassInfo> OuterClassInfosFromSource(string source, string filePath)
        {
            Console.WriteLine("Parsing file: {0}", filePath);
            try
            {
                MethodBodyRemovalResult methodBodyRemovalResult;

                if (HasCurlyBetweenDirectives.IsMatch(source))
                {
                    methodBodyRemovalResult = new MethodBodyRemovalResult(source, ImmutableDictionary<int, string>.Empty);
                }
                else
                {
                    ImmutableList<Tuple<int, int>> blocksToRemove = RegexBasedCMethodBodyRemover.FindBlocksToRemove(source);
                    methodBodyRemovalResult = MethodBodyRemovalResult.From(source, blocksToRemove);
                }

                char[] codeArray = methodBodyRemovalResult.Source.ToCharArray();
                AntlrInputStream inputStream = new AntlrInputStream(codeArray, codeArray.Length);

                CLexer lexer = new CLexer(inputStream);
                CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
                CParser parser = new CParser(commonTokenStream);

                parser.RemoveErrorListeners();
                parser.AddErrorListener(new ErrorListener()); // add ours

                // a compilation unit is the highest level container -> start there
                // do not call parser.compilationUnit() more than once
                CParser.CompilationUnitContext compilationUnitContext = parser.compilationUnit();
                AstNode.FileNode astNode =
                    compilationUnitContext.Accept(new CVisitor(filePath, methodBodyRemovalResult)) as AstNode.FileNode;
                
                return ImmutableList.Create(AstToClassInfoConverter.ToClassInfo(astNode, SourceCodeLanguage.C));
            }
            catch (Exception e)
            {
                Console.WriteLine($"file: {filePath}, source: {source}, exception: {e}");
            }

            return ImmutableList<ClassInfo>.Empty;
        }
    }
}