using Antlr4.Runtime;
using PrimitiveCodebaseElements.Primitive.dto;

namespace antlr_parser.Antlr4Impl.Cobol
{
    public class AntlrParseCobol
    {
        public static FileDto Parse(string source, string filePath)
        {
            return AstNodeToClassDtoConverter.ToFileDto(ParseFileNode(source, filePath), source);
        }

        static AstNode.FileNode ParseFileNode(string source, string filePath)
        {
            char[] codeArray = source.ToCharArray();
            AntlrInputStream inputStream = new AntlrInputStream(codeArray, codeArray.Length);

            Cobol85Lexer lexer = new Cobol85Lexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
            Cobol85Parser parser = new Cobol85Parser(commonTokenStream);

            parser.RemoveErrorListeners();
            parser.AddErrorListener(new ErrorListener()); // add ours

            CodeRangeCalculator codeRangeCalculator = new CodeRangeCalculator(source);

            // a translationunit is the highest level container -> start there
            // do not call parser.translationUnit() more than once
            Cobol85Parser.CompilationUnitContext compilationUnitContext = parser.compilationUnit();
            
            return compilationUnitContext.Accept(
                new CobolAstVisitor(
                    filePath,
                    source,
                    codeRangeCalculator
                )
            ) as AstNode.FileNode;
        }
    }
}