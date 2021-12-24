using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using PrimitiveCodebaseElements.Primitive;
using CodeRange = PrimitiveCodebaseElements.Primitive.dto.CodeRange;

namespace antlr_parser.Antlr4Impl.CPP
{
    public class CppAstVisitor : CPP14ParserBaseVisitor<AstNode>
    {
        private readonly string Path;
        private readonly MethodBodyRemovalResult MethodBodyRemovalResult;
        private readonly IndexToLocationConverter IndexToLocationConverter;

        public CppAstVisitor(string path, MethodBodyRemovalResult methodBodyRemovalResult)
        {
            Path = path;
            MethodBodyRemovalResult = methodBodyRemovalResult;
            IndexToLocationConverter = new IndexToLocationConverter(methodBodyRemovalResult.OriginalSource);
        }

        public override AstNode VisitTranslationUnit(CPP14Parser.TranslationUnitContext context)
        {
            List<AstNode> members = context.declarationseq()?.declaration()
                .Select(it => it.Accept(this))
                .Where(it => it != null)
                .ToList() ?? new List<AstNode>();

            List<AstNode.MethodNode> methods = members.OfType<AstNode.MethodNode>().ToList();
            List<AstNode.ClassNode> classes = members.OfType<AstNode.ClassNode>().ToList();
            List<AstNode.FieldNode> fields = members.OfType<AstNode.FieldNode>().ToList();
            List<AstNode.Namespace> namespaces = members.OfType<AstNode.Namespace>().ToList();

            return new AstNode.FileNode(
                Path,
                new AstNode.PackageNode(""),
                classes,
                fields,
                methods,
                "",
                namespaces,
                language: SourceCodeLanguage.Cpp,
                isTest: false,
                codeRange: null //TODO
            );
        }


        public override AstNode VisitNamespaceDefinition(CPP14Parser.NamespaceDefinitionContext context)
        {
            List<AstNode> members = context.declarationseq()?.declaration()
                .Select(it => it.Accept(this))
                .Where(it => it != null)
                .ToList() ?? new List<AstNode>();

            List<AstNode.MethodNode> methods = members.OfType<AstNode.MethodNode>().ToList();
            List<AstNode.ClassNode> classes = members.OfType<AstNode.ClassNode>().ToList();
            List<AstNode.FieldNode> fields = members.OfType<AstNode.FieldNode>().ToList();
            List<AstNode.Namespace> namespaces = members.OfType<AstNode.Namespace>().ToList();

            return new AstNode.Namespace(classes, fields, methods, namespaces);
        }

        public override AstNode VisitDeclaration(CPP14Parser.DeclarationContext context)
        {
            AstNode node2 = new List<Func<ParserRuleContext>>
                {
                    () => context.templateDeclaration(),
                    () => context.namespaceDefinition(),
                    () => context.functionDefinition(),
                }
                .Select(it => it()?.Accept(this))
                .FirstOrDefault(it => it != null);
            if (node2 != null)
            {
                return node2;
            }

            IEnumerable<string> fieldNames = context.blockDeclaration()
                ?.simpleDeclaration()
                ?.initDeclaratorList()
                ?.initDeclarator()
                .Select(it => ExtractFieldNameOrNull(it.declarator()))
                .Where(it => it != null)
                .ToList() ?? new List<string>();

            int startIdx = MethodBodyRemovalResult.RestoreIdx(context.Start.StartIndex);
            int endIdx = MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex);
            CodeRange codeRange = IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx);
            if (fieldNames.Count() != 0)
            {
                return new AstNode.FieldNode(
                    String.Join(",", fieldNames),
                    AccessFlags.AccPublic,
                    context.GetFullText(),
                    startIdx,
                    endIdx,
                    codeRange: codeRange
                );
            }

            AstNode node = context.blockDeclaration()?.simpleDeclaration()?.declSpecifierSeq()?.Accept(this);

            if (node != null)
            {
                return node;
            }

            string methodName = ExtractUnqualifiedMethodName(
                context.blockDeclaration()
                    ?.simpleDeclaration()
                    ?.initDeclaratorList()
                    ?.initDeclarator()
                    ?.Single()
                    ?.declarator()
            );

            if (methodName != null)
            {
                string sourceCode = MethodBodyRemovalResult.ExtractOriginalSubstring(
                    startIdx,
                    endIdx)
                    .TrimIndent().Trim();

                return new AstNode.MethodNode(
                    methodName,
                    AccessFlags.AccPublic,
                    sourceCode,
                    startIdx,
                    endIdx,
                    codeRange
                );
            }

            try
            {
                PrimitiveLogger.Logger.Instance().Warn($"Cannot parse DeclarationContext: {context.GetFullText()}");
            }
            catch (Exception e)
            {
                PrimitiveLogger.Logger.Instance()
                    .Error($"Cannot parse DeclarationContext for unknown source in: {Path}", e);
            }

            return null;
        }

        public override AstNode VisitTemplateDeclaration(CPP14Parser.TemplateDeclarationContext context)
        {
            return context.declaration()?.functionDefinition()?.Accept(this);
        }

        public override AstNode VisitDeclSpecifierSeq(CPP14Parser.DeclSpecifierSeqContext context)
        {
            CPP14Parser.ClassSpecifierContext classSpecifier = context.declSpecifier()
                .Select(it => it.typeSpecifier()?.classSpecifier())
                .FirstOrDefault(it => it != null);

            if (classSpecifier != null)
            {
                return classSpecifier.Accept(this) as AstNode.ClassNode;
            }

            CPP14Parser.EnumSpecifierContext enumSpecifier = context.declSpecifier()
                .Select(it => it.typeSpecifier()?.enumSpecifier())
                .FirstOrDefault(it => it != null);

            if (enumSpecifier != null)
            {
                return enumSpecifier.Accept(this) as AstNode.ClassNode;
            }

            CPP14Parser.ElaboratedTypeSpecifierContext elaboratedTypeSpecifierContext = context.declSpecifier()
                .Select(it => it?.typeSpecifier()
                    ?.trailingTypeSpecifier()
                    ?.elaboratedTypeSpecifier())
                .FirstOrDefault(it => it != null);

            if (elaboratedTypeSpecifierContext != null)
            {
                return elaboratedTypeSpecifierContext.Accept(this) as AstNode.ClassNode;
            }

            return null;
        }

        public override AstNode VisitElaboratedTypeSpecifier(CPP14Parser.ElaboratedTypeSpecifierContext context)
        {
            string name = context.Identifier().GetText();
            int headerStart = PreviousPeerEndPosition(context.Parent, context) + 1;
            int headerEnd = MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex);
            CodeRange codeRange = IndexToLocationConverter.IdxToCodeRange(headerStart, headerEnd);

            string header = MethodBodyRemovalResult.ExtractOriginalSubstring(
                    headerStart,
                    headerEnd)
                .TrimIndent().Trim();

            
            return new AstNode.ClassNode(
                name,
                new List<AstNode.MethodNode>(),
                new List<AstNode.FieldNode>(),
                new List<AstNode.ClassNode>(),
                AccessFlags.AccPublic,
                headerStart,
                headerEnd,
                header,
                codeRange: codeRange
            );
        }

        public override AstNode VisitFunctionDefinition(CPP14Parser.FunctionDefinitionContext context)
        {
            string methodName = ExtractUnqualifiedMethodName(context.declarator())
                                ?? ExtractQualifiedMethodName(context.declarator())
                                ?? ExtractUnqualifiedOperatorName(context.declarator())
                                ?? ExtractQualifiedOperatorName(context.declarator())
                                ?? ExtractConversionOperatorName(context.declarator())
                                ?? ExtractDestructorName(context.declarator())
                ;

            if (methodName != null)
            {
                int startIdx = MethodBodyRemovalResult.RestoreIdx(context.Start.StartIndex);
                int endIdx = MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex);
                CodeRange codeRange = IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx);

                string sourceCode = MethodBodyRemovalResult
                    .ExtractOriginalSubstring(startIdx, endIdx)
                    .TrimIndent()
                    .Trim();

                return new AstNode.MethodNode(
                    methodName,
                    AccessFlags.AccPublic,
                    sourceCode,
                    startIdx,
                    endIdx,
                    codeRange: codeRange
                );
            }

            return null;
        }

        public override AstNode VisitClassSpecifier(CPP14Parser.ClassSpecifierContext context)
        {
            string name = context.classHead()?.classHeadName()?.className()?.Identifier()?.GetText() ?? "";
            List<AstNode> members = context.memberSpecification()?.memberdeclaration()
                .Select(it => it.Accept(this))
                .Where(it => it != null)
                .ToList() ?? new List<AstNode>();

            List<AstNode.FieldNode> fields = members.OfType<AstNode.FieldNode>().ToList();
            List<AstNode.MethodNode> methods = members.OfType<AstNode.MethodNode>().ToList();
            List<AstNode.ClassNode> classes = members.OfType<AstNode.ClassNode>().ToList();

            int headerStart = PreviousPeerEndPosition(context, context.Parent) + 1;
            int headerEnd = MethodBodyRemovalResult.RestoreIdx(context.LeftBrace().Symbol.StartIndex);
            CodeRange codeRange = IndexToLocationConverter.IdxToCodeRange(headerStart, headerEnd);

            string header = MethodBodyRemovalResult
                .ExtractOriginalSubstring(headerStart, headerEnd)
                .TrimIndent()
                .Trim();

            return new AstNode.ClassNode(
                name,
                methods,
                fields,
                classes,
                AccessFlags.AccPublic,
                headerStart,
                headerEnd,
                header,
                codeRange: codeRange
            );
        }

        public override AstNode VisitEnumSpecifier(CPP14Parser.EnumSpecifierContext context)
        {
            int headerStart = PreviousPeerEndPosition(context, context.Parent) + 1;
            int headerEnd = MethodBodyRemovalResult.RestoreIdx(context.LeftBrace().Symbol.StartIndex);
            CodeRange codeRange = IndexToLocationConverter.IdxToCodeRange(headerStart, headerEnd);

            string header = MethodBodyRemovalResult
                .ExtractOriginalSubstring(headerStart, headerEnd)
                .TrimIndent()
                .Trim();

            return new AstNode.ClassNode(
                context.enumHead().Identifier().GetText(),
                new List<AstNode.MethodNode>(),
                new List<AstNode.FieldNode>(),
                new List<AstNode.ClassNode>(),
                AccessFlags.AccPublic,
                headerStart,
                headerEnd,
                header,
                codeRange: codeRange
            );
        }

        string ExtractFieldNameOrNull(CPP14Parser.DeclaratorContext context)
        {
            return context?.pointerDeclarator()
                ?.noPointerDeclarator()
                ?.declaratorid()
                ?.idExpression()
                ?.unqualifiedId()
                ?.Identifier()
                ?.GetText();
        }

        public override AstNode VisitMemberdeclaration(CPP14Parser.MemberdeclarationContext context)
        {
            if (context.functionDefinition() != null)
            {
                return context.functionDefinition().Accept(this) as AstNode.MethodNode;
            }

            List<string> fieldNames = context.memberDeclaratorList()
                ?.memberDeclarator()
                .Select(it => ExtractFieldNameOrNull(it.declarator()))
                .Where(it => it != null)
                .ToList() ?? new List<string>();

            int startIdx = MethodBodyRemovalResult.RestoreIdx(context.Start.StartIndex);
            int endIdx = MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex);
            CodeRange codeRange = IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx);
            
            if (fieldNames.Count != 0)
            {
                return new AstNode.FieldNode(
                    String.Join(",", fieldNames),
                    AccessFlags.AccPublic,
                    context.GetFullText(),
                    startIdx,
                    endIdx,
                    codeRange: codeRange
                );
            }


            string methodName = ExtractUnqualifiedMethodName(context.memberDeclaratorList()
                ?.memberDeclarator()
                ?.Single()
                ?.declarator()
            );

            if (methodName != null)
            {
                string sourceCode = MethodBodyRemovalResult
                    .ExtractOriginalSubstring(startIdx, endIdx)
                    .TrimIndent()
                    .Trim();

                return new AstNode.MethodNode(
                    methodName,
                    AccessFlags.AccPublic,
                    sourceCode,
                    startIdx,
                    endIdx,
                    codeRange: codeRange
                );
            }

            AstNode node = context.declSpecifierSeq()?.Accept(this);

            if (node != null)
            {
                return node;
            }

            PrimitiveLogger.Logger.Instance().Warn($"Cannot parse MemberdeclarationContext: {context.GetFullText()}");

            return null;
        }

        CPP14Parser.IdExpressionContext ExtractIdExpression(CPP14Parser.DeclaratorContext declarator)
        {
            return declarator
                ?.pointerDeclarator()
                ?.noPointerDeclarator()
                ?.noPointerDeclarator()
                ?.declaratorid()
                ?.idExpression();
        }

        string ExtractDestructorName(CPP14Parser.DeclaratorContext declarator)
        {
            return ExtractIdExpression(declarator)
                ?.qualifiedId()
                ?.unqualifiedId()
                ?.GetText();
        }

        string ExtractUnqualifiedMethodName(CPP14Parser.DeclaratorContext declarator)
        {
            return ExtractIdExpression(declarator)
                ?.unqualifiedId()
                ?.Identifier()
                ?.GetText();
        }

        string ExtractQualifiedMethodName(CPP14Parser.DeclaratorContext declarator)
        {
            return ExtractIdExpression(declarator)
                ?.qualifiedId()
                ?.unqualifiedId()
                ?.Identifier()
                ?.GetText();
        }

        string ExtractUnqualifiedOperatorName(CPP14Parser.DeclaratorContext declarator)
        {
            return ExtractIdExpression(declarator)
                ?.unqualifiedId()
                ?.operatorFunctionId()
                ?.theOperator()
                ?.GetText();
        }

        string ExtractQualifiedOperatorName(CPP14Parser.DeclaratorContext declarator)
        {
            return ExtractIdExpression(declarator)
                ?.qualifiedId()
                ?.unqualifiedId()
                ?.operatorFunctionId()
                ?.theOperator()
                ?.GetText();
        }

        string ExtractConversionOperatorName(CPP14Parser.DeclaratorContext declarator)
        {
            return ExtractIdExpression(declarator)
                ?.qualifiedId()
                ?.unqualifiedId()
                ?.conversionFunctionId()
                ?.conversionTypeId()
                ?.typeSpecifierSeq()
                ?.typeSpecifier()
                ?.Single()
                ?.trailingTypeSpecifier()
                ?.simpleTypeSpecifier()
                ?.GetText();
        }

        int PreviousPeerEndPosition(RuleContext parent, IParseTree self)
        {
            if (parent == null)
            {
                return -1;
            }

            if (parent is CPP14Parser.DeclarationseqContext)
            {
                CPP14Parser.DeclarationseqContext declarationseq = parent as CPP14Parser.DeclarationseqContext;
                return declarationseq.children
                    .TakeWhile(it => it != self)
                    .OfType<CPP14Parser.DeclarationContext>()
                    .Select(it => MethodBodyRemovalResult.RestoreIdx(it.Stop.StopIndex))
                    .DefaultIfEmpty(-1)
                    .Last();
            }

            return PreviousPeerEndPosition(parent.Parent, parent);
        }
    }
}