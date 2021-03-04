using System.Collections.Generic;
using System.Linq;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.Java
{
    #region TOP LEVEL - Member Listener

    /// <summary>
    /// Listener for Method, Field, and Inner Class declarations 
    /// </summary>
    public class MemberDeclarationListener : JavaParserBaseListener
    {
        readonly ClassInfo parentClass;

        public MemberDeclarationListener(ClassInfo parentClass)
        {
            this.parentClass = parentClass;
        }

        public override void EnterMemberDeclaration(JavaParser.MemberDeclarationContext context)
        {
            // this member could be a method, class, or field
            if (context.methodDeclaration() != null)
            {
                MethodDeclarationListener methodDeclarationListener =
                    new MethodDeclarationListener(parentClass);
                context.methodDeclaration().EnterRule(methodDeclarationListener);
            }

            if (context.constructorDeclaration() != null)
            {
                ConstructorDeclarationListener constructorDeclarationListener =
                    new ConstructorDeclarationListener(parentClass);
                context.constructorDeclaration().EnterRule(constructorDeclarationListener);
            }

            if (context.fieldDeclaration() != null)
            {
                FieldDeclarationListener fieldDeclarationListener =
                    new FieldDeclarationListener(parentClass);
                context.fieldDeclaration().EnterRule(fieldDeclarationListener);
            }

            if (context.classDeclaration() != null)
            {
                ClassDeclarationListener classDeclarationListener =
                    new ClassDeclarationListener(parentClass);
                context.classDeclaration().EnterRule(classDeclarationListener);
            }
        }
    }

    public class InterfaceMemberDeclarationListener : JavaParserBaseListener
    {
        readonly ClassInfo parentClass;

        public InterfaceMemberDeclarationListener(ClassInfo parentClass)
        {
            this.parentClass = parentClass;
        }

        public override void EnterInterfaceMemberDeclaration(
            JavaParser.InterfaceMemberDeclarationContext context)
        {
            // this member could be a method, class, or field
            if (context.interfaceMethodDeclaration() != null)
            {
                InterfaceMethodDeclarationListener methodDeclarationListener =
                    new InterfaceMethodDeclarationListener(parentClass);
                context.interfaceMethodDeclaration().EnterRule(methodDeclarationListener);
            }
        }
    }

    #endregion

    #region METHOD Listeners

    public class MethodDeclarationListener : BaseMethodDeclarationListener
    {
        public MethodDeclarationListener(ClassInfo parentClass) : base(parentClass)
        {
        }

        public override void EnterMethodDeclaration(JavaParser.MethodDeclarationContext context)
        {
            ParseMethodFromContext(
                context.formalParameters(),
                context.typeTypeOrVoid(),
                context.qualifiedNameList(),
                context.IDENTIFIER().GetText(),
                context.GetFullText());
        }
    }

    public class ConstructorDeclarationListener : BaseMethodDeclarationListener
    {
        public ConstructorDeclarationListener(ClassInfo parentClass) : base(parentClass)
        {
        }

        public override void EnterConstructorDeclaration(JavaParser.ConstructorDeclarationContext context)
        {
            ParseMethodFromContext(
                context.formalParameters(),
                null,
                context.qualifiedNameList(),
                context.IDENTIFIER().GetText(),
                context.GetFullText());
        }
    }

    public class InterfaceMethodDeclarationListener : BaseMethodDeclarationListener
    {
        public InterfaceMethodDeclarationListener(ClassInfo parentClass) : base(parentClass)
        {
        }

        public override void EnterInterfaceMethodDeclaration(JavaParser.InterfaceMethodDeclarationContext context)
        {
            ParseMethodFromContext(
                context.formalParameters(),
                context.typeTypeOrVoid(),
                context.qualifiedNameList(),
                context.IDENTIFIER().GetText(),
                context.GetFullText());
        }
    }

    public abstract class BaseMethodDeclarationListener : JavaParserBaseListener
    {
        readonly ClassInfo parentClass;

        protected BaseMethodDeclarationListener(ClassInfo parentClass)
        {
            this.parentClass = parentClass;
        }

        protected void ParseMethodFromContext(
            JavaParser.FormalParametersContext formalParametersContext,
            JavaParser.TypeTypeOrVoidContext typeTypeOrVoidContext,
            JavaParser.QualifiedNameListContext qualifiedNameListContext,
            string methodNameText,
            string methodBody)
        {
            FormalParametersListener formalParametersListener =
                new FormalParametersListener();
            formalParametersContext.EnterRule(formalParametersListener);

            TypeName returnType = TypeName.For("void");
            if (typeTypeOrVoidContext != null)
            {
                TypeTypeOrVoidListener typeOrVoidListener = new TypeTypeOrVoidListener();
                typeTypeOrVoidContext.EnterRule(typeOrVoidListener);
                returnType = typeOrVoidListener.TypeName;
            }

            QualifiedNameListListener qualifiedNameListListener = new QualifiedNameListListener();
            if (qualifiedNameListContext != null)
            {
                // Exceptions
                qualifiedNameListContext.EnterRule(qualifiedNameListListener);
                List<string> qualifiedNames = qualifiedNameListListener.QualifiedNames;
            }

            MethodName methodName = new MethodName(
                parentClass.className,
                methodNameText,
                returnType.Signature,
                formalParametersListener
                    .Arguments
                    .Select(arg => new Argument(
                        arg.Type.Signature,
                        TypeName.For(arg.Type.Signature))).ToList());
            MethodInfo newMethodInfo = new MethodInfo(
                methodName,
                AccessFlags.AccPublic, // TODO
                parentClass.className,
                formalParametersListener.Arguments,
                returnType,
                new SourceCodeSnippet(methodBody, SourceCodeLanguage.Java));

            parentClass.Children.Add(newMethodInfo);
        }

        class QualifiedNameListListener : JavaParserBaseListener
        {
            public readonly List<string> QualifiedNames = new List<string>();

            public override void EnterQualifiedNameList(JavaParser.QualifiedNameListContext context)
            {
                QualifiedNameListener qualifiedNameListener = new QualifiedNameListener();
                foreach (JavaParser.QualifiedNameContext qualifiedNameContext in context.qualifiedName())
                {
                    qualifiedNameContext.EnterRule(qualifiedNameListener);
                    QualifiedNames.Add(qualifiedNameListener.QualifiedName);
                }
            }

            class QualifiedNameListener : JavaParserBaseListener
            {
                public string QualifiedName;

                public override void EnterQualifiedName(JavaParser.QualifiedNameContext context)
                {
                    QualifiedName = context.IDENTIFIER().ToString();
                }
            }
        }

        class TypeTypeOrVoidListener : JavaParserBaseListener
        {
            public TypeName TypeName;

            public override void EnterTypeTypeOrVoid(JavaParser.TypeTypeOrVoidContext context)
            {
                if (context.typeType() == null)
                {
                    TypeName = TypeName.For("void");
                    return;
                }

                TypeTypeListener typeTypeListener = new TypeTypeListener();
                context.typeType().EnterRule(typeTypeListener);

                if (typeTypeListener.PrimitiveTypeName != null)
                {
                    TypeName = typeTypeListener.PrimitiveTypeName;
                }
                else if (!string.IsNullOrEmpty(typeTypeListener.ID))
                {
                    TypeName = TypeName.For(typeTypeListener.ID);
                }
                else
                {
                    TypeName = TypeName.For("void");
                }
            }
        }

        class FormalParametersListener : JavaParserBaseListener
        {
            public List<Argument> Arguments = new List<Argument>();

            public override void EnterFormalParameters(JavaParser.FormalParametersContext context)
            {
                if (context.formalParameterList() != null)
                {
                    FormalParameterListListener formalParameterListListener = new FormalParameterListListener();
                    context.formalParameterList().EnterRule(formalParameterListListener);
                    Arguments = formalParameterListListener.Arguments;
                }
            }

            class FormalParameterListListener : JavaParserBaseListener
            {
                public readonly List<Argument> Arguments = new List<Argument>();

                public override void EnterFormalParameterList(JavaParser.FormalParameterListContext context)
                {
                    FormalParameterListener formalParameterListener = new FormalParameterListener();
                    foreach (JavaParser.FormalParameterContext formalParameterContext in context.formalParameter())
                    {
                        formalParameterContext.EnterRule(formalParameterListener);
                        Arguments.Add(formalParameterListener.Argument);
                    }
                }

                class FormalParameterListener : JavaParserBaseListener
                {
                    public Argument Argument;

                    public override void EnterFormalParameter(JavaParser.FormalParameterContext context)
                    {
                        // type of parameter
                        TypeTypeListener typeTypeListener = new TypeTypeListener();
                        context.typeType().EnterRule(typeTypeListener);

                        TypeName typeName = null;
                        if (typeTypeListener.PrimitiveTypeName != null)
                        {
                            typeName = typeTypeListener.PrimitiveTypeName;
                        }
                        else if (!string.IsNullOrEmpty(typeTypeListener.ID))
                        {
                            typeName = TypeName.For(typeTypeListener.ID);
                        }
                        else
                        {
                            typeName = TypeName.For("void");
                        }

                        // name of parameter
                        VariableDeclaratorIdListener variableDeclaratorIdListener = new VariableDeclaratorIdListener();
                        context.variableDeclaratorId().EnterRule(variableDeclaratorIdListener);

                        Argument = new Argument(variableDeclaratorIdListener.ID, typeName);
                    }
                }
            }
        }
    }

    #endregion

    #region FIELD Listeners

    public class FieldDeclarationListener : JavaParserBaseListener
    {
        readonly ClassInfo parentClass;

        public FieldDeclarationListener(ClassInfo parentClass)
        {
            this.parentClass = parentClass;
        }

        public override void EnterFieldDeclaration(JavaParser.FieldDeclarationContext context)
        {
            PrimitiveTypeName primitiveTypeName = null;
            string qualifiedName = "";
            if (context.typeType() != null)
            {
                TypeTypeListener typeTypeListener = new TypeTypeListener();
                context.typeType().EnterRule(typeTypeListener);
                primitiveTypeName = typeTypeListener.PrimitiveTypeName;
                qualifiedName = typeTypeListener.QualifiedName;
            }

            List<string> IDs = new List<string>();
            if (context.variableDeclarators() != null)
            {
                VariableDeclaratorsListener variableDeclaratorsListener = new VariableDeclaratorsListener();
                context.variableDeclarators().EnterRule(variableDeclaratorsListener);
                IDs = variableDeclaratorsListener.IDs;
            }

            if (primitiveTypeName == null && string.IsNullOrEmpty(qualifiedName))
            {
                primitiveTypeName = TypeName.For("void") as PrimitiveTypeName;
            }

            string fieldName = IDs.FirstOrDefault();
            FieldName fieldFqn = new FieldName(
                parentClass.className,
                fieldName,
                primitiveTypeName != null
                    ? primitiveTypeName.Signature
                    : TypeName.For(qualifiedName).Signature);
            FieldInfo fieldInfo = new FieldInfo(
                fieldFqn,
                parentClass.className,
                AccessFlags.AccPublic,
                new SourceCodeSnippet(context.GetFullText(), SourceCodeLanguage.Java));

            parentClass.Children.Add(fieldInfo);
        }

        class VariableDeclaratorsListener : JavaParserBaseListener
        {
            public readonly List<string> IDs = new List<string>();

            public override void EnterVariableDeclarators(JavaParser.VariableDeclaratorsContext context)
            {
                VariableDeclaratorListener variableDeclaratorListener = new VariableDeclaratorListener();
                foreach (JavaParser.VariableDeclaratorContext variableDeclaratorContext in context.variableDeclarator())
                {
                    variableDeclaratorContext.EnterRule(variableDeclaratorListener);
                    IDs.Add(variableDeclaratorListener.ID);
                    variableDeclaratorListener.ID = null;
                }
            }

            class VariableDeclaratorListener : JavaParserBaseListener
            {
                public string ID;

                public override void EnterVariableDeclarator(JavaParser.VariableDeclaratorContext context)
                {
                    VariableDeclaratorIdListener variableDeclaratorIdListener = new VariableDeclaratorIdListener();
                    context.variableDeclaratorId().EnterRule(variableDeclaratorIdListener);
                    ID = variableDeclaratorIdListener.ID;
                }
            }
        }
    }

    public class VariableDeclaratorIdListener : JavaParserBaseListener
    {
        public string ID;

        public override void EnterVariableDeclaratorId(JavaParser.VariableDeclaratorIdContext context)
        {
            ID = context.IDENTIFIER().GetText();
        }
    }

    #endregion
}