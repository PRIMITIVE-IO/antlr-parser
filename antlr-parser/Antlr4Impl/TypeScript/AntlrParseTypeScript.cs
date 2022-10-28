using System;
using System.Collections.Generic;
using antlr_parser.Antlr4Impl;
using Antlr4.Runtime;
using PrimitiveCodebaseElements.Primitive.dto;

namespace antlr_parser.Antlr4Impl.TypeScript
{
    public static class AntlrParseTypeScript
    {
        public static FileDto? Parse(string source, string filePath)
        {
            return ParseFileNode(source, filePath)
                ?.Let(fileNode => AstNodeToClassDtoConverter.ToFileDto(fileNode, source));
        }

        static AstNode.FileNode? ParseFileNode(string source, string filePath)
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

                CodeRangeCalculator codeRangeCalculator = new CodeRangeCalculator(source);

                TypeScriptVisitor visitor = new TypeScriptVisitor(
                    filePath,
                    removalResult,
                    codeRangeCalculator
                );
                return parser.program().Accept(visitor) as AstNode.FileNode;
            }
            catch (Exception e)
            {
                PrimitiveLogger.Logger.Instance().Error($"Failed to parse TS file {filePath}", e);
                return null;
            }
        }
    }
}