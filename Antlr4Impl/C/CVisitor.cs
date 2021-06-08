using System;
using System.Collections.Generic;
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
            List<AstNode.MethodNode> methods = context.translationUnit()?.externalDeclaration()
                .Select(it => it.functionDefinition()?.Accept(this) as AstNode.MethodNode)
                .Where(it => it != null)
                .ToList() ?? new List<AstNode.MethodNode>();

            List<AstNode.ClassNode> structs = context.translationUnit()?.externalDeclaration()
                .SelectMany(it =>
                    it?.declaration()?.declarationSpecifiers()
                        ?.declarationSpecifier() ?? new CParser.DeclarationSpecifierContext[0]
                )
                .Select(it =>
                    it.typeSpecifier()?.structOrUnionSpecifier()?.Accept(this) as AstNode.ClassNode)
                .Where(it => it != null)
                .ToList() ?? new List<AstNode.ClassNode>();


            int headerEnd = methods.Select(it => it.StartIdx - 1)
                .Concat(structs.Select(it => it.StartIdx - 1))
                .DefaultIfEmpty(context.Stop?.StopIndex ?? 0)
                .Min();

            string header = methodBodyRemovalResult.RestoreOriginalSubstring(0, headerEnd)
                .Trim();

            return new AstNode.FileNode(
                fileName,
                new AstNode.PackageNode(""),
                structs,
                new List<AstNode.FieldNode>(),
                methods,
                header
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

            List<AstNode.FieldNode> fields = context.structDeclarationList()?.structDeclaration()
                .Select(it => it.Accept(this) as AstNode.FieldNode)
                .ToList() ?? new List<AstNode.FieldNode>();

            List<AstNode.ClassNode> innerClasses = context.structDeclarationList()?.structDeclaration()
                .Select(it => it.specifierQualifierList()
                    ?.typeSpecifier()
                    ?.structOrUnionSpecifier()
                    ?.Accept(this) as AstNode.ClassNode
                )
                .Where(it => it != null)
                .ToList() ?? new List<AstNode.ClassNode>();

            int headerEnd = fields.Select(it => it.StartIdx - 1)
                .Concat(innerClasses.Select(it => it.StartIdx - 1))
                .DefaultIfEmpty(context.Stop?.StopIndex ?? 0)
                .Min();

            string header = methodBodyRemovalResult.RestoreOriginalSubstring(context.Start.StartIndex, headerEnd)
                .Trim();

            return new AstNode.ClassNode(
                name,
                new List<AstNode.MethodNode>(),
                fields,
                innerClasses,
                "public",
                context.Start.StartIndex,
                context.Stop.StopIndex,
                header
            );
        }

        public override AstNode VisitStructDeclaration(CParser.StructDeclarationContext context)
        {
            string fieldName = ExtractPlainFieldName(context) ?? ExtractArrayFieldName(context);

            return new AstNode.FieldNode(
                fieldName,
                "public",
                context.GetFullText(),
                context.Start.StartIndex,
                context.Stop.StopIndex
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

            methodBodyRemovalResult.IdxToRemovedMethodBody.TryGetValue(context.Stop.StopIndex,
                out string removedSourceCode);

            return new AstNode.MethodNode(
                fName,
                "public",
                text + removedSourceCode ?? "",
                context.Start.StartIndex,
                context.Stop.StopIndex
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