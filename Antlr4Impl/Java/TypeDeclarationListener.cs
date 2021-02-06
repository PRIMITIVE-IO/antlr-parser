using System;
using System.Collections.Generic;
using System.Linq;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.Java
{
    #region TOP LEVEL - Type, Modifier, Annotation Listeners

    /// <summary>
    /// Listener for Class, Enum, and Interface declarations
    /// </summary>
    public class TypeDeclarationListener : JavaParserBaseListener
    {
        readonly string parentFilePath;
        readonly string packageFqn;
        public ClassInfo OuterClassInfo;
        public ClassInfo InterfaceInfo;
        public ClassInfo EnumInfo;

        public TypeDeclarationListener(string parentFilePath, string packageFqn)
        {
            this.parentFilePath = parentFilePath;
            this.packageFqn = packageFqn;
        }

        public override void EnterTypeDeclaration(JavaParser.TypeDeclarationContext context)
        {
            AccessFlags modifier = GetAccessFlags(context);

            if (context.annotationTypeDeclaration() != null)
            {
                AnnotationTypeDeclarationListener annotationTypeDeclarationListener =
                    new AnnotationTypeDeclarationListener();
                context.annotationTypeDeclaration().EnterRule(annotationTypeDeclarationListener);
            }

            // this type could be a class, enum, or interface
            if (context.classDeclaration() != null)
            {
                ClassDeclarationListener classDeclarationListener =
                    new ClassDeclarationListener(parentFilePath, packageFqn, modifier);
                context.classDeclaration().EnterRule(classDeclarationListener);
                OuterClassInfo = classDeclarationListener.ClassInfo;
            }

            if (context.interfaceDeclaration() != null)
            {
                InterfaceDeclarationListener interfaceDeclarationListener =
                    new InterfaceDeclarationListener(parentFilePath, packageFqn, modifier);
                context.interfaceDeclaration().EnterRule(interfaceDeclarationListener);
                InterfaceInfo = interfaceDeclarationListener.InterfaceInfo;
            }

            if (context.enumDeclaration() != null)
            {
                EnumDeclarationListener enumDeclarationListener =
                    new EnumDeclarationListener(parentFilePath, packageFqn, modifier);
                context.enumDeclaration().EnterRule(enumDeclarationListener);
                EnumInfo = enumDeclarationListener.EnumInfo;
            }
        }

        public static AccessFlags GetAccessFlags(JavaParser.TypeDeclarationContext context)
        {
            ModifierListener modifierListener = new ModifierListener();
            AccessFlags modifier = AccessFlags.None;
            foreach (JavaParser.ClassOrInterfaceModifierContext classOrInterfaceModifierContext in
                context.classOrInterfaceModifier())
            {
                classOrInterfaceModifierContext.EnterRule(modifierListener);
                modifier |= modifierListener.flag;
            }

            return modifier;
        }
    }

    public class ModifierListener : JavaParserBaseListener
    {
        public AccessFlags flag;

        public override void EnterClassOrInterfaceModifier(JavaParser.ClassOrInterfaceModifierContext context)
        {
            if (context.FINAL() != null)
            {
                flag = AccessFlags.AccFinal;
            }

            if (context.PUBLIC() != null)
            {
                flag = AccessFlags.AccPublic;
            }

            if (context.STATIC() != null)
            {
                flag = AccessFlags.AccStatic;
            }

            if (context.PRIVATE() != null)
            {
                flag = AccessFlags.AccPrivate;
            }

            if (context.ABSTRACT() != null)
            {
                flag = AccessFlags.AccAbstract;
            }

            if (context.STRICTFP() != null)
            {
                flag = AccessFlags.AccStrict;
            }

            if (context.PROTECTED() != null)
            {
                flag = AccessFlags.AccProtected;
            }
        }
    }

    public class AnnotationTypeDeclarationListener : JavaParserBaseListener
    {
        public string Body;
        public string ID;

        public override void EnterAnnotationTypeDeclaration(JavaParser.AnnotationTypeDeclarationContext context)
        {
            ID = context.IDENTIFIER().GetText();

            AnnotationTypeBodyListener annotationTypeBodyListener =
                new AnnotationTypeBodyListener();
            context.annotationTypeBody().EnterRule(annotationTypeBodyListener);
            Body = annotationTypeBodyListener.Body;
        }
    }

    public class AnnotationTypeBodyListener : JavaParserBaseListener
    {
        public string Body;

        public override void EnterAnnotationTypeBody(JavaParser.AnnotationTypeBodyContext context)
        {
            Body = context.GetText();
        }
    }

    #endregion

    #region CLASS Listeners

    public class ClassDeclarationListener : JavaParserBaseListener
    {
        readonly string parentFileName;
        readonly string packageFqn;
        readonly AccessFlags modifier;
        public ClassInfo ClassInfo;
        readonly ClassName outerClass;

        public ClassDeclarationListener(
            string parentFileName,
            string packageFqn,
            AccessFlags modifier,
            ClassName outerClass = null)
        {
            this.parentFileName = parentFileName;
            this.packageFqn = packageFqn;
            this.modifier = modifier;
            this.outerClass = outerClass;
        }

        public override void EnterClassDeclaration(JavaParser.ClassDeclarationContext context)
        {
            string name = context.IDENTIFIER().GetText();
            if (outerClass != null)
            {
                name = $"{outerClass.ShortName}${name}";
            }

            ClassName className = new ClassName(
                new FileName(parentFileName),
                new PackageName(packageFqn),
                name);

            string headerText = context.GetFullText();
            if (headerText.Contains("{"))
            {
                headerText = headerText.Substring(
                    0,
                    headerText.IndexOf("{", StringComparison.Ordinal));
            }

            ClassBodyListener classBodyListener = new ClassBodyListener(className, parentFileName, packageFqn);
            context.classBody().EnterRule(classBodyListener);

            ClassInfo = new ClassInfo(
                className,
                classBodyListener.MethodInfos,
                classBodyListener.FieldInfos,
                modifier,
                classBodyListener.InnerClasses,
                new SourceCodeSnippet(headerText, SourceCodeLanguage.Java),
                false);
        }
    }

    public class ClassBodyListener : JavaParserBaseListener
    {
        readonly ClassName parentClass;
        readonly string package;
        readonly string packageFqn;

        public ClassBodyListener(ClassName parentClass, string package, string packageFqn)
        {
            this.parentClass = parentClass;
            this.package = package;
            this.packageFqn = packageFqn;
        }

        public List<MethodInfo> MethodInfos;
        public List<FieldInfo> FieldInfos;
        public List<ClassInfo> InnerClasses;

        public override void EnterClassBody(JavaParser.ClassBodyContext context)
        {
            ClassBodyDeclarationListener classBodyDeclarationListener =
                new ClassBodyDeclarationListener(parentClass, package, packageFqn);
            foreach (JavaParser.ClassBodyDeclarationContext classBodyDeclarationContext in
                context.classBodyDeclaration())
            {
                classBodyDeclarationContext.EnterRule(classBodyDeclarationListener);
            }

            MethodInfos = classBodyDeclarationListener.MethodInfos;
            FieldInfos = classBodyDeclarationListener.FieldInfos;
            InnerClasses = classBodyDeclarationListener.InnerClasses;
        }
    }

    public class ClassBodyDeclarationListener : JavaParserBaseListener
    {
        readonly ClassName parentClassName;
        readonly string package;
        readonly string packageFqn;

        public readonly List<MethodInfo> MethodInfos = new List<MethodInfo>();
        public readonly List<FieldInfo> FieldInfos = new List<FieldInfo>();
        public readonly List<ClassInfo> InnerClasses = new List<ClassInfo>();

        public ClassBodyDeclarationListener(
            ClassName parentClassName,
            string package,
            string packageFqn)
        {
            this.parentClassName = parentClassName;
            this.package = package;
            this.packageFqn = packageFqn;
        }

        public override void EnterClassBodyDeclaration(JavaParser.ClassBodyDeclarationContext context)
        {
            if (context.memberDeclaration() == null) return;
            MemberDeclarationListener memberDeclarationListener =
                new MemberDeclarationListener(parentClassName, package, packageFqn);

            context.memberDeclaration().EnterRule(memberDeclarationListener);
            if (memberDeclarationListener.MethodInfo != null)
            {
                MethodInfos.Add(memberDeclarationListener.MethodInfo);
            }

            if (memberDeclarationListener.FieldInfo != null)
            {
                FieldInfos.Add(memberDeclarationListener.FieldInfo);
            }

            if (memberDeclarationListener.InnerClassInfo != null)
            {
                InnerClasses.Add(memberDeclarationListener.InnerClassInfo);
            }
        }
    }

    #endregion

    #region INTERFACE and ENUM Listeners

    public class InterfaceDeclarationListener : JavaParserBaseListener
    {
        readonly string parentFilePath;
        readonly string packageFqn;
        readonly AccessFlags modifier;
        public ClassInfo InterfaceInfo;

        public InterfaceDeclarationListener(string parentFilePath, string packageFqn, AccessFlags modifier)
        {
            this.parentFilePath = parentFilePath;
            this.packageFqn = packageFqn;
            this.modifier = modifier;
        }

        public override void EnterInterfaceDeclaration(JavaParser.InterfaceDeclarationContext context)
        {
            string name = context.IDENTIFIER().GetText();

            
            ClassName className = new ClassName(
                new FileName(parentFilePath),
                new PackageName(packageFqn),
                name);
            
            string headerText = context.GetFullText();
            if (headerText.Contains("{"))
            {
                headerText = headerText.Substring(
                    0,
                    headerText.IndexOf("{", StringComparison.Ordinal));
            }

            InterfaceBodyListener classBodyListener =
                new InterfaceBodyListener(className);
            context.interfaceBody().EnterRule(classBodyListener);

            InterfaceInfo = new ClassInfo(
                className,
                classBodyListener.MethodInfos,
                new List<FieldInfo>(),
                modifier,
                new List<ClassInfo>(),
                new SourceCodeSnippet(headerText, SourceCodeLanguage.Java),
                false);
        }
    }

    public class InterfaceBodyListener : JavaParserBaseListener
    {
        readonly ClassName parentClass;

        public InterfaceBodyListener(ClassName parentClass)
        {
            this.parentClass = parentClass;
        }

        public List<MethodInfo> MethodInfos;

        public override void EnterInterfaceBody(JavaParser.InterfaceBodyContext context)
        {
            InterfaceBodyDeclarationListener classBodyDeclarationListener =
                new InterfaceBodyDeclarationListener(parentClass);
            foreach (JavaParser.InterfaceBodyDeclarationContext classBodyDeclarationContext in
                context.interfaceBodyDeclaration())
            {
                classBodyDeclarationContext.EnterRule(classBodyDeclarationListener);
            }

            MethodInfos = classBodyDeclarationListener.MethodInfos;
        }
    }

    public class InterfaceBodyDeclarationListener : JavaParserBaseListener
    {
        readonly ClassName parentClassName;

        public readonly List<MethodInfo> MethodInfos = new List<MethodInfo>();

        public InterfaceBodyDeclarationListener(ClassName parentClassName)
        {
            this.parentClassName = parentClassName;
        }

        public override void EnterInterfaceBodyDeclaration(
            JavaParser.InterfaceBodyDeclarationContext context)
        {
            if (context.interfaceMemberDeclaration() == null) return;
            InterfaceMemberDeclarationListener memberDeclarationListener =
                new InterfaceMemberDeclarationListener(parentClassName);

            context.interfaceMemberDeclaration().EnterRule(memberDeclarationListener);
            if (memberDeclarationListener.MethodInfo != null)
            {
                MethodInfos.Add(memberDeclarationListener.MethodInfo);
            }
        }
    }

    public class EnumDeclarationListener : JavaParserBaseListener
    {
        readonly string parentFilePath;
        readonly string packageFqn;
        readonly AccessFlags modifier;
        public ClassInfo EnumInfo;

        public EnumDeclarationListener(string parentFilePath, string packageFqn, AccessFlags modifier)
        {
            this.parentFilePath = parentFilePath;
            this.packageFqn = packageFqn;
            this.modifier = modifier;
        }

        public override void EnterEnumDeclaration(JavaParser.EnumDeclarationContext context)
        {
            string name = context.IDENTIFIER().GetText();

            ClassName enumName = new ClassName(
                new FileName(parentFilePath),
                new PackageName(packageFqn),
                name);

            EnumBodyDeclarationsListener enumBodyDeclarationsListener =
                new EnumBodyDeclarationsListener(enumName, parentFilePath, packageFqn);
            context.enumBodyDeclarations().EnterRule(enumBodyDeclarationsListener);

            EnumInfo = new ClassInfo(
                enumName,
                new List<MethodInfo>(),
                enumBodyDeclarationsListener.FieldInfos,
                modifier,
                new List<ClassInfo>(),
                new SourceCodeSnippet("", SourceCodeLanguage.Java), 
                false);
        }
    }

    public class EnumBodyDeclarationsListener : JavaParserBaseListener
    {
        readonly ClassName parentClassName;
        readonly string parentFilePath;
        readonly string packageFqn;

        public readonly List<FieldInfo> FieldInfos = new List<FieldInfo>();

        public EnumBodyDeclarationsListener(ClassName parentClassName, string parentFilePath, string packageFqn)
        {
            this.parentClassName = parentClassName;
            this.parentFilePath = parentFilePath;
            this.packageFqn = packageFqn;
        }

        public override void EnterEnumBodyDeclarations(JavaParser.EnumBodyDeclarationsContext context)
        {
            ClassBodyDeclarationListener classBodyDeclarationListener =
                new ClassBodyDeclarationListener(parentClassName, parentFilePath, packageFqn);
            foreach (JavaParser.ClassBodyDeclarationContext classBodyDeclarationContext
                in context.classBodyDeclaration())
            {
                classBodyDeclarationContext.EnterRule(classBodyDeclarationListener);
                FieldInfos.AddRange(classBodyDeclarationListener.FieldInfos);
            }
        }
    }

    #endregion

    #region TYPE Listeners

    public class TypeTypeListener : JavaParserBaseListener
    {
        public string QualifiedName;
        public string ID;
        public PrimitiveTypeName PrimitiveTypeName;

        public override void EnterTypeType(JavaParser.TypeTypeContext context)
        {
            if (context.annotation() != null)
            {
                AnnotationListener annotationListener = new AnnotationListener();
                context.annotation().EnterRule(annotationListener);
                QualifiedName = annotationListener.QualifiedName;
            }

            if (context.classOrInterfaceType() != null)
            {
                ClassOrInterfaceTypeListener classOrInterfaceTypeListener =
                    new ClassOrInterfaceTypeListener();
                context.classOrInterfaceType().EnterRule(classOrInterfaceTypeListener);
                ID = classOrInterfaceTypeListener.id;
            }

            if (context.primitiveType() != null)
            {
                PrimitiveTypeListener primitiveTypeListener = new PrimitiveTypeListener();
                context.primitiveType().EnterRule(primitiveTypeListener);
                PrimitiveTypeName = primitiveTypeListener.PrimitiveTypeName;
            }
        }
    }

    public class AnnotationListener : JavaParserBaseListener
    {
        public string QualifiedName;

        public override void EnterAnnotation(JavaParser.AnnotationContext context)
        {
            QualifiedName = context.qualifiedName().GetText();
        }
    }

    public class ClassOrInterfaceTypeListener : JavaParserBaseListener
    {
        public string id;

        public override void EnterClassOrInterfaceType(JavaParser.ClassOrInterfaceTypeContext context)
        {
            TypeArgumentsListener typeArgumentsListener = new TypeArgumentsListener();
            List<string> typeArguments = new List<string>();
            foreach (JavaParser.TypeArgumentsContext typeArgumentsContext in context.typeArguments())
            {
                typeArgumentsContext.EnterRule(typeArgumentsListener);
                typeArguments.AddRange(typeArgumentsListener.typeArguments);
            }

            if (typeArguments.Any())
            {
                id = "";
            }

            foreach (string typeArgument in typeArguments)
            {
                id += typeArgument;
            }
        }
    }

    public class PrimitiveTypeListener : JavaParserBaseListener
    {
        public PrimitiveTypeName PrimitiveTypeName;

        public override void EnterPrimitiveType(JavaParser.PrimitiveTypeContext context)
        {
            if (context.INT() != null)
            {
                PrimitiveTypeName = TypeName.For("int") as PrimitiveTypeName;
            }

            if (context.BYTE() != null)
            {
                PrimitiveTypeName = TypeName.For("byte") as PrimitiveTypeName;
            }

            if (context.CHAR() != null)
            {
                PrimitiveTypeName = TypeName.For("char") as PrimitiveTypeName;
            }

            if (context.LONG() != null)
            {
                PrimitiveTypeName = TypeName.For("long") as PrimitiveTypeName;
            }

            if (context.FLOAT() != null)
            {
                PrimitiveTypeName = TypeName.For("float") as PrimitiveTypeName;
            }

            if (context.SHORT() != null)
            {
                PrimitiveTypeName = TypeName.For("short") as PrimitiveTypeName;
            }

            if (context.DOUBLE() != null)
            {
                PrimitiveTypeName = TypeName.For("double") as PrimitiveTypeName;
            }

            if (context.BOOLEAN() != null)
            {
                PrimitiveTypeName = TypeName.For("bool") as PrimitiveTypeName;
            }
        }
    }

    public class TypeListListener : JavaParserBaseListener
    {
        public override void EnterTypeList(JavaParser.TypeListContext context)
        {
        }
    }

    public class TypeArgumentsListener : JavaParserBaseListener
    {
        public readonly List<string> typeArguments = new List<string>();

        public override void EnterTypeArguments(JavaParser.TypeArgumentsContext context)
        {
            TypeArgumentListener typeArgumentListener = new TypeArgumentListener();
            foreach (JavaParser.TypeArgumentContext typeArgumentContext in context.typeArgument())
            {
                typeArgumentContext.EnterRule(typeArgumentListener);
                typeArguments.Add(typeArgumentListener.Type);
            }
        }
    }

    public class TypeArgumentListener : JavaParserBaseListener
    {
        public string Type;

        public override void EnterTypeArgument(JavaParser.TypeArgumentContext context)
        {
            if (context.typeType() == null)
            {
                Type = TypeName.For("void").Signature;
                return;
            }

            TypeTypeListener typeTypeListener = new TypeTypeListener();
            context.typeType().EnterRule(typeTypeListener);

            if (typeTypeListener.ID != null)
            {
                Type = typeTypeListener.ID;
            }
            else if (typeTypeListener.QualifiedName != null)
            {
                Type = typeTypeListener.QualifiedName;
            }
            else if (typeTypeListener.PrimitiveTypeName != null)
            {
                Type = TypeName.For("void").Signature;
            }
        }
    }

    #endregion
}