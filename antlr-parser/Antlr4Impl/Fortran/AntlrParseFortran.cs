using Antlr4.Runtime;
using PrimitiveCodebaseElements.Primitive.dto;

namespace antlr_parser.Antlr4Impl.Fortran
{
    public static class AntlrParseFortran
    {
        public static FileDto Parse(string source, string filePath)
        {
            return AstNodeToClassDtoConverter.ToFileDto(ParseFileNode(source, filePath), source);
        }

        static AstNode.FileNode ParseFileNode(string source, string filePath)
        {
            char[] codeArray = source.ToCharArray();
            AntlrInputStream inputStream = new AntlrInputStream(codeArray, codeArray.Length);

            Fortran90Lexer lexer = new Fortran90Lexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
            Fortran90Parser parser = new Fortran90Parser(commonTokenStream);

            parser.RemoveErrorListeners();
            parser.AddErrorListener(new ErrorListener()); // add ours

            CodeRangeCalculator codeRangeCalculator = new CodeRangeCalculator(source);

            // a translationunit is the highest level container -> start there
            // do not call parser.translationUnit() more than once
            Fortran90Parser.ProgramContext programContext = parser.program();
            
            return programContext.Accept(
                new FortranAstVisitor(
                    filePath,
                    source,
                    codeRangeCalculator
                )
            ) as AstNode.FileNode;
        }
    }
}