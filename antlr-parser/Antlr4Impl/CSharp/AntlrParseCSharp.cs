using System;
using System.Collections.Generic;
using antlr_parser.Antlr4Impl.C;
using antlr_parser.Antlr4Impl.dto.converter;
using Antlr4.Runtime;
using PrimitiveCodebaseElements.Primitive.dto;

namespace antlr_parser.Antlr4Impl.CSharp
{
    public static class AntlrParseCSharp
    {
        public static FileDto? Parse(string source, string filePath)
        {
            try
            {
                MethodBodyRemovalResult methodBodyRemovalResult1 = MethodBodyRemovalResult
                    .From(source, DirectivesRemover.FindBlocksToRemove(source));

                MethodBodyRemovalResult methodBodyRemovalResult = methodBodyRemovalResult1.RemoveFromShortened(
                    CSharpMethodBodyRemover.FindBlocksToRemove(methodBodyRemovalResult1.ShortenedSource)
                );

                char[] codeArray = methodBodyRemovalResult.ShortenedSource.ToCharArray();
                AntlrInputStream inputStream = new AntlrInputStream(codeArray, codeArray.Length);
                CSharpLexer lexer = new CSharpLexer(inputStream);
                ListTokenSource tokenSource = CSharpPreprocessorAwareParser.TokenSource(lexer);
                CommonTokenStream commonTokenStream = new CommonTokenStream(tokenSource);
                CSharpParser parser = new CSharpParser(commonTokenStream);

                parser.RemoveErrorListeners();
                parser.AddErrorListener(new ErrorListener()); // add ours

                CSharpParser.Compilation_unitContext compilationUnitContext = parser.compilation_unit();

                AstNode.FileNode fileNode = (AstNode.FileNode)compilationUnitContext
                    .Accept(new CSharpAstVisitor(filePath, methodBodyRemovalResult));

                return AstNodeToClassDtoConverter.ToFileDto(fileNode, source);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return null;
        }
    }
}