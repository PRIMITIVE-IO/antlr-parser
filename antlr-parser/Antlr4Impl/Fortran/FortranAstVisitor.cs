using System;
using System.Collections.Generic;
using System.Linq;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;

namespace antlr_parser.Antlr4Impl.Fortran
{
    public class FortranAstVisitor : Fortran90ParserBaseVisitor<AstNode>
    {
        readonly string Path;
        readonly string OriginalSource;
        readonly IndexToLocationConverter IndexToLocationConverter;
        readonly CodeRangeCalculator CodeRangeCalculator;

        public FortranAstVisitor(
            string path,
            string originalSource,
            CodeRangeCalculator codeRangeCalculator
        )
        {
            Path = path;
            OriginalSource = originalSource;
            IndexToLocationConverter = new IndexToLocationConverter(OriginalSource);
            CodeRangeCalculator = codeRangeCalculator;
        }
        
        #region VISITORS

        public override AstNode VisitExecutableProgram(Fortran90Parser.ExecutableProgramContext context)
        {
            List<AstNode> members = AntlrUtil.WalkUntilType(context.children, new HashSet<Type>
                {
                    typeof(Fortran90Parser.SubroutineSubprogramContext)
                },
                this);

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

        public override AstNode VisitSubroutineSubprogram(Fortran90Parser.SubroutineSubprogramContext context)
        {
            CodeRange codeRange = new CodeRange(new CodeLocation(context.Start.Line, context.Start.Column), new CodeLocation(context.Stop.Line, context.Stop.Column));

            return new AstNode.MethodNode(
                name: context.subroutineName().GetFullText(),
                accFlag: AccessFlags.None,
                startIdx: context.Start.StartIndex,
                codeRange: codeRange,
                arguments: new List<AstNode.ArgumentNode>(),
                returnType: "void"
            );
        }

        #endregion
    }
}