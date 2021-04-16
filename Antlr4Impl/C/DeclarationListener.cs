using System;
using System.Collections.Generic;
using System.Linq;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.C
{
    public class DeclarationListener : CBaseListener
    {
        public override void EnterDeclaration(CParser.DeclarationContext context)
        {
            Console.WriteLine(context.GetFullText());
        }
    }
    
    public class ExternalDeclarationListener : CBaseListener
    {
        readonly ClassInfo fileClassInfo;

        public ExternalDeclarationListener(ClassInfo fileClassInfo)
        {
            this.fileClassInfo = fileClassInfo;
        }

        public override void EnterExternalDeclaration(CParser.ExternalDeclarationContext context)
        {
            if (context.functionDefinition() != null)
            {
                FunctionDefinitionListener functionDefinitionListener =
                    new FunctionDefinitionListener(fileClassInfo);
                context.functionDefinition().EnterRule(functionDefinitionListener);
            }
        }
        
        class FunctionDefinitionListener : CBaseListener
        {
            readonly ClassInfo classInfo;

            public FunctionDefinitionListener(ClassInfo classInfo)
            {
                this.classInfo = classInfo;
            }

            public override void EnterFunctionDefinition(CParser.FunctionDefinitionContext context)
            {
                AccessFlags accessFlags = AccessFlags.AccPublic;

                TypeName returnType = TypeName.For("void");
                if (context.declarationSpecifiers() != null)
                {
                    DeclarationSpecifiersListener declarationSpecifiersListener = new DeclarationSpecifiersListener();
                    context.declarationSpecifiers().EnterRule(declarationSpecifiersListener);
                    
                    returnType = declarationSpecifiersListener.ReturnType;
                }
                
                DeclaratorListener declaratorListener = new DeclaratorListener();
                context.declarator().EnterRule(declaratorListener);
                string methodNameString = declaratorListener.DeclaratorName;

                // function body
                //CompoundStatementListener compoundStatementListener = new CompoundStatementListener();
                //context.compoundStatement().EnterRule(compoundStatementListener);

                List<Argument> arguments = declaratorListener.Arguments;

                MethodName methodName = new MethodName(
                    classInfo.className,
                    methodNameString,
                    returnType.Signature,
                    arguments);

                MethodInfo methodInfo = new MethodInfo(
                    methodName,
                    accessFlags,
                    classInfo.className,
                    arguments,
                    returnType,
                    new SourceCodeSnippet(context.GetFullText(), SourceCodeLanguage.C));

                classInfo.Children.Add(methodInfo);
            }
            
            class DeclarationSpecifiersListener : CBaseListener
            {
                public TypeName ReturnType;

                public override void EnterDeclarationSpecifiers(CParser.DeclarationSpecifiersContext context)
                {
                    foreach (CParser.DeclarationSpecifierContext declarationSpecifierContext in
                        context.declarationSpecifier())
                    {
                        ReturnType = TypeName.For(declarationSpecifierContext.GetFullText());
                    }
                }
            }
        }
    }

    public class DeclaratorListener : CBaseListener
    {
        public string DeclaratorName = "";
        public readonly List<Argument> Arguments = new List<Argument>();

        public override void EnterDeclarator(CParser.DeclaratorContext context)
        {
            if (context.directDeclarator() != null)
            {
                DirectDeclaratorListener directDeclaratorListener = new DirectDeclaratorListener();
                context.directDeclarator().EnterRule(directDeclaratorListener);
                Arguments.AddRange(directDeclaratorListener.Arguments);

                DeclaratorName = directDeclaratorListener.DirectDeclarator;
            }
        }
        
        class DirectDeclaratorListener : CBaseListener
        {
            public string DirectDeclarator = "";

            public readonly List<Argument> Arguments = new List<Argument>();

            public override void EnterDirectDeclarator(CParser.DirectDeclaratorContext context)
            {
                if (context.directDeclarator() != null)
                {
                    DirectDeclaratorListener directDeclaratorListener = new DirectDeclaratorListener();
                    context.directDeclarator().EnterRule(directDeclaratorListener);
                    DirectDeclarator = directDeclaratorListener.DirectDeclarator;
                }
                else if (!string.IsNullOrEmpty(context.GetText()))
                {
                    DirectDeclarator = context.GetFullText();
                }

                if (context.typeQualifierList() != null)
                {
                    TypeQualifierListener typeQualifierListener = new TypeQualifierListener();
                    context.typeQualifierList().EnterRule(typeQualifierListener);
                    
                    Console.WriteLine($"TypeQualifier: {typeQualifierListener.TypeQualifier}");
                }

                if (context.parameterTypeList() != null)
                {
                    ParameterTypeListListener parameterTypeListListener = new ParameterTypeListListener();
                    context.parameterTypeList().EnterRule(parameterTypeListListener);

                    Arguments.AddRange(parameterTypeListListener.Arguments);
                }
            }
        }
    }

    public class ParameterTypeListListener : CBaseListener
    {
        public readonly List<Argument> Arguments = new List<Argument>();
        
        public override void EnterParameterTypeList(CParser.ParameterTypeListContext context)
        {
            if (context.parameterList() != null)
            {
                ParameterListListener parameterListListener = new ParameterListListener();
                context.parameterList().EnterRule(parameterListListener);

                Arguments.AddRange(parameterListListener.Arguments);
            }
        }

        class ParameterListListener : CBaseListener
        {
            public readonly List<Argument> Arguments = new List<Argument>();
            
            public override void EnterParameterList(CParser.ParameterListContext context)
            {
                if (context.parameterDeclaration() != null)
                {
                    ParameterDeclarationListener parameterDeclarationListener = new ParameterDeclarationListener();
                    context.parameterDeclaration().EnterRule(parameterDeclarationListener);
                    
                    Arguments.Add(parameterDeclarationListener.Parameter);
                }

                if (context.parameterList() != null)
                {
                    ParameterListListener parameterListListener = new ParameterListListener();
                    context.parameterList().EnterRule(parameterListListener);
                    
                    Arguments.AddRange(parameterListListener.Arguments);
                }
            }
        }

        class ParameterDeclarationListener: CBaseListener
        {
            public Argument Parameter = new Argument("", TypeName.For("void"));
            
            public override void EnterParameterDeclaration(CParser.ParameterDeclarationContext context)
            {
                TypeName parameterType = TypeName.For("void");
                if (context.declarationSpecifiers() != null)
                {
                    DeclarationSpecifiersListener declarationSpecifiersListener = new DeclarationSpecifiersListener();
                    context.declarationSpecifiers().EnterRule(declarationSpecifiersListener);
                    if (declarationSpecifiersListener.Types.FirstOrDefault() != null)
                    {
                        parameterType = declarationSpecifiersListener.Types.FirstOrDefault();
                    }
                }

                string declaratorName = "";
                if (context.declarator() != null)
                {
                    DeclaratorListener declaratorListener = new DeclaratorListener();
                    context.declarator().EnterRule(declaratorListener);
                    declaratorName = declaratorListener.DeclaratorName;
                }

                Parameter = new Argument(declaratorName, parameterType);
            }
        }
    }

    public class DeclarationSpecifiersListener : CBaseListener
    {
        public List<string> TypeQualifiers = new List<string>();
        public readonly List<TypeName> Types = new List<TypeName>();
        
        public override void EnterDeclarationSpecifiers(CParser.DeclarationSpecifiersContext context)
        {
            foreach (CParser.DeclarationSpecifierContext declarationSpecifierContext in context.declarationSpecifier())
            {
                DeclarationSpecifierListener declarationSpecifierListener = new DeclarationSpecifierListener();
                declarationSpecifierContext.EnterRule(declarationSpecifierListener);

                if (!string.IsNullOrEmpty(declarationSpecifierListener.TypeQualifier))
                {
                    TypeQualifiers.Add(declarationSpecifierListener.TypeQualifier);
                }
                
                Types.Add(declarationSpecifierListener.Type);
            }
        }

        class DeclarationSpecifierListener : CBaseListener
        {
            public string TypeQualifier = "";
            public TypeName Type;
            
            public override void EnterDeclarationSpecifier(CParser.DeclarationSpecifierContext context)
            {
                if (context.typeQualifier() != null)
                {
                    TypeQualifierListener typeQualifierListener = new TypeQualifierListener();
                    context.typeQualifier().EnterRule(typeQualifierListener);
                    TypeQualifier = typeQualifierListener.TypeQualifier;
                }

                if (context.typeSpecifier() != null)
                {
                    TypeSpecifierListener typeSpecifierListener = new TypeSpecifierListener();
                    context.typeSpecifier().EnterRule(typeSpecifierListener);
                    Type = typeSpecifierListener.Type;
                }
            }
        }
    }
}