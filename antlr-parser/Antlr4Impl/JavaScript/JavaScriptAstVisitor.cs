using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
using CodeRange = PrimitiveCodebaseElements.Primitive.dto.CodeRange;

namespace antlr_parser.Antlr4Impl.JavaScript
{
    public class JavaScriptAstVisitor : JavaScriptParserBaseVisitor<AstNode>
    {
        readonly string FilePath;
        readonly MethodBodyRemovalResult MethodBodyRemovalResult;
        readonly IndexToLocationConverter IndexToLocationConverter;
        readonly CodeRangeCalculator CodeRangeCalculator;

        public JavaScriptAstVisitor(
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

        public override AstNode VisitProgram(JavaScriptParser.ProgramContext context)
        {
            List<AstNode> children = AntlrUtil.WalkUntilType(context.children, new HashSet<Type>
                {
                    typeof(JavaScriptParser.FunctionDeclarationContext),
                    typeof(JavaScriptParser.ClassDeclarationContext),
                    typeof(JavaScriptParser.VariableDeclarationContext),
                    typeof(JavaScriptParser.PropertyAssignmentContext)
                },
                this);
            
            List<AstNode.ClassNode> classes = children.OfType<AstNode.ClassNode>().ToList();
            List<AstNode.MethodNode> methods = children.OfType<AstNode.MethodNode>().ToList();
            List<AstNode.FieldNode> fields = children.OfType<AstNode.FieldNode>().ToList();

            int headerEnd = classes.Select(it => it.StartIdx - 1)
                .Concat(methods.Select(it => it.StartIdx - 1))
                .Concat(fields.Select(it => it.StartIdx - 1))
                .DefaultIfEmpty(MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex))
                .Min();

            CodeLocation headerEndLocation = IndexToLocationConverter.IdxToLocation(headerEnd);

            CodeRange codeRange = CodeRangeCalculator.Trim(
                new CodeRange(new CodeLocation(1, 1), headerEndLocation)
            );

            return new AstNode.FileNode(
                path: FilePath,
                packageNode: new AstNode.PackageNode(null),
                classes: classes,
                fields: fields,
                methods: methods,
                namespaces: new List<AstNode.Namespace>(),
                language: SourceCodeLanguage.Java,
                isTest: false,
                codeRange: codeRange
            );
        }

        static int PreviousPeerEndPosition(RuleContext parent, ITree self)
        {
            switch (parent)
            {
                case null:
                    return -1;
                case JavaScriptParser.StatementContext _:
                case JavaScriptParser.SourceElementContext _:
                    return PreviousPeerEndPosition(parent.Parent, parent);
                default:
                {
                    JavaScriptParser.SourceElementsContext sourceElementsContext =
                        parent as JavaScriptParser.SourceElementsContext;

                    return sourceElementsContext.sourceElement()
                        .TakeWhile(it => it != self)
                        .Select(it => it.Stop.StopIndex + 1)
                        .DefaultIfEmpty(-1)
                        .Max();
                }
            }
        }

        public override AstNode VisitFunctionDeclaration(JavaScriptParser.FunctionDeclarationContext context)
        {
            int startIdx = MethodBodyRemovalResult.RestoreIdx(context.Start.StartIndex);
            int endIdx = MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex + 1);
            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx)
            );

            return new AstNode.MethodNode(
                context.identifier().GetFullText(),
                AccessFlags.None,
                startIdx: startIdx,
                codeRange: codeRange,
                arguments: new List<AstNode.ArgumentNode>()
            );
        }

        public override AstNode VisitClassDeclaration(JavaScriptParser.ClassDeclarationContext context)
        {
            List<AstNode> classElements = AntlrUtil.WalkUntilType(context.children, new HashSet<Type>
                {
                    typeof(JavaScriptParser.FunctionDeclarationContext),
                    typeof(JavaScriptParser.ClassDeclarationContext),
                    typeof(JavaScriptParser.MethodDefinitionContext),
                    typeof(JavaScriptParser.VariableDeclarationContext),
                    typeof(JavaScriptParser.PropertyAssignmentContext)
                },
                this);

            List<AstNode.MethodNode> methodNodes = classElements.OfType<AstNode.MethodNode>().ToList();
            List<AstNode.FieldNode> fieldNodes = classElements.OfType<AstNode.FieldNode>().ToList();
            List<AstNode.ClassNode> innerClasses = classElements.OfType<AstNode.ClassNode>().ToList();

            int endIdx = MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex);

            int headerEnd = methodNodes.Select(it => it.StartIdx - 1)
                .Concat(fieldNodes.Select(it => it.StartIdx - 1))
                .Concat(innerClasses.Select(it => it.StartIdx - 1))
                .DefaultIfEmpty(endIdx)
                .Min();

            int headerStart = new[]
            {
                PreviousPeerEndPosition(context.Parent, context),
                0
            }.Max();

            int startIdx = MethodBodyRemovalResult.RestoreIdx(headerStart);
            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(startIdx, headerEnd)
            );

            return new AstNode.ClassNode(
                context.identifier().GetFullText(),
                methodNodes,
                fieldNodes,
                innerClasses,
                AccessFlags.None,
                startIdx,
                codeRange: codeRange
            );
        }

        public override AstNode VisitPropertyExpressionAssignment(JavaScriptParser.PropertyExpressionAssignmentContext context)
        {
            // field containing lambda, like `field = x => { return 10 }` 
            int startIdx = MethodBodyRemovalResult.RestoreIdx(context.Start.StartIndex);
            int endIdx = MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex);
            CodeRange codeRange = IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx);

            return new AstNode.MethodNode(
                name: context.GetText(),
                accFlag: AccessFlags.None,
                startIdx: -1,
                codeRange: codeRange,
                arguments: new List<AstNode.ArgumentNode>()
            );
        }

        public override AstNode VisitMethodDefinition(JavaScriptParser.MethodDefinitionContext context)
        {
            int startIdx = MethodBodyRemovalResult.RestoreIdx(context.Start.StartIndex);
            int endIdx = MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex + 1);

            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx)
            );

            return new AstNode.MethodNode(
                context.propertyName().GetFullText(),
                AccessFlags.None,
                startIdx: startIdx,
                codeRange: codeRange,
                arguments: new List<AstNode.ArgumentNode>()
            );
        }

        public override AstNode VisitVariableDeclaration(JavaScriptParser.VariableDeclarationContext context)
        {
            JavaScriptParser.AssignableContext x = context.assignable();

            string name = "";

            if (x.identifier() != null)
            {
                name = x.identifier().GetFullText();
            }

            if (x.arrayLiteral() != null)
            {
                name = string.Join(",", x.arrayLiteral().Accept(new DeconstructionAssignmentVisitor()));
            }

            if (x.objectLiteral() != null)
            {
                name = string.Join(",", x.objectLiteral().Accept(new DeconstructionAssignmentVisitor()));
            }

            int startIdx = MethodBodyRemovalResult.RestoreIdx(context.Start.StartIndex);

            int endIdx = MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex);

            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx)
            );

            return new AstNode.FieldNode(
                name,
                AccessFlags.None,
                startIdx: startIdx,
                codeRange: codeRange
            );
        }
    }

    public class DeconstructionAssignmentVisitor : JavaScriptParserBaseVisitor<List<string>>
    {
        public override List<string> VisitObjectLiteral(JavaScriptParser.ObjectLiteralContext context)
        {
            return context.propertyAssignment()
                .SelectMany(it => it.Accept(this))
                .ToList();
        }

        public override List<string> VisitPropertyShorthand(JavaScriptParser.PropertyShorthandContext context)
        {
            return new List<string> { context.singleExpression().GetFullText() };
        }

        public override List<string> VisitObjectLiteralExpression(
            JavaScriptParser.ObjectLiteralExpressionContext context)
        {
            JavaScriptParser.PropertyAssignmentContext[] properties = context.objectLiteral().propertyAssignment();

            return properties.SelectMany(property =>
                {
                    List<IParseTree> furtherDeconstructions = property
                        .children.OfType<JavaScriptParser.ObjectLiteralExpressionContext>()
                        .Concat<IParseTree>(property.children.OfType<JavaScriptParser.ArrayLiteralExpressionContext>())
                        .ToList();

                    if (!furtherDeconstructions.Any())
                    {
                        return property.children.OfType<JavaScriptParser.IdentifierExpressionContext>()
                            .Single()
                            .Accept(this);
                    }

                    return furtherDeconstructions.SelectMany(it => it.Accept(this)).ToList();
                })
                .ToList();
        }

        public override List<string> VisitArrayLiteral(JavaScriptParser.ArrayLiteralContext context)
        {
            return context.elementList().arrayElement().SelectMany(it => it.Accept(this)).ToList();
        }

        public override List<string> VisitArrayElement(JavaScriptParser.ArrayElementContext context)
        {
            return context.singleExpression().Accept(this);
        }

        public override List<string> VisitIdentifierExpression(JavaScriptParser.IdentifierExpressionContext context)
        {
            return new List<string> { context.identifier().GetFullText() };
        }
    }
}