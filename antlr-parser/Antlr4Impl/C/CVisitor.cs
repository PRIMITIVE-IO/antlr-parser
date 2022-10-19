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
        readonly string FilePath;
        readonly MethodBodyRemovalResult MethodBodyRemovalResult;
        readonly IndexToLocationConverter IndexToLocationConverter;
        readonly CodeRangeCalculator CodeRangeCalculator;

        public CVisitor(
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

        #region VISITORS

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
                .DefaultIfEmpty(
                    MethodBodyRemovalResult.RestoreIdx(context.Stop?.StopIndex ??
                                                       context.Start
                                                           .StartIndex)) // for 'empty' files context starts at the end of file
                .Min();

            CodeRange headerCodeRange = CodeRangeCalculator.Trim(
                new CodeRange(
                    new CodeLocation(1, 1),
                    IndexToLocationConverter.IdxToLocation(headerEnd)
                )
            );

            return new AstNode.FileNode(
                path: FilePath,
                packageNode: null,
                classes: structs,
                fields: new List<AstNode.FieldNode>(),
                methods: methods,
                namespaces: new List<AstNode.Namespace>(),
                language: SourceCodeLanguage.C,
                isTest: false,
                codeRange: headerCodeRange
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
                .DefaultIfEmpty(MethodBodyRemovalResult.RestoreIdx(context.Stop?.StopIndex ?? 0))
                .Min();

            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(headerStart, headerEnd)
            );

            return new AstNode.ClassNode(
                name: name,
                methods: new List<AstNode.MethodNode>(),
                fields: fields,
                innerClasses: innerClasses,
                modifier: AccessFlags.AccPublic,
                startIdx: MethodBodyRemovalResult.RestoreIdx(context.Start.StartIndex),
                codeRange: codeRange
            );
        }

        public override AstNode VisitStructDeclaration(CParser.StructDeclarationContext context)
        {
            string fieldName = ExtractPlainFieldName(context)
                               ?? ExtractArrayFieldName(context)
                               ?? ExtractReferenceFieldName(context)
                               ?? ExtractMultiName(context);

            int startIdx = MethodBodyRemovalResult.RestoreIdx(context.Start.StartIndex);
            int endIdx = MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex);

            CodeRange codeRange = CodeRangeCalculator.Trim(IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx));

            return new AstNode.FieldNode(
                name: fieldName,
                accFlag: AccessFlags.AccPublic,
                startIdx: startIdx,
                codeRange: codeRange
            );
        }

        public override AstNode VisitFunctionDefinition(CParser.FunctionDefinitionContext context)
        {
            string fName = ExtractFunctionName(context.declarator().directDeclarator());

            int startIdx = MethodBodyRemovalResult.RestoreIdx(context.Start.StartIndex);
            //+1 forces to restore removed part
            int endIdx = MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex + 1);

            CodeRange codeRange = CodeRangeCalculator.Trim(IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx));

            return new AstNode.MethodNode(
                name: fName,
                accFlag: AccessFlags.AccPublic,
                startIdx: startIdx,
                codeRange: codeRange,
                arguments: new List<AstNode.ArgumentNode>(),
                returnType: "void"
            );
        }

        #endregion

        #region UTIL
        
        int PreviousPeerEndPosition(RuleContext parent, ITree self)
        {
            return parent switch
            {
                null => -1,
                CParser.StructDeclarationListContext structDeclarationListContext => structDeclarationListContext
                    .structDeclaration()
                    .TakeWhile(it => it != self)
                    .Select(it => MethodBodyRemovalResult.RestoreIdx(it.Stop.StopIndex + 1))
                    .DefaultIfEmpty(-1)
                    .Max(),
                CParser.TranslationUnitContext translationUnitContext => translationUnitContext.externalDeclaration()
                    .TakeWhile(it => it != self)
                    .Select(it => MethodBodyRemovalResult.RestoreIdx(it.Stop.StopIndex + 1))
                    .DefaultIfEmpty(-1)
                    .Max(),
                _ => PreviousPeerEndPosition(parent.Parent, parent)
            };
        }

        static string ExtractMultiName(CParser.StructDeclarationContext context)
        {
            List<string> names = context.structDeclaratorList()
                ?.structDeclarator()
                ?.Select(it => it.declarator().directDeclarator().Identifier().ToString())
                .ToList();
            return (names?.Count ?? 0) != 0 ? string.Join(",", names) : null;
        }

        static string? ExtractReferenceFieldName(CParser.StructDeclarationContext context)
        {
            return context.specifierQualifierList()
                ?.specifierQualifierList()
                ?.specifierQualifierList()
                ?.typeSpecifier()
                ?.typedefName()
                ?.Identifier()
                ?.ToString();
        }

        static string? ExtractPlainFieldName(CParser.StructDeclarationContext context)
        {
            return context.specifierQualifierList()
                ?.specifierQualifierList()
                ?.typeSpecifier()
                ?.typedefName()
                ?.Identifier()
                ?.ToString();
        }

        static string? ExtractArrayFieldName(CParser.StructDeclarationContext context)
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

        static string ExtractFunctionName(CParser.DirectDeclaratorContext context)
        {
            if (context.directDeclarator() == null)
            {
                return context.GetText();
            }

            return ExtractFunctionName(context.directDeclarator());
        }
        
        #endregion
    }
}