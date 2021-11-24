using System;
using System.Collections.Generic;
using antlr_parser.Antlr4Impl.dto;
using antlr_parser.Antlr4Impl.dto.converter;
using Antlr4.Runtime;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;

namespace antlr_parser.Antlr4Impl.Kotlin
{
    public static class AntlrParseKotlin
    {
        public static IEnumerable<ClassInfo> OuterClassInfosFromSource(string source, string filePath)
        {
            try
            {
                var astFileNode = ParseFileNode(source, filePath);

                return AstToClassInfoConverter.ToClassInfo(astFileNode, SourceCodeLanguage.Kotlin);
            }
            catch (Exception e)
            {
                PrimitiveLogger.Logger.Instance().Error($"Failed to parse Kotlin file {filePath}", e);
            }

            return null;
        }

        public static FileDto Parse(string source, string filePath)
        {
            return AstNodeToClassDtoConverter.ToFileDto(ParseFileNode(source, filePath), source);
        }

        static AstNode.FileNode ParseFileNode(string source, string filePath)
        {
            List<Tuple<int, int>> blocksToRemove = ClassBasedMethodBodyRemover.FindBlocksToRemove(source);
            MethodBodyRemovalResult removalMethodBodyRemovalResult =
                MethodBodyRemovalResult.From(source, blocksToRemove);

            char[] codeArray = removalMethodBodyRemovalResult.ShortenedSource.ToCharArray();
            AntlrInputStream inputStream = new AntlrInputStream(codeArray, codeArray.Length);

            KotlinLexer lexer = new KotlinLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
            KotlinParser parser = new KotlinParser(commonTokenStream);

            parser.RemoveErrorListeners();
            parser.AddErrorListener(new ErrorListener()); // add ours 

            // a KotlinFile is the highest level container -> start there
            // do not call parser.kotlinFile() more than once
            KotlinParser.KotlinFileContext kotlinFileContext = parser.kotlinFile();
            return kotlinFileContext.Accept(new KotlinVisitor(filePath, removalMethodBodyRemovalResult)) as AstNode.FileNode;
        }
    }
}