using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Tree;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.CPP
{
    public class DeclarationListener : CPP14ParserBaseListener
    {
        readonly ClassInfo outerClass;

        public DeclarationListener(ClassInfo outerClass)
        {
            this.outerClass = outerClass;
        }

        public override void EnterDeclaration(CPP14Parser.DeclarationContext context)
        {
            if (context.functionDefinition() != null)
            {
                FunctionDefinitionListener functionDefinitionListener = new FunctionDefinitionListener(outerClass);
                context.functionDefinition().EnterRule(functionDefinitionListener);
            }

            if (context.blockDeclaration() != null)
            {
                BlockDeclarationListener blockDeclarationListener = new BlockDeclarationListener(outerClass);
                context.blockDeclaration().EnterRule(blockDeclarationListener);
            }
        }

        class BlockDeclarationListener : CPP14ParserBaseListener
        {
            readonly ClassInfo outerClass;
            public BlockDeclarationListener(ClassInfo outerClass)
            {
                this.outerClass = outerClass;
            }
            
            public override void EnterBlockDeclaration(CPP14Parser.BlockDeclarationContext context)
            {
                if (context.simpleDeclaration() != null)
                {
                    SimpleDeclarationListener simpleDeclarationListener = new SimpleDeclarationListener(outerClass);
                    context.simpleDeclaration().EnterRule(simpleDeclarationListener);
                }
            }

            class SimpleDeclarationListener : CPP14ParserBaseListener
            {
                readonly ClassInfo outerClass;
                public SimpleDeclarationListener(ClassInfo outerClass)
                {
                    this.outerClass = outerClass;
                }
                
                public override void EnterSimpleDeclaration(CPP14Parser.SimpleDeclarationContext context)
                {
                    DeclSpecifierSeqListener declSpecifierSeqListener = new DeclSpecifierSeqListener(outerClass);
                    context.declSpecifierSeq().EnterRule(declSpecifierSeqListener);
                }
            }
        }
    }

    public class FunctionDefinitionListener : CPP14ParserBaseListener
    {
        readonly ClassInfo outerClass;

        public FunctionDefinitionListener(ClassInfo outerClass)
        {
            this.outerClass = outerClass;
        }

        public override void EnterFunctionDefinition(CPP14Parser.FunctionDefinitionContext context)
        {
            AccessFlags accessFlags = AccessFlags.AccPublic;

            TypeName returnType = TypeName.For("void");
            if (context.declSpecifierSeq() != null)
            {
                DeclSpecifierSeqListener declSpecifierSeqListener = new DeclSpecifierSeqListener(outerClass);
                context.declSpecifierSeq().EnterRule(declSpecifierSeqListener);

                if (declSpecifierSeqListener.Types.FirstOrDefault() != null)
                {
                    returnType = declSpecifierSeqListener.Types.First();
                }
            }

            DeclaratorListener declaratorListener = new DeclaratorListener();
            context.declarator().EnterRule(declaratorListener);
            string methodNameString = declaratorListener.DeclaratorName;

            List<Argument> arguments = declaratorListener.Parameters;

            MethodName methodName = new MethodName(
                outerClass.className,
                methodNameString,
                returnType.Signature,
                arguments);

            MethodInfo methodInfo = new MethodInfo(
                methodName,
                accessFlags,
                outerClass.className,
                arguments,
                returnType,
                new SourceCodeSnippet(context.GetFullText(), SourceCodeLanguage.C));

            outerClass.Children.Add(methodInfo);
        }
    }

    public class DeclaratorListener : CPP14ParserBaseListener
    {
        public string DeclaratorName = "";
        public readonly List<Argument> Parameters = new List<Argument>();

        public override void EnterDeclarator(CPP14Parser.DeclaratorContext context)
        {
            if (context.parametersAndQualifiers() != null)
            {
                //context.parametersAndQualifiers().EnterRule();
            }

            if (context.pointerDeclarator() != null)
            {
                PointerDeclaratorListener pointerDeclaratorListener = new PointerDeclaratorListener();
                context.pointerDeclarator().EnterRule(pointerDeclaratorListener);

                DeclaratorName = pointerDeclaratorListener.Identifier;
            }
        }

        class PointerDeclaratorListener : CPP14ParserBaseListener
        {
            public string Identifier = "";

            public override void EnterPointerDeclarator(CPP14Parser.PointerDeclaratorContext context)
            {
                if (context.noPointerDeclarator() != null)
                {
                    NoPointerDeclaratorListener noPointerDeclaratorListener = new NoPointerDeclaratorListener();
                    context.noPointerDeclarator().EnterRule(noPointerDeclaratorListener);
                    Identifier = noPointerDeclaratorListener.Identifier;
                }

                foreach (CPP14Parser.PointerOperatorContext pointerOperatorContext in context.pointerOperator())
                {
                    PointerOperatorListener pointerOperatorListener = new PointerOperatorListener();
                    pointerOperatorContext.EnterRule(pointerOperatorListener);
                }

                foreach (ITerminalNode terminalNode in context.Const())
                {
                    string s = terminalNode.GetText();
                    var x = 1;
                }
            }

            class PointerOperatorListener : CPP14ParserBaseListener
            {
                public override void EnterPointerOperator(CPP14Parser.PointerOperatorContext context)
                {
                    if (context.cvqualifierseq() != null)
                    {
                        CvqualifierseqListener cvqualifierseqListener = new CvqualifierseqListener();
                        context.cvqualifierseq().EnterRule(cvqualifierseqListener);
                    }

                    if (context.nestedNameSpecifier() != null)
                    {
                        string s = context.nestedNameSpecifier().GetFullText();
                        var x = 1;
                    }

                    if (context.attributeSpecifierSeq() != null)
                    {
                        string s = context.nestedNameSpecifier().GetFullText();
                        var x = 1;
                    }
                }

                class CvqualifierseqListener : CPP14ParserBaseListener
                {
                    public override void EnterCvqualifierseq(CPP14Parser.CvqualifierseqContext context)
                    {
                        foreach (CPP14Parser.CvQualifierContext cvQualifierContext in context.cvQualifier())
                        {
                            CvqualifierseqListener cvqualifierseqListener = new CvqualifierseqListener();
                            cvQualifierContext.EnterRule(cvqualifierseqListener);
                        }
                    }

                    class CvQualifierListener : CPP14ParserBaseListener
                    {
                        public override void EnterCvQualifier(CPP14Parser.CvQualifierContext context)
                        {
                            string s = context.GetFullText();
                            var x = 1;
                        }
                    }
                }
            }

            class NoPointerDeclaratorListener : CPP14ParserBaseListener
            {
                public string Identifier = "";

                public override void EnterNoPointerDeclarator(CPP14Parser.NoPointerDeclaratorContext context)
                {
                    if (context.declaratorid() != null)
                    {
                        DeclaratoridListener declaratoridListener = new DeclaratoridListener();
                        context.declaratorid().EnterRule(declaratoridListener);
                        Identifier = declaratoridListener.Identifier;
                    }

                    if (context.noPointerDeclarator() != null)
                    {
                        NoPointerDeclaratorListener noPointerDeclaratorListener = new NoPointerDeclaratorListener();
                        context.noPointerDeclarator().EnterRule(noPointerDeclaratorListener);
                        Identifier = noPointerDeclaratorListener.Identifier;
                    }

                    if (context.parametersAndQualifiers() != null)
                    {
                        ParametersAndQualifiersListener parametersAndQualifiersListener =
                            new ParametersAndQualifiersListener();
                        context.parametersAndQualifiers().EnterRule(parametersAndQualifiersListener);
                    }
                }

                class DeclaratoridListener : CPP14ParserBaseListener
                {
                    public string Identifier = "";

                    public override void EnterDeclaratorid(CPP14Parser.DeclaratoridContext context)
                    {
                        IdExpressionListener idExpressionListener = new IdExpressionListener();
                        context.idExpression().EnterRule(idExpressionListener);
                        Identifier = idExpressionListener.Identifier;
                    }

                    class IdExpressionListener : CPP14ParserBaseListener
                    {
                        public string Identifier = "";

                        public override void EnterIdExpression(CPP14Parser.IdExpressionContext context)
                        {
                            if (context.qualifiedId() != null)
                            {
                                QualifiedIdListener qualifiedIdListener = new QualifiedIdListener();
                                context.qualifiedId().EnterRule(qualifiedIdListener);
                                Identifier = qualifiedIdListener.Identifier;
                            }

                            if (context.unqualifiedId() != null)
                            {
                                UnqualifiedIdListener unqualifiedIdListener = new UnqualifiedIdListener();
                                context.unqualifiedId().EnterRule(unqualifiedIdListener);
                                Identifier = unqualifiedIdListener.Identifier;
                            }
                        }
                    }

                    class QualifiedIdListener : CPP14ParserBaseListener
                    {
                        public string Identifier = "";

                        public override void EnterQualifiedId(CPP14Parser.QualifiedIdContext context)
                        {
                            if (context.unqualifiedId() != null)
                            {
                                UnqualifiedIdListener unqualifiedIdListener = new UnqualifiedIdListener();
                                context.unqualifiedId().EnterRule(unqualifiedIdListener);
                                Identifier = unqualifiedIdListener.Identifier;
                            }
                        }
                    }

                    class UnqualifiedIdListener : CPP14ParserBaseListener
                    {
                        public string Identifier = "";

                        public override void EnterUnqualifiedId(CPP14Parser.UnqualifiedIdContext context)
                        {
                            if (context.Identifier() != null)
                            {
                                Identifier = context.Identifier().GetText();
                            }
                        }
                    }
                }
            }

            class ParametersAndQualifiersListener : CPP14ParserBaseListener
            {
                public override void EnterParametersAndQualifiers(CPP14Parser.ParametersAndQualifiersContext context)
                {
                    var x = 1;
                }
            }
        }
    }

    public class DeclarationseqListener : CPP14ParserBaseListener
    {
        readonly ClassInfo outerClass;

        public DeclarationseqListener(ClassInfo outerClass)
        {
            this.outerClass = outerClass;
        }

        public override void EnterDeclarationseq(CPP14Parser.DeclarationseqContext context)
        {
            foreach (CPP14Parser.DeclarationContext declarationContext in context.declaration())
            {
                DeclarationListener declarationListener = new DeclarationListener(outerClass);
                declarationContext.EnterRule(declarationListener);
            }
        }
    }

    public class DeclSpecifierSeqListener : CPP14ParserBaseListener
    {
        public readonly List<TypeName> Types = new List<TypeName>();
        readonly ClassInfo outerClass;

        public DeclSpecifierSeqListener(ClassInfo outerClass)
        {
            this.outerClass = outerClass;
        }

        public override void EnterDeclSpecifierSeq(CPP14Parser.DeclSpecifierSeqContext context)
        {
            foreach (CPP14Parser.DeclSpecifierContext declSpecifierContext in context.declSpecifier())
            {
                DeclSpecifierListener declSpecifierListener = new DeclSpecifierListener(outerClass);
                declSpecifierContext.EnterRule(declSpecifierListener);
                Types.Add(declSpecifierListener.Type);
            }
        }

        class DeclSpecifierListener : CPP14ParserBaseListener
        {
            public TypeName Type = TypeName.For("void");

            readonly ClassInfo outerClass;

            public DeclSpecifierListener(ClassInfo outerClass)
            {
                this.outerClass = outerClass;
            }

            public override void EnterDeclSpecifier(CPP14Parser.DeclSpecifierContext context)
            {
                if (context.Typedef() != null)
                {
                    Type = TypeName.For(context.Typedef().GetText());
                }

                if (context.typeSpecifier() != null)
                {
                    TypeSpecifierListener typeSpecifierListener = new TypeSpecifierListener(outerClass);
                    context.typeSpecifier().EnterRule(typeSpecifierListener);
                }
            }
        }
    }
}