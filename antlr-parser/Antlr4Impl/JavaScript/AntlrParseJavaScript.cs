using System;
using System.Collections.Generic;
using antlr_parser.Antlr4Impl.dto.converter;
using Antlr4.Runtime;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;

namespace antlr_parser.Antlr4Impl.JavaScript
{
    public static class AntlrParseJavaScript
    {
        public static IEnumerable<ClassInfo> OuterClassInfosFromSource(string source, string filePath)
        {
            try
            {
                return AstToClassInfoConverter.ToClassInfo(ParseFileNode(source, filePath), SourceCodeLanguage.JavaScript);
            }
            catch (Exception e)
            {
                PrimitiveLogger.Logger.Instance().Error($"Failed to parse JavaScript file {filePath}", e);

                return new List<ClassInfo>();
            }
        }

        public static FileDto Parse(string source, string filePath)
        {
            return AstNodeToClassDtoConverter.ToFileDto(ParseFileNode(source, filePath), source);
        }

        static AstNode.FileNode ParseFileNode(string source, string filePath)
        {
            List<Tuple<int, int>> blocksToRemove = RegexBasedJavaScriptMethodBodyRemover.FindBlocksToRemove(source);
            MethodBodyRemovalResult removalResult = MethodBodyRemovalResult.From(source, blocksToRemove);

            char[] codeArray = removalResult.ShortenedSource.ToCharArray();
            AntlrInputStream inputStream = new AntlrInputStream(codeArray, codeArray.Length);

            JavaScriptLexer lexer = new JavaScriptLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
            JavaScriptParser parser = new JavaScriptParser(commonTokenStream);

            parser.RemoveErrorListeners();
            parser.AddErrorListener(new ErrorListener()); // add ours

            // a program is the highest level container -> start there
            // do not call parser.program() more than once
            JavaScriptParser.ProgramContext programContext = parser.program();
            return programContext.Accept(new JavaScriptAstVisitor(filePath, removalResult)) as AstNode.FileNode;
        }
    }
}