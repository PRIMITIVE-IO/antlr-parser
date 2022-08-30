using System;
using System.Collections.Generic;
using System.Linq;
using antlr_parser.Antlr4Impl.dto.converter;
using Antlr4.Runtime;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
using CodeRange = PrimitiveCodebaseElements.Primitive.dto.CodeRange;

namespace antlr_parser.Antlr4Impl.Java
{
    public static class AntlrParseJava
    {
        public static FileDto Parse(string source, string filePath)
        {
            try
            {
                return AstNodeToClassDtoConverter.ToFileDto(ParseFileNode(source, filePath), source);
            }
            catch (Exception e)
            {
                PrimitiveLogger.Logger.Instance().Error($"Failed to parse Java file {filePath}", e);
            }

            return null;
        }

        static AstNode.FileNode ParseFileNode(string source, string filePath)
        {
            try
            {
                List<Tuple<int, int>> blocksToRemove = RegexBasedJavaMethodBodyRemover.FindBlocksToRemove(source);

                MethodBodyRemovalResult methodBodyRemovalResult = MethodBodyRemovalResult.From(source, blocksToRemove);

                char[] codeArray = methodBodyRemovalResult.ShortenedSource.ToCharArray();
                AntlrInputStream inputStream = new AntlrInputStream(codeArray, codeArray.Length);

                JavaLexer lexer = new JavaLexer(inputStream);
                CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
                JavaParser parser = new JavaParser(commonTokenStream);

                parser.RemoveErrorListeners();
                parser.AddErrorListener(new ErrorListener()); // add ours

                CodeRangeCalculator codeRangeCalculator = new CodeRangeCalculator(source);
                // a compilation unit is the highest level container -> start there
                // do not call parser.compilationUnit() more than once
                return parser.compilationUnit().Accept(
                    new JavaAstVisitor(methodBodyRemovalResult, filePath, codeRangeCalculator)
                ) as AstNode.FileNode;
            }
            catch (Exception e)
            {
                PrimitiveLogger.Logger.Instance().Error($"Failed to parse Java file {filePath}", e);

                string[] lines = source.Split('\n');

                return new AstNode.FileNode(
                    path: filePath,
                    packageNode: null,
                    classes: new List<AstNode.ClassNode>(),
                    fields: new List<AstNode.FieldNode>(),
                    methods: new List<AstNode.MethodNode>(),
                    header: source,
                    namespaces: new List<AstNode.Namespace>(),
                    language: SourceCodeLanguage.Java,
                    isTest: false,
                    codeRange: new CodeRange(
                        new CodeLocation(1, 1),
                        new CodeLocation(lines.Length, lines.Last().Length)
                    )
                );
            }
        }
    }
}