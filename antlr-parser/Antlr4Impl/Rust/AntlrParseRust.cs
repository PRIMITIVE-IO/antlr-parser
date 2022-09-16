using System;
using System.Collections.Generic;
using antlr_parser.Antlr4Impl.C;
using Antlr4.Runtime;
using PrimitiveCodebaseElements.Primitive.dto;

namespace antlr_parser.Antlr4Impl.Rust
{
    public static class AntlrParseRust
    {
        public static FileDto Parse(string source, string filePath)
        {
            return AstNodeToClassDtoConverter.ToFileDto(ParseFileNode(source, filePath), source);
        }

        static AstNode.FileNode ParseFileNode(string source, string filePath)
        {
            List<Tuple<int, int>> removedDirectiveBlocks = DirectivesRemover.FindBlocksToRemove(source);

            MethodBodyRemovalResult directivesRemovalResult =
                MethodBodyRemovalResult.From(source, removedDirectiveBlocks);

            MethodBodyRemovalResult methodBodyRemovalResult = directivesRemovalResult.RemoveFromShortened(
                RegexBasedCMethodBodyRemover.FindBlocksToRemove(directivesRemovalResult.ShortenedSource)
            );

            char[] codeArray = methodBodyRemovalResult.ShortenedSource.ToCharArray();

            AntlrInputStream inputStream = new AntlrInputStream(codeArray, codeArray.Length);
            RustLexer lexer = new RustLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
            RustParser parser = new RustParser(commonTokenStream);

            parser.RemoveErrorListeners();
            parser.AddErrorListener(new ErrorListener()); // add ours

            CodeRangeCalculator codeRangeCalculator = new CodeRangeCalculator(source);
            
            return (AstNode.FileNode)parser.crate().Accept(
                new RustAstVisitor(
                    filePath,
                    methodBodyRemovalResult,
                    codeRangeCalculator
                )
            );
        }
    }
}