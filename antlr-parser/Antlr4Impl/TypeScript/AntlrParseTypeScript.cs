using System;
using System.Collections.Generic;
using antlr_parser.Antlr4Impl.dto.converter;
using Antlr4.Runtime;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;

namespace antlr_parser.Antlr4Impl.TypeScript
{
    public static class AntlrParseTypeScript
    {
        public static IEnumerable<ClassInfo> OuterClassInfosFromSource(string source, string filePath)
        {
            return AstToClassInfoConverter.ToClassInfo(ParseFileNode(source, filePath), SourceCodeLanguage.TypeScript);
        }

        public static FileDto Parse(string source, string filePath)
        {
            return AstNodeToClassDtoConverter.ToFileDto(ParseFileNode(source, filePath), source);
        }

        private static AstNode.FileNode ParseFileNode(string source, string filePath)
        {
            try
            {
                List<Tuple<int, int>> blocksToRemove = RegexBasedTypeScriptMethodBodyRemover.FindBlocksToRemove(source);
                MethodBodyRemovalResult removalResult = MethodBodyRemovalResult.From(source, blocksToRemove);

                char[] codeArray = removalResult.ShortenedSource.ToCharArray();
                AntlrInputStream inputStream = new AntlrInputStream(codeArray, codeArray.Length);

                TypeScriptLexer lexer = new TypeScriptLexer(inputStream);
                CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
                TypeScriptParser parser = new TypeScriptParser(commonTokenStream);

                parser.RemoveErrorListeners();
                parser.AddErrorListener(new ErrorListener()); // add ours

                TypeScriptVisitor visitor = new TypeScriptVisitor(filePath, removalResult);
                AstNode.FileNode res = parser.program().Accept(visitor) as AstNode.FileNode;
                return res;
            }
            catch (Exception e)
            {
                PrimitiveLogger.Logger.Instance().Error($"Failed to parse TS file {filePath}", e);
                return null;
            }
        }
    }
}