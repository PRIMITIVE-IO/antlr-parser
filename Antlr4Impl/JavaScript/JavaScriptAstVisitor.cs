using System;
using System.Collections.Generic;
using System.Linq;

namespace antlr_parser.Antlr4Impl.JavaScript
{
    public class JavaScriptAstVisitor : JavaScriptParserBaseVisitor<AstNode>
    {
        readonly string FileName;
        private readonly MethodBodyRemovalResult MethodBodyRemovalResult;

        public JavaScriptAstVisitor(string fileName, MethodBodyRemovalResult methodBodyRemovalResult)
        {
            FileName = fileName;
            MethodBodyRemovalResult = methodBodyRemovalResult;
        }

        public override AstNode VisitProgram(JavaScriptParser.ProgramContext context)
        {
            List<AstNode.ClassNode> classes = new List<AstNode.ClassNode>();
            List<AstNode.MethodNode> methods = new List<AstNode.MethodNode>();
            List<AstNode.FieldNode> fields = new List<AstNode.FieldNode>();
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
                .DefaultIfEmpty(context.Stop.StopIndex)
                .Min();

            string header = MethodBodyRemovalResult.RestoreOriginalSubstring(0, headerEnd)
                .Trim();

            return new AstNode.FileNode(
                FileName,
                new AstNode.PackageNode(""),
                classes,
                fields,
                methods,
                header
            );
        }

        public override AstNode VisitFunctionDeclaration(JavaScriptParser.FunctionDeclarationContext context)
        {
            MethodBodyRemovalResult.IdxToRemovedMethodBody.TryGetValue(context.Stop.StopIndex,
                out string removeSourceCode);
            return new AstNode.MethodNode(
                context.identifier().GetFullText(),
                "",
                context.GetFullText() + (removeSourceCode ?? ""),
                context.Start.StartIndex,
                context.Stop.StopIndex
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

            int headerEnd = methodNodes.Select(it => it.StartIdx - 1)
                .Concat(fieldNodes.Select(it => it.StartIdx - 1))
                .Concat(innerClasses.Select(it => it.StartIdx - 1))
                .DefaultIfEmpty(context.Stop.StopIndex)
                .Min();

            string header = MethodBodyRemovalResult.RestoreOriginalSubstring(context.Start.StartIndex, headerEnd);

            return new AstNode.ClassNode(
                context.identifier().GetFullText(),
                methodNodes,
                fieldNodes,
                innerClasses,
                "",
                context.Start.StartIndex,
                context.Stop.StopIndex,
                header
            );
        }

        public override AstNode VisitClassElement(JavaScriptParser.ClassElementContext context)
        {
            return context.methodDefinition().Accept(this);
        }

        public override AstNode VisitMethodDefinition(JavaScriptParser.MethodDefinitionContext context)
        {
            MethodBodyRemovalResult.IdxToRemovedMethodBody.TryGetValue(context.Stop.StopIndex,
                out string removedSourceCode);
            return new AstNode.MethodNode(
                context.propertyName().GetFullText(),
                "",
                context.GetFullText() + (removedSourceCode ?? ""),
                context.Start.StartIndex,
                context.Stop.StopIndex
            );
        }

        public override AstNode VisitVariableDeclaration(JavaScriptParser.VariableDeclarationContext context)
        {
            return new AstNode.FieldNode(
                context.assignable().identifier().GetFullText(),
                "",
                context.GetFullText(),
                context.Start.StartIndex,
                context.Stop.StopIndex
            );
        }
    }
}