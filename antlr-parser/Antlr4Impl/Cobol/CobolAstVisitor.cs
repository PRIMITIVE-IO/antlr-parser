using System.Collections.Generic;
using System.Linq;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;

namespace antlr_parser.Antlr4Impl.Cobol
{
    public class CobolAstVisitor : Cobol85BaseVisitor<AstNode>
    {
        readonly string Path;
        readonly string OriginalSource;
        readonly IndexToLocationConverter IndexToLocationConverter;
        readonly CodeRangeCalculator CodeRangeCalculator;

        public CobolAstVisitor(
            string path,
            string source,
            CodeRangeCalculator codeRangeCalculator
        )
        {
            Path = path;
            OriginalSource = source;
            IndexToLocationConverter = new IndexToLocationConverter(OriginalSource);
            CodeRangeCalculator = codeRangeCalculator;
        }
        
        #region VISITORS

        public override AstNode VisitCompilationUnit(Cobol85Parser.CompilationUnitContext context)
        {
            List<AstNode> members = context.programUnit()
                .Select(it => it.Accept(this))
                .Where(it => it != null)
                .ToList() ?? new List<AstNode>();

            List<AstNode.MethodNode> methods = members.OfType<AstNode.MethodNode>().ToList();
            List<AstNode.ClassNode> classes = members.OfType<AstNode.ClassNode>().ToList();
            List<AstNode.FieldNode> fields = members.OfType<AstNode.FieldNode>().ToList();
            List<AstNode.Namespace> namespaces = members.OfType<AstNode.Namespace>().ToList();

            CodeRange codeRange = CodeRangeCalculator.Trim(
                new CodeRange(
                    new CodeLocation(1, 1),
                    CodeRangeCalculator.EndPosition()
                )
            );

            return new AstNode.FileNode(
                path: Path,
                packageNode: new AstNode.PackageNode(""),
                classes: classes,
                fields: fields,
                methods: methods,
                namespaces: namespaces,
                language: SourceCodeLanguage.PlainText,
                isTest: false,
                codeRange: codeRange
            );
        }
    
        #endregion
    }
}