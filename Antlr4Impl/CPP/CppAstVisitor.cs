using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;

namespace antlr_parser.Antlr4Impl.CPP
{
    public class CppAstVisitor : CPP14ParserBaseVisitor<AstNode>
    {
        string Path;

        public CppAstVisitor(string path)
        {
            Path = path;
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
                "", //TODO
                namespaces
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

            if (fieldNames.Count() != 0)
            {
                return new AstNode.FieldNode(
                    String.Join(",", fieldNames),
                    "",
                    context.GetFullText(),
                    context.Start.StartIndex,
                    context.Stop.StopIndex
                );
            }

            AstNode node = context.blockDeclaration()?.simpleDeclaration()?.declSpecifierSeq()?.Accept(this);

            if (node != null)
            {
                return node;
            }

            string methodName = extractUnqualifiedMethodName(
                context.blockDeclaration()
                    ?.simpleDeclaration()
                    ?.initDeclaratorList()
                    ?.initDeclarator()
                    ?.Single()
                    ?.declarator()
            );

            if (methodName != null)
            {
                return new AstNode.MethodNode(
                    methodName,
                    "",
                    context.GetFullText().TrimIndent(),
                    context.Start.StartIndex,
                    context.Stop.StopIndex
                );
            }

            try
            {
                Console.WriteLine($"Cannot parse DeclarationContext: {context.GetFullText()}");
            }
            catch (Exception _)
            {
                Console.WriteLine($"Cannot parse DeclarationContext for unknown source in: {Path}");
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
            return new AstNode.ClassNode(
                name,
                new List<AstNode.MethodNode>(),
                new List<AstNode.FieldNode>(),
                new List<AstNode.ClassNode>(),
                "",
                context.Start.StartIndex,
                context.Stop.StopIndex,
                context.GetFullText()
            );
        }

        public override AstNode VisitFunctionDefinition(CPP14Parser.FunctionDefinitionContext context)
        {
            string methodName = extractUnqualifiedMethodName(context.declarator())
                                ?? extractQualifiedMethodName(context.declarator())
                                ?? extractUnqualifiedOperatorName(context.declarator())
                                ?? extractQualifiedOperatorName(context.declarator())
                                ?? extractConversionOperatorName(context.declarator())
                ;

            if (methodName != null)
            {
                return new AstNode.MethodNode(
                    methodName,
                    "",
                    context.GetFullText().TrimIndent(),
                    context.Start.StartIndex,
                    context.Stop.StopIndex
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

            return new AstNode.ClassNode(
                name,
                methods,
                fields,
                classes,
                "",
                context.Start.StartIndex,
                context.Stop.StopIndex,
                context.GetFullText() // TODO 
            );
        }

        public override AstNode VisitEnumSpecifier(CPP14Parser.EnumSpecifierContext context)
        {
            return new AstNode.ClassNode(
                context.enumHead().Identifier().GetText(),
                new List<AstNode.MethodNode>(),
                new List<AstNode.FieldNode>(),
                new List<AstNode.ClassNode>(),
                "",
                context.Start.StartIndex,
                context.Stop.StopIndex,
                context.GetFullText()
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

            if (fieldNames.Count != 0)
            {
                return new AstNode.FieldNode(
                    String.Join(",", fieldNames),
                    "",
                    context.GetFullText(),
                    context.Start.StartIndex,
                    context.Stop.StopIndex
                );
            }


            string methodName = extractUnqualifiedMethodName(context.memberDeclaratorList()
                ?.memberDeclarator()
                ?.Single()
                ?.declarator()
            );

            if (methodName != null)
            {
                return new AstNode.MethodNode(
                    methodName,
                    "",
                    context.GetFullText().TrimIndent(),
                    context.Start.StartIndex,
                    context.Stop.StopIndex
                );
            }

            AstNode node = context.declSpecifierSeq()?.Accept(this);

            if (node != null)
            {
                return node;
            }

            Console.WriteLine($"Cannot parse MemberdeclarationContext: {context.GetFullText()}");
            return null;
        }

        string extractUnqualifiedMethodName(CPP14Parser.DeclaratorContext declarator)
        {
            return declarator
                ?.pointerDeclarator()
                ?.noPointerDeclarator()
                ?.noPointerDeclarator()
                ?.declaratorid()
                ?.idExpression()
                ?.unqualifiedId()
                ?.Identifier()
                ?.GetText();
        }

        string extractQualifiedMethodName(CPP14Parser.DeclaratorContext declarator)
        {
            return declarator
                ?.pointerDeclarator()
                ?.noPointerDeclarator()
                ?.noPointerDeclarator()
                ?.declaratorid()
                ?.idExpression()
                ?.qualifiedId()
                ?.unqualifiedId()
                ?.Identifier()
                ?.GetText();
        }

        string extractUnqualifiedOperatorName(CPP14Parser.DeclaratorContext declarator)
        {
            return declarator
                ?.pointerDeclarator()
                ?.noPointerDeclarator()
                ?.noPointerDeclarator()
                ?.declaratorid()
                ?.idExpression()
                ?.unqualifiedId()
                ?.operatorFunctionId()
                ?.theOperator()
                ?.GetText();
        }

        string extractQualifiedOperatorName(CPP14Parser.DeclaratorContext declarator)
        {
            return declarator
                ?.pointerDeclarator()
                ?.noPointerDeclarator()
                ?.noPointerDeclarator()
                ?.declaratorid()
                ?.idExpression()
                ?.qualifiedId()
                ?.unqualifiedId()
                ?.operatorFunctionId()
                ?.theOperator()
                ?.GetText();
        }

        string extractConversionOperatorName(CPP14Parser.DeclaratorContext declarator)
        {
            return declarator
                ?.pointerDeclarator()
                ?.noPointerDeclarator()
                ?.noPointerDeclarator()
                ?.declaratorid()
                ?.idExpression()
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
    }
}