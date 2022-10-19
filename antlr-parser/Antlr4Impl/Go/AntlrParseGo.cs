using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using PrimitiveCodebaseElements.Primitive.dto;

namespace antlr_parser.Antlr4Impl.Go
{
    public static class AntlrParseGo
    {
        public static FileDto? Parse(string source, string filePath)
        {
            try
            {
                MethodBodyRemovalResult methodBodyRemovalResult =
                    MethodBodyRemovalResult.From(source, new List<Tuple<int, int>>());

                char[] codeArray = source.ToCharArray();
                AntlrInputStream inputStream = new AntlrInputStream(codeArray, codeArray.Length);

                GoLexer lexer = new GoLexer(inputStream);
                CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
                GoParser parser = new GoParser(commonTokenStream);

                parser.RemoveErrorListeners();
                parser.AddErrorListener(new ErrorListener()); // add ours 

                CodeRangeCalculator codeRangeCalculator = new CodeRangeCalculator(source);

                // a sourceUnit is the highest level container -> start there
                // do not call parser.sourceUnit() more than once
                AstNode.FileNode fileNode = parser.sourceFile().Accept(
                    new GoAstVisitor(
                        filePath,
                        methodBodyRemovalResult,
                        codeRangeCalculator
                    )) as AstNode.FileNode;

                return AstNodeToClassDtoConverter.ToFileDto(fileNode, source);
            }
            catch (Exception e)
            {
                PrimitiveLogger.Logger.Instance().Error($"Failed to parse Go file {filePath}", e);
                return null;
            }
        }
    }
}