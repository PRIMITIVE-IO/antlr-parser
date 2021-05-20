using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace antlr_parser.Antlr4Impl.C
{
    public class CVisitor : CBaseVisitor<AstNode>
    {
        readonly string fileName;

        public CVisitor(string fileName)
        {
            this.fileName = fileName;
        }

        public override AstNode VisitCompilationUnit(CParser.CompilationUnitContext context)
        {
            ImmutableList<AstNode.MethodNode> methods = context.translationUnit().externalDeclaration()
                .Select(it => it.functionDefinition()?.Accept(this) as AstNode.MethodNode)
                .Where(it => it != null)
                .ToImmutableList();

            ImmutableList<AstNode.ClassNode> structs = context.translationUnit().externalDeclaration()
                .SelectMany(it =>
                    it?.declaration()?.declarationSpecifiers()?.declarationSpecifier() ??
                    new CParser.DeclarationSpecifierContext[0]
                )
                .Select(it => it.typeSpecifier().structOrUnionSpecifier().Accept(this) as AstNode.ClassNode)
                .ToImmutableList();

            return new AstNode.FileNode(
                fileName,
                new AstNode.PackageNode(""),
                structs,
                ImmutableList<AstNode.FieldNode>.Empty,
                methods
            );
        }

        public override AstNode VisitStructOrUnionSpecifier(CParser.StructOrUnionSpecifierContext context)
        {
            string name = context.Identifier().ToString();
            ImmutableList<AstNode.FieldNode> fields = context.structDeclarationList().structDeclaration()
                .Select(it => it.Accept(this) as AstNode.FieldNode)
                .ToImmutableList();

            return new AstNode.ClassNode(
                name,
                ImmutableList<AstNode.MethodNode>.Empty,
                fields,
                ImmutableList<AstNode.ClassNode>.Empty,
                "public"
            );
        }

        public override AstNode VisitStructDeclaration(CParser.StructDeclarationContext context)
        {
            return new AstNode.FieldNode(
                context.specifierQualifierList().specifierQualifierList().GetFullText(),
                "public",
                context.GetFullText()
            );
        }

        public override AstNode VisitFunctionDefinition(CParser.FunctionDefinitionContext context)
        {
            string fName = ExtractFunctionName(context.declarator().directDeclarator());

            return new AstNode.MethodNode(
                fName,
                "public",
                context.GetFullText()
            );
        }

        private string ExtractFunctionName(CParser.DirectDeclaratorContext context)
        {
            if (context.directDeclarator() == null)
            {
                return context.GetText();
            }

            return ExtractFunctionName(context.directDeclarator());
        }
    }
}