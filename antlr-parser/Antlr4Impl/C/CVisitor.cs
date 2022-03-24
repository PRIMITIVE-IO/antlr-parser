using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
using CodeRange = PrimitiveCodebaseElements.Primitive.dto.CodeRange;

namespace antlr_parser.Antlr4Impl.C
{
    public class CVisitor : CBaseVisitor<AstNode>
    {
        readonly string filePath;
        private readonly MethodBodyRemovalResult methodBodyRemovalResult;
        private readonly IndexToLocationConverter IndexToLocationConverter;

        public CVisitor(string filePath, MethodBodyRemovalResult methodBodyRemovalResult)
        {
            this.filePath = filePath;
            this.methodBodyRemovalResult = methodBodyRemovalResult;
            IndexToLocationConverter = new IndexToLocationConverter(methodBodyRemovalResult.OriginalSource);
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
                        ?.declarationSpecifier() ?? Array.Empty<CParser.DeclarationSpecifierContext>()
                )
                .Select(it =>
                    it.typeSpecifier()?.structOrUnionSpecifier()?.Accept(this) as AstNode.ClassNode)
                .Where(it => it != null)
                .ToList() ?? new List<AstNode.ClassNode>();


            int headerEnd = methods.Select(it => it.StartIdx - 1)
                .Concat(structs.Select(it => it.StartIdx - 1))
                .DefaultIfEmpty(methodBodyRemovalResult.RestoreIdx(context.Stop?.StopIndex ?? 0))
                .Min();

            string header = methodBodyRemovalResult.ExtractOriginalSubstring(0, headerEnd)
                .Trim();

            CodeLocation headerEndLocation = IndexToLocationConverter.IdxToLocation(headerEnd);

            return new AstNode.FileNode(
                path: filePath,
                packageNode: new AstNode.PackageNode(""),
                classes: structs,
                fields: new List<AstNode.FieldNode>(),
                methods: methods,
                header: header,
                namespaces: new List<AstNode.Namespace>(),
                language: SourceCodeLanguage.C,
                isTest: false,
                codeRange: new CodeRange(new CodeLocation(1, 1), headerEndLocation)
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
                .DefaultIfEmpty(methodBodyRemovalResult.RestoreIdx(context.Stop?.StopIndex ?? 0))
                .Min();

            string header = methodBodyRemovalResult.ExtractOriginalSubstring(headerStart, headerEnd)
                .Trim();

            CodeRange codeRange = IndexToLocationConverter.IdxToCodeRange(headerStart, headerEnd);

            return new AstNode.ClassNode(
                name,
                new List<AstNode.MethodNode>(),
                fields,
                innerClasses,
                AccessFlags.AccPublic,
                methodBodyRemovalResult.RestoreIdx(context.Start.StartIndex),
                methodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex),
                header,
                codeRange
            );
        }

        int PreviousPeerEndPosition(RuleContext parent, IParseTree self)
        {
            return parent switch
            {
                null => -1,
                CParser.StructDeclarationListContext structDeclarationListContext => structDeclarationListContext
                    .structDeclaration()
                    .TakeWhile(it => it != self)
                    .Select(it => methodBodyRemovalResult.RestoreIdx(it.Stop.StopIndex + 1))
                    .DefaultIfEmpty(-1)
                    .Max(),
                CParser.TranslationUnitContext translationUnitContext => translationUnitContext.externalDeclaration()
                    .TakeWhile(it => it != self)
                    .Select(it => methodBodyRemovalResult.RestoreIdx(it.Stop.StopIndex + 1))
                    .DefaultIfEmpty(-1)
                    .Max(),
                _ => PreviousPeerEndPosition(parent.Parent, parent)
            };
        }

        public override AstNode VisitStructDeclaration(CParser.StructDeclarationContext context)
        {
            string fieldName = ExtractPlainFieldName(context)
                               ?? ExtractArrayFieldName(context)
                               ?? ExtractReferenceFieldName(context)
                               ?? ExtractMultiName(context);

            int startIdx = methodBodyRemovalResult.RestoreIdx(context.Start.StartIndex);
            int endIdx = methodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex);
            CodeRange codeRange = IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx);

            return new AstNode.FieldNode(
                fieldName,
                AccessFlags.AccPublic,
                context.GetFullText(),
                startIdx,
                endIdx,
                codeRange
            );
        }

        private static string ExtractMultiName(CParser.StructDeclarationContext context)
        {
            List<string> names = context.structDeclaratorList()
                ?.structDeclarator()
                ?.Select(it => it.declarator().directDeclarator().Identifier().ToString())
                ?.ToList();
            return (names?.Count ?? 0) != 0 ? string.Join(",", names) : null;
        }

        private static string ExtractReferenceFieldName(CParser.StructDeclarationContext context)
        {
            return context.specifierQualifierList()
                ?.specifierQualifierList()
                ?.specifierQualifierList()
                ?.typeSpecifier()
                ?.typedefName()
                ?.Identifier()
                ?.ToString();
        }

        private static string ExtractPlainFieldName(CParser.StructDeclarationContext context)
        {
            return context.specifierQualifierList()
                ?.specifierQualifierList()
                ?.typeSpecifier()
                ?.typedefName()
                ?.Identifier()
                ?.ToString();
        }

        private static string ExtractArrayFieldName(CParser.StructDeclarationContext context)
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

            int startIdx = methodBodyRemovalResult.RestoreIdx(context.Start.StartIndex);
            int endIdx = methodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex);
            CodeRange codeRange = IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx);

            return new AstNode.MethodNode(
                fName,
                AccessFlags.AccPublic,
                text + removedSourceCode ?? "",
                startIdx: startIdx,
                endIdx: endIdx,
                codeRange: codeRange,
                arguments: new List<AstNode.ArgumentNode>()
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