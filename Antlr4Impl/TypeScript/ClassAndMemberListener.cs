using System;
using System.Collections.Generic;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.TypeScript
{
    public class ClassDeclarationListener : TypeScriptParserBaseListener
    {
        readonly ClassInfo outerClassInfo;

        public ClassDeclarationListener(ClassInfo outerClassInfo)
        {
            this.outerClassInfo = outerClassInfo;
        }

        public override void EnterClassDeclaration(TypeScriptParser.ClassDeclarationContext context)
        {
            string classNameString =
                $"{outerClassInfo.className.ShortName}${context.Identifier().GetText()}";

            ClassName className = new ClassName(
                new FileName(outerClassInfo.className.ContainmentFile().FilePath),
                new PackageName(outerClassInfo.className.ContainmentPackage.PackageNameString),
                classNameString);
            ClassInfo classInfo = new ClassInfo(
                className,
                new List<MethodInfo>(),
                new List<FieldInfo>(),
                AccessFlags.AccPublic,
                new List<ClassInfo>(),
                new SourceCodeSnippet(context.GetFullText(), SourceCodeLanguage.TypeScript),
                false);

            outerClassInfo.Children.Add(classInfo);

            // get class members
            if (context.classTail() != null)
            {
                ClassTailListener classTailListener = new ClassTailListener(classInfo);
                context.classTail().EnterRule(classTailListener);
            }
        }

        class ClassTailListener : TypeScriptParserBaseListener
        {
            readonly ClassInfo parentClass;

            public ClassTailListener(ClassInfo parentClass)
            {
                this.parentClass = parentClass;
            }

            public override void EnterClassTail(TypeScriptParser.ClassTailContext context)
            {
                foreach (TypeScriptParser.ClassElementContext classElementContext in context.classElement())
                {
                    if (classElementContext.statement() != null)
                    {
                        StatementListener statementListener = new StatementListener(parentClass);
                        classElementContext.statement().EnterRule(statementListener);
                    }

                    if (classElementContext.constructorDeclaration() != null)
                    {
                        ConstructorDeclarationListener constructorDeclarationListener =
                            new ConstructorDeclarationListener(parentClass);
                        classElementContext.constructorDeclaration().EnterRule(constructorDeclarationListener);
                    }

                    if (classElementContext.GetChild(0) is TypeScriptParser.MethodDeclarationExpressionContext
                        methodDeclarationExpressionContext)
                    {
                        AccessFlags accessFlags = AccessFlags.None;
                        if (methodDeclarationExpressionContext.propertyMemberBase() != null)
                        {
                            PropertyMemberBaseListener propertyMemberBaseListener = new PropertyMemberBaseListener();
                            methodDeclarationExpressionContext.propertyMemberBase()
                                .EnterRule(propertyMemberBaseListener);
                            accessFlags = propertyMemberBaseListener.Flags;
                        }

                        string methodNameString = "";
                        if (methodDeclarationExpressionContext.propertyName() != null)
                        {
                            PropertyNameListener propertyNameListener = new PropertyNameListener();
                            methodDeclarationExpressionContext.propertyName().EnterRule(propertyNameListener);
                            methodNameString = propertyNameListener.Name;
                        }

                        // TODO
                        List<Argument> arguments = new List<Argument>();
                        TypeName returnType = TypeName.For("void");

                        MethodName expressionMethodName = new MethodName(
                            parentClass.className,
                            methodNameString,
                            returnType.Signature,
                            arguments);

                        MethodInfo expressionMethodInfo = new MethodInfo(
                            expressionMethodName,
                            accessFlags,
                            parentClass.className,
                            arguments,
                            returnType,
                            new SourceCodeSnippet(methodDeclarationExpressionContext.GetFullText(), SourceCodeLanguage.TypeScript));

                        parentClass.Children.Add(expressionMethodInfo);
                    }
                }
            }

            class PropertyNameListener : TypeScriptParserBaseListener
            {
                public string Name;

                public override void EnterPropertyName(TypeScriptParser.PropertyNameContext context)
                {
                    Name = context.identifierName().GetFullText();
                }
            }

            class PropertyMemberBaseListener : TypeScriptParserBaseListener
            {
                public AccessFlags Flags = AccessFlags.None;

                public override void EnterPropertyMemberBase(TypeScriptParser.PropertyMemberBaseContext context)
                {
                    if (context.accessibilityModifier() != null)
                    {
                        AccessibilityModifierListener accessibilityModifierListener =
                            new AccessibilityModifierListener();
                        context.accessibilityModifier().EnterRule(accessibilityModifierListener);
                        Flags = accessibilityModifierListener.Flags;
                    }
                }

                class AccessibilityModifierListener : TypeScriptParserBaseListener
                {
                    public AccessFlags Flags = AccessFlags.None;

                    public override void EnterAccessibilityModifier(
                        TypeScriptParser.AccessibilityModifierContext context)
                    {
                        if (context.Private() != null)
                        {
                            Flags = AccessFlags.AccPrivate;
                        }

                        if (context.Public() != null)
                        {
                            Flags = AccessFlags.AccPublic;
                        }

                        if (context.Protected() != null)
                        {
                            Flags = AccessFlags.AccProtected;
                        }
                    }
                }
            }

            class ConstructorDeclarationListener : TypeScriptParserBaseListener
            {
                readonly ClassInfo parentClass;

                public ConstructorDeclarationListener(ClassInfo parentClass)
                {
                    this.parentClass = parentClass;
                }

                public override void EnterConstructorDeclaration(TypeScriptParser.ConstructorDeclarationContext context)
                {
                    if (context.Constructor() != null)
                    {
                        MethodName expressionMethodName = new MethodName(
                            parentClass.className,
                            "constructor",
                            TypeName.For("void").Signature,
                            new List<Argument>());

                        MethodInfo expressionMethodInfo = new MethodInfo(
                            expressionMethodName,
                            AccessFlags.AccPublic,
                            parentClass.className,
                            new List<Argument>(),
                            TypeName.For("void"),
                            new SourceCodeSnippet(context.GetFullText(), SourceCodeLanguage.TypeScript));

                        parentClass.Children.Add(expressionMethodInfo);
                    }
                }
            }
        }
    }
}