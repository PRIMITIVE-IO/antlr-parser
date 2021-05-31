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
                                     .Where(it => it != null)
                                 ?? new List<AstNode.MethodNode>());

                fields.AddRange(statement.variableStatement()
                                    ?.variableDeclarationList()
                                    ?.variableDeclaration()
                                    ?.Select(variableDecl => variableDecl.Accept(this))
                                    .OfType<AstNode.FieldNode>()
                                    .ToList()
                                ?? new List<AstNode.FieldNode>()
                );
            }

            return new AstNode.FileNode(
                FileName,
                new AstNode.PackageNode(""),
                classes.ToList(),
                fields.ToList(),
                methods.ToList()
            );
        }

        public override AstNode VisitFunctionDeclaration(JavaScriptParser.FunctionDeclarationContext context)
        {
            MethodBodyRemovalResult.IdxToRemovedMethodBody.TryGetValue(context.Stop.StopIndex,
                out string removeSourceCode);
            return new AstNode.MethodNode(
                context.identifier().GetFullText(),
                "",
                context.GetFullText() + (removeSourceCode ?? "")
            );
        }


        public override AstNode VisitClassDeclaration(JavaScriptParser.ClassDeclarationContext context)
        {
            List<AstNode> classElements = context.classTail().classElement()
                .Select(elem => elem.Accept(this))
                .ToList();

            return new AstNode.ClassNode(
                context.identifier().GetFullText(),
                classElements.OfType<AstNode.MethodNode>().ToList(),
                classElements.OfType<AstNode.FieldNode>().ToList(),
                classElements.OfType<AstNode.ClassNode>().ToList(),
                ""
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
                context.GetFullText() + (removedSourceCode ?? "")
            );
        }

        public override AstNode VisitVariableDeclaration(JavaScriptParser.VariableDeclarationContext context)
        {
            return new AstNode.FieldNode(context.assignable().identifier().GetFullText(), "", context.GetFullText());
        }
    }
}