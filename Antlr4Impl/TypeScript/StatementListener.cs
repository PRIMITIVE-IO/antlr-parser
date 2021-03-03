using System;
using System.Collections.Generic;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.TypeScript
{
    public class StatementListener : TypeScriptParserBaseListener
    {
        readonly ClassInfo outerClassInfo;

        public StatementListener(ClassInfo outerClassInfo)
        {
            this.outerClassInfo = outerClassInfo;
        }

        public override void EnterStatement(TypeScriptParser.StatementContext context)
        {
            DoStatement(context, outerClassInfo);
        }

        static void DoStatement(TypeScriptParser.StatementContext context, ClassInfo outerClassInfo)
        {
            if (context.classDeclaration() != null)
            {
                ClassDeclarationListener classDeclarationListener = new ClassDeclarationListener(outerClassInfo);
                context.classDeclaration().EnterRule(classDeclarationListener);
            }

            if (context.functionDeclaration() != null)
            {
                FunctionDeclarationListener functionDeclarationListener =
                    new FunctionDeclarationListener(outerClassInfo);
                context.functionDeclaration().EnterRule(functionDeclarationListener);
            }

            if (context.expressionStatement() != null)
            {
                ExpressionStatementListener expressionStatementListener =
                    new ExpressionStatementListener(outerClassInfo);
                context.expressionStatement().EnterRule(expressionStatementListener);
            }

            if (context.variableStatement() != null)
            {
                VariableStatementListener variableStatementListener = new VariableStatementListener(outerClassInfo);
                context.variableStatement().EnterRule(variableStatementListener);
            }
        }
    }

    public class ExpressionStatementListener : TypeScriptParserBaseListener
    {
        readonly ClassInfo outerClassInfo;

        public ExpressionStatementListener(ClassInfo outerClassInfo)
        {
            this.outerClassInfo = outerClassInfo;
        }

        public override void EnterExpressionStatement(TypeScriptParser.ExpressionStatementContext context)
        {
            if (context.expressionSequence() != null)
            {
                ExpressionSequenceListener expressionSequenceListener = new ExpressionSequenceListener(outerClassInfo);
                context.expressionSequence().EnterRule(expressionSequenceListener);
            }
        }
    }

    public class ExpressionSequenceListener : TypeScriptParserBaseListener
    {
        readonly ClassInfo outerClassInfo;

        public ExpressionSequenceListener(ClassInfo outerClassInfo)
        {
            this.outerClassInfo = outerClassInfo;
        }

        public override void EnterExpressionSequence(TypeScriptParser.ExpressionSequenceContext context)
        {
            foreach (TypeScriptParser.SingleExpressionContext singleExpressionContext in context.singleExpression())
            {
                Console.WriteLine($"single expression: {singleExpressionContext.GetFullText()}");
            }
        }
    }

    /// <summary>
    /// Function Declarations are at the top statement level. Unlike methods that are inside of classes.
    /// </summary>
    public class FunctionDeclarationListener : TypeScriptParserBaseListener
    {
        readonly ClassInfo outerClassInfo;

        public FunctionDeclarationListener(ClassInfo outerClassInfo)
        {
            this.outerClassInfo = outerClassInfo;
        }

        public override void EnterFunctionDeclaration(TypeScriptParser.FunctionDeclarationContext context)
        {
            // TODO
            List<Argument> arguments = new List<Argument>();
            TypeName returnType = TypeName.For("void");

            MethodName expressionMethodName = new MethodName(
                outerClassInfo.className,
                context.Identifier().GetText(),
                returnType.Signature,
                arguments);

            MethodInfo expressionMethodInfo = new MethodInfo(
                expressionMethodName,
                AccessFlags.AccPublic,
                outerClassInfo.className,
                arguments,
                returnType,
                new SourceCodeSnippet(context.GetFullText(), SourceCodeLanguage.TypeScript));

            outerClassInfo.Children.Add(expressionMethodInfo);
        }
    }
}