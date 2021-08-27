using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.C
{
    public class CVisitor : CBaseVisitor<AstNode>
    {
        readonly string _filePath;
        private readonly MethodBodyRemovalResult _methodBodyRemovalResult;

        public CVisitor(string filePath, MethodBodyRemovalResult methodBodyRemovalResult)
        {
            _filePath = filePath;
            _methodBodyRemovalResult = methodBodyRemovalResult;
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
                .DefaultIfEmpty(_methodBodyRemovalResult.RestoreIdx(context.Stop?.StopIndex ?? 0))
                .Min();

            string header = _methodBodyRemovalResult.ExtractOriginalSubstring(0, headerEnd)
                .Trim();

            return new AstNode.FileNode(
                path: _filePath,
                packageNode: new AstNode.PackageNode(""),
                classes: structs,
                fields: new List<AstNode.FieldNode>(),
                methods: methods,
                header: header,
                language: SourceCodeLanguage.C,
                isTest: false
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

            int headerStart = new[]
            {
                PreviousPeerEndPosition(context.Parent, context),
                0
            }.Max();

            int headerEnd = fields.Select(it => it.StartIdx - 1)
                .Concat(innerClasses.Select(it => it.StartIdx - 1))
                .DefaultIfEmpty(_methodBodyRemovalResult.RestoreIdx(context.Stop?.StopIndex ?? 0))
                .Min();

            string header = _methodBodyRemovalResult.ExtractOriginalSubstring(headerStart, headerEnd)
                .Trim();

            return new AstNode.ClassNode(
                name,
                new List<AstNode.MethodNode>(),
                fields,
                innerClasses,
                AccessFlags.AccPublic,
                _methodBodyRemovalResult.RestoreIdx(context.Start.StartIndex),
                _methodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex),
                header
            );
        }

        int PreviousPeerEndPosition(RuleContext parent, IParseTree self)
        {
            if (parent == null)
            {
                return -1;
            }

            if (parent is CParser.StructDeclarationListContext structDeclarationListContext)
            {
                return structDeclarationListContext.structDeclaration()
                    .TakeWhile(it => it != self)
                    .Select(it => _methodBodyRemovalResult.RestoreIdx(it.Stop.StopIndex + 1))
                    .DefaultIfEmpty(-1)
                    .Max();
            }

            if (parent is CParser.TranslationUnitContext translationUnitContext)
            {
                return translationUnitContext.externalDeclaration()
                    .TakeWhile(it => it != self)
                    .Select(it => _methodBodyRemovalResult.RestoreIdx(it.Stop.StopIndex + 1))
                    .DefaultIfEmpty(-1)
                    .Max();
            }

            return PreviousPeerEndPosition(parent.Parent, parent);
        }

        public override AstNode VisitStructDeclaration(CParser.StructDeclarationContext context)
        {
            string fieldName = ExtractPlainFieldName(context)
                               ?? ExtractArrayFieldName(context)
                               ?? ExtractReferenceFieldName(context)
                               ?? ExtractMultiName(context);

            return new AstNode.FieldNode(
                fieldName,
                AccessFlags.AccPublic,
                context.GetFullText(),
                _methodBodyRemovalResult.RestoreIdx(context.Start.StartIndex),
                _methodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex)
            );
        }

        private string ExtractMultiName(CParser.StructDeclarationContext context)
        {
            List<String> names = context.structDeclaratorList()
                ?.structDeclarator()
                ?.Select(it => it.declarator().directDeclarator().Identifier().ToString())
                ?.ToList();
            return (names?.Count ?? 0) != 0 ? string.Join(",", names) : null;
        }

        private string ExtractReferenceFieldName(CParser.StructDeclarationContext context)
        {
            return context.specifierQualifierList()
                ?.specifierQualifierList()
                ?.specifierQualifierList()
                ?.typeSpecifier()
                ?.typedefName()
                ?.Identifier()
                ?.ToString();
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

            _methodBodyRemovalResult.IdxToRemovedMethodBody.TryGetValue(context.Stop.StopIndex,
                out string removedSourceCode);

            return new AstNode.MethodNode(
                fName,
                AccessFlags.AccPublic,
                text + removedSourceCode ?? "",
                _methodBodyRemovalResult.RestoreIdx(context.Start.StartIndex),
                    _methodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex)
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