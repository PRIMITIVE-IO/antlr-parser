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

            if (context.interfaceDeclaration() != null)
            {
                
            }

            if (context.enumDeclaration() != null)
            {
                
            }
        }
        
        class ExpressionStatementListener : TypeScriptParserBaseListener
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
            
            class ExpressionSequenceListener : TypeScriptParserBaseListener
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
                        outerClassInfo.SourceCode = new SourceCodeSnippet(
                            outerClassInfo.SourceCode.Text + '\n' + singleExpressionContext.GetFullText(),
                            SourceCodeLanguage.TypeScript);
                    }
                }
            }
        }
        
        /// <summary>
        /// Function Declarations are at the top statement level. Unlike methods that are inside of classes.
        /// </summary>
        class FunctionDeclarationListener : TypeScriptParserBaseListener
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

                if (context.callSignature() != null)
                {
                    CallSignatureListener callSignatureListener = new CallSignatureListener();
                    context.callSignature().EnterRule(callSignatureListener);

                    if (!string.IsNullOrEmpty(callSignatureListener.TypeString))
                    {
                        returnType = TypeName.For(callSignatureListener.TypeString);
                    }
                }

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

            class CallSignatureListener : TypeScriptParserBaseListener
            {
                public string TypeString = "";
                
                public override void EnterCallSignature(TypeScriptParser.CallSignatureContext context)
                {
                    if (context.typeAnnotation() != null)
                    {
                        TypeAnnotationListener typeAnnotationListener = new TypeAnnotationListener();
                        context.typeAnnotation().EnterRule(typeAnnotationListener);

                        TypeString = typeAnnotationListener.TypeString;
                    }
                }

                class TypeAnnotationListener : TypeScriptParserBaseListener
                {
                    public string TypeString = "";
                    
                    public override void EnterTypeAnnotation(TypeScriptParser.TypeAnnotationContext context)
                    {
                        if (context.type_() != null)
                        {
                            TypeString = context.type_().GetFullText();
                        }
                    }
                }
            }
        }
    }
}