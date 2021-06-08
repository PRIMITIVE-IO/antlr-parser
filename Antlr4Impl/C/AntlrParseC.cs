using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.C
{
    public static class AntlrParseC
    {
        public static IEnumerable<ClassInfo> OuterClassInfosFromSource(string source, string filePath)
        {
            Console.WriteLine("Parsing file: {0}", filePath);
            try
            {
                string preprocessedSource = MethodBodyRemovalResult
                    .From(source, DirectivesRemover.FindBlocksToRemove(source))
                    .ShortenedSource;

                List<Tuple<int, int>> blocksToRemove =
                    RegexBasedCMethodBodyRemover.FindBlocksToRemove(preprocessedSource);

                MethodBodyRemovalResult methodBodyRemovalResult =
                    MethodBodyRemovalResult.From(preprocessedSource, blocksToRemove);


                char[] codeArray = methodBodyRemovalResult.ShortenedSource.ToCharArray();
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

                return new List<ClassInfo> {AstToClassInfoConverter.ToClassInfo(astNode, SourceCodeLanguage.C)};
            }
            catch (Exception e)
            {
                Console.WriteLine($"file: {filePath}, source: {source}, exception: {e}");
            }

            return new List<ClassInfo>();
        }
    }
}