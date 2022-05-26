using System;
using System.Collections.Generic;
using System.IO;
using antlr_parser.Antlr4Impl.dto.converter;
using Antlr4.Runtime;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
using CodeRange = PrimitiveCodebaseElements.Primitive.dto.CodeRange;

namespace antlr_parser.Antlr4Impl.Python
{
    public static class AntlrParsePython3
    {
        public static FileDto Parse(string source, string filePath)
        {
            return AstNodeToClassDtoConverter.ToFileDto(ParseFileNode(source, filePath), source);
        }

        private static AstNode.FileNode ParseFileNode(string source, string filePath)
        {
            try
            {
                if (source.IsBlank())
                {
                    return new AstNode.FileNode(
                        path: filePath,
                        packageNode: null,
                        classes: new List<AstNode.ClassNode>(),
                        fields: new List<AstNode.FieldNode>(),
                        methods: new List<AstNode.MethodNode>(),
                        header: "",
                        namespaces: new List<AstNode.Namespace>(),
                        language: SourceCodeLanguage.Python,
                        isTest: false,
                        codeRange: new CodeRange(new CodeLocation(1, 1), new CodeLocation(1, 1))
                    );
                }

                List<Tuple<int, int>> blocksToRemove = PythonMethodBodyRemover.FindBlocksToRemove(source);
                MethodBodyRemovalResult methodBodyRemovalResult = MethodBodyRemovalResult.From(source, blocksToRemove);
                IndexToLocationConverter indexToLocationConverter = new IndexToLocationConverter(source);


                char[] codeArray = methodBodyRemovalResult.ShortenedSource.ToCharArray();
                AntlrInputStream inputStream = new AntlrInputStream(codeArray, codeArray.Length);

                Python3Lexer lexer3 = new Python3Lexer(inputStream);
                CommonTokenStream commonTokenStream3 = new CommonTokenStream(lexer3);
                Python3Parser parser3 = new Python3Parser(commonTokenStream3);

                parser3.RemoveErrorListeners();
                parser3.AddErrorListener(new ErrorListener());

                Python3Parser.File_inputContext fileUnit3 = parser3.file_input();


                return fileUnit3.Accept(new Python3AstVisitor(
                    filePath,
                    LastNonWhitespaceIndexer.IdxToLastNonWhiteSpace(source),
                    methodBodyRemovalResult,
                    indexToLocationConverter
                )) as AstNode.FileNode;
            }
            catch (Exception ex)
            {
                PrimitiveLogger.Logger.Instance().Error($"Cannot parse {filePath}", ex);
                return new AstNode.FileNode(
                    path: filePath,
                    packageNode: null,
                    classes: new List<AstNode.ClassNode>(),
                    fields: new List<AstNode.FieldNode>(),
                    methods: new List<AstNode.MethodNode>(),
                    header: "",
                    namespaces: new List<AstNode.Namespace>(),
                    language: SourceCodeLanguage.Python,
                    isTest: false,
                    codeRange: new CodeRange(new CodeLocation(1, 1), new CodeLocation(1, 1))
                );
            }
        }
    }
}