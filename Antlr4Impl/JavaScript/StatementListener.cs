using System.Collections.Generic;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.JavaScript
{
    public class StatementListener : JavaScriptParserBaseListener
    {
        readonly ClassInfo outerClassInfo;

        public StatementListener(ClassInfo outerClassInfo)
        {
            this.outerClassInfo = outerClassInfo;
        }

        public override void EnterStatement(JavaScriptParser.StatementContext context)
        {
            DoStatement(context, outerClassInfo);
        }

        static void DoStatement(JavaScriptParser.StatementContext context, ClassInfo outerClassInfo)
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

    public class ExpressionStatementListener : JavaScriptParserBaseListener
    {
        readonly ClassInfo outerClassInfo;

        public ExpressionStatementListener(ClassInfo outerClassInfo)
        {
            this.outerClassInfo = outerClassInfo;
        }

        public override void EnterExpressionStatement(JavaScriptParser.ExpressionStatementContext context)
        {
            ExpressionSequenceListener expressionSequenceListener = new ExpressionSequenceListener(outerClassInfo);
            context.expressionSequence().EnterRule(expressionSequenceListener);
        }
    }

    public class ExpressionSequenceListener : JavaScriptParserBaseListener
    {
        readonly ClassInfo outerClassInfo;

        public ExpressionSequenceListener(ClassInfo outerClassInfo)
        {
            this.outerClassInfo = outerClassInfo;
        }

        public override void EnterExpressionSequence(JavaScriptParser.ExpressionSequenceContext context)
        {
            foreach (JavaScriptParser.SingleExpressionContext singleExpressionContext in context.singleExpression())
            {
                JavaScriptParser.FunctionDeclContext functionDeclContext =
                    singleExpressionContext.GetChild<JavaScriptParser.FunctionDeclContext>(0);
                if (functionDeclContext != null)
                {
                    JavaScriptParser.FunctionDeclarationContext functionDeclarationContext =
                        functionDeclContext.GetChild<JavaScriptParser.FunctionDeclarationContext>(0);
                    FunctionDeclarationListener functionDeclarationListener =
                        new FunctionDeclarationListener(outerClassInfo);
                    functionDeclarationContext.EnterRule(functionDeclarationListener);
                }
            }
        }
    }

    /// <summary>
    /// Function Declarations are at the top statement level. Unlike methods that are inside of classes.
    /// </summary>
    public class FunctionDeclarationListener : JavaScriptParserBaseListener
    {
        readonly ClassInfo outerClassInfo;

        public FunctionDeclarationListener(ClassInfo outerClassInfo)
        {
            this.outerClassInfo = outerClassInfo;
        }

        public override void EnterFunctionDeclaration(JavaScriptParser.FunctionDeclarationContext context)
        {
            // TODO
            List<Argument> arguments = new List<Argument>();
            TypeName returnType = TypeName.For("void");

            MethodName expressionMethodName = new MethodName(
                outerClassInfo.className,
                context.identifier().Identifier().GetText(),
                returnType.Signature,
                arguments);

            MethodInfo expressionMethodInfo = new MethodInfo(
                expressionMethodName,
                AccessFlags.AccPublic,
                outerClassInfo.className,
                arguments,
                returnType,
                new SourceCodeSnippet(context.GetFullText(), SourceCodeLanguage.JavaScript));

            outerClassInfo.Children.Add(expressionMethodInfo);
        }
    }
}