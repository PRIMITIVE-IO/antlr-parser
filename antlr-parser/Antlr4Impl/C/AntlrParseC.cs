using System;
using System.Collections.Generic;
using antlr_parser.Antlr4Impl.dto.converter;
using Antlr4.Runtime;
using PrimitiveCodebaseElements.Primitive.dto;

namespace antlr_parser.Antlr4Impl.C
{
    public static class AntlrParseC
    {

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
            return compilationUnitContext.Accept(new CVisitor(filePath, methodBodyRemovalResult)) as AstNode.FileNode;
        }
    }
}