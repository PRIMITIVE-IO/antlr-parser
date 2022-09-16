using System;
using System.Collections.Generic;
using System.Linq;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
using CodeRange = PrimitiveCodebaseElements.Primitive.dto.CodeRange;

namespace antlr_parser.Antlr4Impl.Rust
{
    public class RustAstVisitor : RustParserBaseVisitor<AstNode>
    {
        readonly string FilePath;
        readonly MethodBodyRemovalResult MethodBodyRemovalResult;
        readonly IndexToLocationConverter IndexToLocationConverter;
        readonly CodeRangeCalculator CodeRangeCalculator;

        public RustAstVisitor(
            string filePath,
            MethodBodyRemovalResult methodBodyRemovalResult,
            CodeRangeCalculator codeRangeCalculator
        )
        {
            FilePath = filePath;
            MethodBodyRemovalResult = methodBodyRemovalResult;
            IndexToLocationConverter = new IndexToLocationConverter(methodBodyRemovalResult.OriginalSource);
            CodeRangeCalculator = codeRangeCalculator;
        }

        public override AstNode VisitCrate(RustParser.CrateContext context)
        {
            List<AstNode.ClassNode> classNodes = AntlrUtil.WalkUntilType(
                    context.children,
                    new HashSet<Type>
                    {
                        typeof(RustParser.Type_Context)
                    },
                    this)
                .OfType<AstNode.ClassNode>()
                .ToList();
            
            string[] lines = MethodBodyRemovalResult.OriginalSource.Split('\n');

            return new AstNode.FileNode(
                path: FilePath,
                packageNode: null,
                classes: classNodes,
                fields: new List<AstNode.FieldNode>(),
                methods: new List<AstNode.MethodNode>(),
                namespaces: new List<AstNode.Namespace>(),
                language: SourceCodeLanguage.Solidity,
                isTest: false,
                codeRange: new CodeRange(
                    new CodeLocation(1, 1),
                    new CodeLocation(lines.Length, lines.Last().Length)
                )
            );
        }
    }
}