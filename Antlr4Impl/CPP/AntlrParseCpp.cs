using System;
using System.Collections.Generic;
using antlr_parser.Antlr4Impl.C;
using Antlr4.Runtime;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.CPP
{
    public static class AntlrParseCpp
    {
        public static IEnumerable<ClassInfo> OuterClassInfosFromSource(string source, string filePath)
        {
            try
            {
                
                string preprocessedSource = MethodBodyRemovalResult
                    .From(source, DirectivesRemover.FindBlocksToRemove(source))
                    .ShortenedSource;

                List<Tuple<int, int>> blocksToRemove =
                    RegexBasedCppMethodBodyRemover.FindBlocksToRemove(preprocessedSource);

                MethodBodyRemovalResult methodBodyRemovalResult =
                    MethodBodyRemovalResult.From(preprocessedSource, blocksToRemove);
                
                char[] codeArray = methodBodyRemovalResult.ShortenedSource.ToCharArray();
                AntlrInputStream inputStream = new AntlrInputStream(codeArray, codeArray.Length);

                CPP14Lexer lexer = new CPP14Lexer(inputStream);
                CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
                CPP14Parser parser = new CPP14Parser(commonTokenStream);

                parser.RemoveErrorListeners();
                parser.AddErrorListener(new ErrorListener()); // add ours

                // a translationunit is the highest level container -> start there
                // do not call parser.translationUnit() more than once
                CPP14Parser.TranslationUnitContext translationUnit = parser.translationUnit();
                AstNode.FileNode fileNode = translationUnit.Accept(new CppAstVisitor(filePath, methodBodyRemovalResult)) as AstNode.FileNode;
                return AstToClassInfoConverter.ToClassInfo(fileNode, SourceCodeLanguage.Cpp);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return new List<ClassInfo>();
        }
    }
}