using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
                                 ?? ImmutableList<AstNode.MethodNode>.Empty);

                fields.AddRange(statement.variableStatement()
                                    ?.variableDeclarationList()
                                    ?.variableDeclaration()
                                    ?.Select(variableDecl => variableDecl.Accept(this))
                                    .OfType<AstNode.FieldNode>()
                                    .ToImmutableList()
                                ?? ImmutableList<AstNode.FieldNode>.Empty
                );
            }

            return new AstNode.FileNode(
                FileName,
                new AstNode.PackageNode(""),
                classes.ToImmutableList(),
                fields.ToImmutableList(),
                methods.ToImmutableList()
            );
        }

        public override AstNode VisitFunctionDeclaration(JavaScriptParser.FunctionDeclarationContext context)
        {
            return new AstNode.MethodNode(
                context.identifier().GetFullText(),
                "",
                context.GetFullText() +
                (MethodBodyRemovalResult.IdxToRemovedMethodBody.GetValueOrDefault(context.Stop.StopIndex) ?? "")
            );
        }


        public override AstNode VisitClassDeclaration(JavaScriptParser.ClassDeclarationContext context)
        {
            ImmutableList<AstNode> classElements = context.classTail().classElement()
                .Select(elem => elem.Accept(this))
                .ToImmutableList();

            return new AstNode.ClassNode(
                context.identifier().GetFullText(),
                classElements.OfType<AstNode.MethodNode>().ToImmutableList(),
                classElements.OfType<AstNode.FieldNode>().ToImmutableList(),
                classElements.OfType<AstNode.ClassNode>().ToImmutableList(),
                ""
            );
        }

        public override AstNode VisitClassElement(JavaScriptParser.ClassElementContext context)
        {
            return context.methodDefinition().Accept(this);
        }

        public override AstNode VisitMethodDefinition(JavaScriptParser.MethodDefinitionContext context)
        {
            try
            {
                return new AstNode.MethodNode(
                    context.propertyName().GetFullText(),
                    "",
                    context.GetFullText() +
                    (MethodBodyRemovalResult.IdxToRemovedMethodBody.GetValueOrDefault(context.Stop.StopIndex) ?? "")
                );
            }
            catch (Exception ex)
            {
                return null; //TODO
            }
        }

        public override AstNode VisitVariableDeclaration(JavaScriptParser.VariableDeclarationContext context)
        {
            return new AstNode.FieldNode(context.assignable().identifier().GetFullText(), "", context.GetFullText());
        }
    }
}