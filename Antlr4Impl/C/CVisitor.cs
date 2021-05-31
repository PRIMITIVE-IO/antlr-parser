using System;
using System.Collections.Immutable;
using System.Linq;
using Antlr4.Runtime;

namespace antlr_parser.Antlr4Impl.C
{
    public class CVisitor : CBaseVisitor<AstNode>
    {
        readonly string fileName;
        private readonly MethodBodyRemovalResult methodBodyRemovalResult;

        public CVisitor(string fileName, MethodBodyRemovalResult methodBodyRemovalResult)
        {
            this.fileName = fileName;
            this.methodBodyRemovalResult = methodBodyRemovalResult;
        }

        public override AstNode VisitCompilationUnit(CParser.CompilationUnitContext context)
        {
            ImmutableList<AstNode.MethodNode> methods = context.translationUnit()?.externalDeclaration()
                .Select(it => it.functionDefinition()?.Accept(this) as AstNode.MethodNode)
                .Where(it => it != null)
                .ToImmutableList()
                ??ImmutableList<AstNode.MethodNode>.Empty;

            ImmutableList<AstNode.ClassNode> structs = context.translationUnit()?.externalDeclaration()
                .SelectMany(it =>
                    it?.declaration()?.declarationSpecifiers()?.declarationSpecifier() ??
                    new CParser.DeclarationSpecifierContext[0]
                )
                .Select(it => it.typeSpecifier()?.structOrUnionSpecifier()?.Accept(this) as AstNode.ClassNode)
                .Where(it => it != null)
                .ToImmutableList()
                ??ImmutableList<AstNode.ClassNode>.Empty;

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
            /* use case for null identifier:
             *
             * typedef struct {
             *   UINT32 X;
             * } GUI_DRAW_REQUEST;
             */
            string name = context.Identifier()?.ToString() ?? "anonymous";

            ImmutableList<AstNode.FieldNode> fields = context.structDeclarationList()?.structDeclaration()
                .Select(it => it.Accept(this) as AstNode.FieldNode)
                .ToImmutableList();

            ImmutableList<AstNode.ClassNode> innerClasses = context.structDeclarationList()?.structDeclaration()
                .Select(it => it.specifierQualifierList()
                    ?.typeSpecifier()
                    ?.structOrUnionSpecifier()
                    ?.Accept(this) as AstNode.ClassNode
                )
                .Where(it => it != null)
                .ToImmutableList();

            return new AstNode.ClassNode(
                name,
                ImmutableList<AstNode.MethodNode>.Empty,
                fields ?? ImmutableList<AstNode.FieldNode>.Empty,
                innerClasses ?? ImmutableList<AstNode.ClassNode>.Empty,
                "public"
            );
        }

        public override AstNode VisitStructDeclaration(CParser.StructDeclarationContext context)
        {
            string fieldName = ExtractPlainFieldName(context) ?? ExtractArrayFieldName(context);

            return new AstNode.FieldNode(
                fieldName,
                "public",
                context.GetFullText()
            );
        }

        private string ExtractPlainFieldName(CParser.StructDeclarationContext context)
        {
            return context.specifierQualifierList()
                ?.specifierQualifierList()
                ?.typeSpecifier()
                ?.typedefName()
                ?.Identifier()
                ?.ToString();
        }

        private string ExtractArrayFieldName(CParser.StructDeclarationContext context)
        {
            return context.structDeclaratorList()
                ?.structDeclarator()
                ?.First()
                ?.declarator()
                ?.directDeclarator()
                ?.directDeclarator()
                ?.Identifier()
                ?.ToString();
        }

        public override AstNode VisitFunctionDefinition(CParser.FunctionDefinitionContext context)
        {
            string text = context.GetFullText();
            string fName = ExtractFunctionName(context.declarator().directDeclarator());

            return new AstNode.MethodNode(
                fName,
                "public",
                text +
                (methodBodyRemovalResult.IdxToRemovedMethodBody.GetValueOrDefault(context.Stop.StopIndex) ?? "")
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