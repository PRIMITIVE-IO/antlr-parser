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
            List<AstNode.ClassNode> classes = new List<AstNode.ClassNode>();
            List<AstNode.MethodNode> methods = new List<AstNode.MethodNode>();
            List<AstNode.FieldNode> fields = new List<AstNode.FieldNode>();

            if (context.sourceElements() == null)
            {
                return new AstNode.FileNode(
                    FilePath,
                    null,
                    classes,
                    fields,
                    methods,
                    "",
                    new List<AstNode.Namespace>(),
                    SourceCodeLanguage.JavaScript,
                    false,
                    new CodeRange(new CodeLocation(0, 0), new CodeLocation(0, 0)));
            }

            foreach (JavaScriptParser.SourceElementContext sourceElementContext in context.sourceElements()
                         .sourceElement())
            {
                JavaScriptParser.StatementContext statement = sourceElementContext.statement();


                if (statement.classDeclaration()?.Accept(this) is AstNode.ClassNode klass) classes.Add(klass);
                if (statement.functionDeclaration()?.Accept(this) is AstNode.MethodNode method) methods.Add(method);

                methods.AddRange(statement.expressionStatement()?.expressionSequence()?.singleExpression()
                    ?.Select(singleExpressionContext => singleExpressionContext
                        .GetChild<JavaScriptParser.FunctionDeclContext>(0)
                        ?.GetChild<JavaScriptParser.FunctionDeclarationContext>(0)
                        ?.Accept(this) as AstNode.MethodNode)
                    .Where(it => it != null) ?? new List<AstNode.MethodNode>());

                fields.AddRange(statement.variableStatement()
                        ?.variableDeclarationList()
                        ?.variableDeclaration()
                        ?.Select(variableDecl => variableDecl.Accept(this))
                        .OfType<AstNode.FieldNode>()
                        .ToList() ?? new List<AstNode.FieldNode>()
                );
            }

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
                packageNode: new AstNode.PackageNode(""),
                classes: classes,
                fields: fields,
                methods: methods,
                header: "",
                namespaces: new List<AstNode.Namespace>(),
                language: SourceCodeLanguage.Java,
                isTest: false,
                codeRange: codeRange
            );
        }

        int PreviousPeerEndPosition(RuleContext parent, IParseTree self)
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
                "",
                startIdx: startIdx,
                endIdx: endIdx,
                codeRange: codeRange,
                arguments: new List<AstNode.ArgumentNode>()
            );
        }

        public override AstNode VisitClassDeclaration(JavaScriptParser.ClassDeclarationContext context)
        {
            List<AstNode> classElements = context.classTail().classElement()
                .Select(elem => elem.Accept(this))
                .ToList();

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
                endIdx,
                "",
                codeRange: codeRange
            );
        }

        public override AstNode VisitClassElement(JavaScriptParser.ClassElementContext context)
        {
            if (context.methodDefinition() != null)
            {
                return context.methodDefinition().Accept(this);
            }

            if (context.propertyName() != null)
            {
                // field containing lambda, like `field = x => { return 10 }` 
                int startIdx = MethodBodyRemovalResult.RestoreIdx(context.Start.StartIndex);
                int endIdx = MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex);
                CodeRange codeRange = IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx);

                return new AstNode.MethodNode(
                    name: context.propertyName().GetText(),
                    accFlag: AccessFlags.None,
                    sourceCode: "",
                    startIdx: -1,
                    endIdx: -1,
                    codeRange: codeRange,
                    arguments: new List<AstNode.ArgumentNode>()
                );
            }

            return null;
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
                "",
                startIdx: startIdx,
                endIdx: endIdx,
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
                context.GetFullText(),
                startIdx: startIdx,
                endIdx: endIdx,
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

                    if (furtherDeconstructions.Count == 0)
                    {
                        return property.children.OfType<JavaScriptParser.IdentifierExpressionContext>()
                            .Single()
                            .Accept(this);
                    }
                    else
                    {
                        return furtherDeconstructions.SelectMany(it => it.Accept(this)).ToList();
                    }
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