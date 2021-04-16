using System.Collections.Generic;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.CPP
{
    public class TypeSpecifierListener : CPP14ParserBaseListener
    {
        readonly ClassInfo outerClass;

        public TypeSpecifierListener(ClassInfo outerClass)
        {
            this.outerClass = outerClass;
        }

        public override void EnterTypeSpecifier(CPP14Parser.TypeSpecifierContext context)
        {
            if (context.classSpecifier() != null)
            {
                ClassSpecifierListener classSpecifierListener = new ClassSpecifierListener(outerClass);
                context.classSpecifier().EnterRule(classSpecifierListener);
            }
        }

        class ClassSpecifierListener : CPP14ParserBaseListener
        {
            readonly ClassInfo outerClass;

            public ClassSpecifierListener(ClassInfo outerClass)
            {
                this.outerClass = outerClass;
            }

            public ClassInfo OuterClass;

            public override void EnterClassSpecifier(CPP14Parser.ClassSpecifierContext context)
            {
                AccessFlags modifier = AccessFlags.None;
                string classNameString = "";
                string classHeaderSource = "";
                if (context.classHead() != null)
                {
                    ClassHeadListener classHeadListener = new ClassHeadListener();
                    context.classHead().EnterRule(classHeadListener);
                    classNameString = classHeadListener.Identifier;

                    classHeaderSource = classHeadListener.ClassHeader;
                }

                // top level class
                ClassName className = new ClassName(
                    outerClass.className.ContainmentFile(),
                    outerClass.className.ContainmentPackage,
                    classNameString);

                ClassInfo newClassInfo = new ClassInfo(
                    className,
                    new List<MethodInfo>(),
                    new List<FieldInfo>(),
                    modifier,
                    new List<ClassInfo>(),
                    new SourceCodeSnippet(classHeaderSource, SourceCodeLanguage.Cpp),
                    false);

                MemberSpecificationListener memberSpecificationListener =
                    new MemberSpecificationListener(outerClass);
                context.memberSpecification().EnterRule(memberSpecificationListener);

                outerClass.Children.Add(newClassInfo);
            }

            class ClassHeadListener : CPP14ParserBaseListener
            {
                public string Identifier = "";
                public string ClassHeader = "";
                
                public override void EnterClassHead(CPP14Parser.ClassHeadContext context)
                {
                    ClassHeadNameListener classHeadNameListener = new ClassHeadNameListener();
                    context.classHeadName().EnterRule(classHeadNameListener);
                    Identifier = classHeadNameListener.Identifier;

                    ClassHeader = context.GetFullText();
                }

                class ClassHeadNameListener : CPP14ParserBaseListener
                {
                    public string Identifier = "";
                    public override void EnterClassHeadName(CPP14Parser.ClassHeadNameContext context)
                    {
                        ClassNameListener classNameListener = new ClassNameListener();
                        context.className().EnterRule(classNameListener);
                        Identifier = classNameListener.Identifier;
                    }

                    class ClassNameListener : CPP14ParserBaseListener
                    {
                        public string Identifier = "";
                        public override void EnterClassName(CPP14Parser.ClassNameContext context)
                        {
                            Identifier = context.GetText();
                        }
                    }
                }
            }

            class MemberSpecificationListener : CPP14ParserBaseListener
            {
                readonly ClassInfo outerClass;

                public MemberSpecificationListener(ClassInfo outerClass)
                {
                    this.outerClass = outerClass;
                }

                public override void EnterMemberSpecification(CPP14Parser.MemberSpecificationContext context)
                {
                    foreach (CPP14Parser.MemberdeclarationContext memberdeclarationContext in context
                        .memberdeclaration())
                    {
                        MemberDeclarationListener memberDeclarationListener =
                            new MemberDeclarationListener(outerClass);
                        memberdeclarationContext.EnterRule(memberDeclarationListener);
                    }
                }

                class MemberDeclarationListener : CPP14ParserBaseListener
                {
                    readonly ClassInfo outerClass;

                    public MemberDeclarationListener(ClassInfo outerClass)
                    {
                        this.outerClass = outerClass;
                    }

                    public override void EnterMemberdeclaration(CPP14Parser.MemberdeclarationContext context)
                    {
                        if (context.functionDefinition() != null)
                        {
                            FunctionDefinitionListener functionDefinitionListener =
                                new FunctionDefinitionListener(outerClass);
                            context.functionDefinition().EnterRule(functionDefinitionListener);
                        }
                    }
                }
            }
        }
    }
}