using System;
using System.Collections.Generic;
using antlr_parser.Antlr4Impl.C;
using antlr_parser.Antlr4Impl.dto;
using antlr_parser.Antlr4Impl.dto.converter;
using Antlr4.Runtime;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;

namespace antlr_parser.Antlr4Impl.CPP
{
    public static class AntlrParseCpp
    {
        public static IEnumerable<ClassInfo> OuterClassInfosFromSource(string source, string filePath)
        {
            try
            {
                return AstToClassInfoConverter.ToClassInfo(ParseFileNode(source, filePath), SourceCodeLanguage.Cpp);
            }
            catch (Exception e)
            {
                PrimitiveLogger.Logger.Instance().Error($"Failed to parse Cpp file: {filePath}", e);
            }

            return new List<ClassInfo>();
        }

        public static FileDto Parse(string source, string filePath)
        {
            return AstNodeToClassDtoConverter.ToFileDto(ParseFileNode(source, filePath), source);
        }

        private static AstNode.FileNode ParseFileNode(string source, string filePath)
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
            return translationUnit.Accept(new CppAstVisitor(filePath, methodBodyRemovalResult)) as AstNode.FileNode;
        }
    }
}