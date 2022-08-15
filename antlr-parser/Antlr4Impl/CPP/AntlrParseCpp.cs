using antlr_parser.Antlr4Impl.C;
using antlr_parser.Antlr4Impl.dto.converter;
using Antlr4.Runtime;
using PrimitiveCodebaseElements.Primitive.dto;

namespace antlr_parser.Antlr4Impl.CPP
{
    public static class AntlrParseCpp
    {
        public static FileDto Parse(string source, string filePath)
        {
            return AstNodeToClassDtoConverter.ToFileDto(ParseFileNode(source, filePath), source);
        }

        static AstNode.FileNode ParseFileNode(string source, string filePath)
        {
            MethodBodyRemovalResult directivesRemovalResult = MethodBodyRemovalResult
                .From(source, DirectivesRemover.FindBlocksToRemove(source));

            MethodBodyRemovalResult methodBodyRemovalResult = directivesRemovalResult.RemoveFromShortened(
                RegexBasedCppMethodBodyRemover.FindBlocksToRemove(directivesRemovalResult.ShortenedSource)
            );

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