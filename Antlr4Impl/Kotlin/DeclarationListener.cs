using System.Collections.Generic;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.Kotlin
{
    public class ClassDeclarationListener : KotlinParserBaseListener
    {
        readonly ClassInfo outerFileClass;
        public ClassDeclarationListener(ClassInfo outerFileClass)
        {
            this.outerFileClass = outerFileClass;
        }

        public override void EnterClassDeclaration(KotlinParser.ClassDeclarationContext context)
        {
            string classNameString = $"{outerFileClass.className.ShortName}${context.simpleIdentifier().GetText()}";
            
            ClassName className = new ClassName(
                outerFileClass.className.ContainmentFile(),
                outerFileClass.className.ContainmentPackage,
                classNameString);
            
            ClassInfo classInfo = new ClassInfo(
                className,
                new List<MethodInfo>(),
                new List<FieldInfo>(),
                AccessFlags.AccPublic,
                new List<ClassInfo>(),
                new SourceCodeSnippet("", SourceCodeLanguage.Kotlin),
                false);
            
            outerFileClass.Children.Add(classInfo);
            
            if (context.classBody() != null)
            {
                ClassBodyListener classBodyListener = new ClassBodyListener(classInfo);
                context.classBody().EnterRule(classBodyListener);
            }
        }
    }

    public class ClassBodyListener : KotlinParserBaseListener
    {
        readonly ClassInfo outerFileClass;
        public ClassBodyListener(ClassInfo outerFileClass)
        {
            this.outerFileClass = outerFileClass;
        }

        public override void EnterClassBody(KotlinParser.ClassBodyContext context)
        {
            if (context.classMemberDeclaration() != null)
            {
                foreach (KotlinParser.ClassMemberDeclarationContext classMemberDeclarationContext in context.classMemberDeclaration())
                {
                    ClassMemberDeclarationListener classMemberDeclarationListener = new ClassMemberDeclarationListener(outerFileClass);
                    classMemberDeclarationContext.EnterRule(classMemberDeclarationListener);
                }
            }
        }
    }

    public class ClassMemberDeclarationListener : KotlinParserBaseListener
    {
        readonly ClassInfo classInfo;
        public ClassMemberDeclarationListener(ClassInfo classInfo)
        {
            this.classInfo = classInfo;
        }

        public override void EnterClassMemberDeclaration(KotlinParser.ClassMemberDeclarationContext context)
        {
            if (context.classDeclaration() != null)
            {
                ClassDeclarationListener classDeclarationListener = new ClassDeclarationListener(classInfo);
                context.classDeclaration().EnterRule(classDeclarationListener);
            }

            if (context.functionDeclaration() != null)
            {
                FunctionDeclarationListener functionDeclarationListener = new FunctionDeclarationListener(classInfo);
                context.functionDeclaration().EnterRule(functionDeclarationListener);
            }

            if (context.objectDeclaration() != null)
            {
                ObjectDeclarationListener objectDeclarationListener = new ObjectDeclarationListener(classInfo);
                context.objectDeclaration().EnterRule(objectDeclarationListener);
            }

            if (context.propertyDeclaration() != null)
            {
                PropertyDeclarationListener propertyDeclarationListener = new PropertyDeclarationListener(classInfo);
                context.propertyDeclaration().EnterRule(propertyDeclarationListener);
            }
        }
    }

    public class FunctionDeclarationListener : KotlinParserBaseListener
    {
        readonly ClassInfo classInfo;
        public FunctionDeclarationListener(ClassInfo classInfo)
        {
            this.classInfo = classInfo;
        }

        public override void EnterFunctionDeclaration(KotlinParser.FunctionDeclarationContext context)
        {
            foreach (KotlinParser.TypeContext typeContext in context.type())
            {
                TypeListener typeListener = new TypeListener();
                typeContext.EnterRule(typeListener);
            }

            AccessFlags accessFlags = AccessFlags.AccPublic;
            if (context.modifierList() != null)
            {
                ModifierListListener modifierListListener = new ModifierListListener();
                context.modifierList().EnterRule(modifierListListener);
                accessFlags = modifierListListener.AccessFlags;
            }

            // TODO
            List<Argument> arguments = new List<Argument>(); 
            TypeName returnType = TypeName.For("void");

            MethodName methodName = new MethodName(
                classInfo.className,
                context.identifier().GetText(),
                returnType.Signature,
                arguments);

            MethodInfo methodInfo = new MethodInfo(
                methodName,
                accessFlags,
                classInfo.className,
                arguments,
                returnType,
                new SourceCodeSnippet(context.GetFullText(), SourceCodeLanguage.Kotlin));
            
            classInfo.Children.Add(methodInfo);
        }
    }

    public class ModifierListListener : KotlinParserBaseListener
    {
        public AccessFlags AccessFlags;
        public override void EnterModifierList(KotlinParser.ModifierListContext context)
        {
            AccessFlags = AccessFlags.None;
            foreach (KotlinParser.ModifierContext modifierContext in context.modifier())
            {
                ModifierListener modifierListener = new ModifierListener();
                modifierContext.EnterRule(modifierListener);

                AccessFlags |= modifierListener.AccessFlag;
            }
        }
    }

    public class ModifierListener : KotlinParserBaseListener
    {
        public AccessFlags AccessFlag = AccessFlags.None;
        public override void EnterModifier(KotlinParser.ModifierContext context)
        {
            if (context.visibilityModifier() != null)
            {
                VisibilityModifierListener visibilityModifierListener = new VisibilityModifierListener();
                context.visibilityModifier().EnterRule(visibilityModifierListener);
                AccessFlag = visibilityModifierListener.AccessFlag;
            }
        }
    }

    public class VisibilityModifierListener : KotlinParserBaseListener
    {
        public AccessFlags AccessFlag;
        public override void EnterVisibilityModifier(KotlinParser.VisibilityModifierContext context)
        {
            if (context.PRIVATE() != null)
            {
                AccessFlag = AccessFlags.AccPrivate;
            }

            if (context.PUBLIC() != null)
            {
                AccessFlag = AccessFlags.AccPublic;
            }

            if (context.INTERNAL() != null)
            {
                AccessFlag = AccessFlags.AccPrivate;
            }

            if (context.PROTECTED() != null)
            {
                AccessFlag = AccessFlags.AccProtected;
            }
        }
    }

    public class TypeListener : KotlinParserBaseListener
    {
        public override void EnterType(KotlinParser.TypeContext context)
        {
            
        }
    }

    public class ObjectDeclarationListener : KotlinParserBaseListener
    {
        readonly ClassInfo outerClassInfo;
        public ObjectDeclarationListener(ClassInfo outerClassInfo)
        {
            this.outerClassInfo = outerClassInfo;
        }

        public override void EnterObjectDeclaration(KotlinParser.ObjectDeclarationContext context)
        {
            string classNameString = $"{outerClassInfo.className.ShortName}${context.simpleIdentifier().GetText()}";
            
            ClassName className = new ClassName(
                outerClassInfo.className.ContainmentFile(),
                outerClassInfo.className.ContainmentPackage,
                classNameString);

            ClassInfo classInfo = new ClassInfo(
                className,
                new List<MethodInfo>(),
                new List<FieldInfo>(),
                AccessFlags.AccPublic,
                new List<ClassInfo>(),
                new SourceCodeSnippet("", SourceCodeLanguage.Kotlin),
                false);
            
            outerClassInfo.Children.Add(classInfo);

            if (context.classBody() != null)
            {
                ClassBodyListener classBodyListener = new ClassBodyListener(classInfo);
                context.classBody().EnterRule(classBodyListener);
            }
        }
    }

    public class PropertyDeclarationListener : KotlinParserBaseListener
    {
        readonly ClassInfo outerClassInfo;
        public PropertyDeclarationListener(ClassInfo outerClassInfo)
        {
            this.outerClassInfo = outerClassInfo;
        }

        public override void EnterPropertyDeclaration(KotlinParser.PropertyDeclarationContext context)
        {
            VariableDeclarationListener variableDeclarationListener = new VariableDeclarationListener();
            context.variableDeclaration().EnterRule(variableDeclarationListener);
            string fieldNameString = variableDeclarationListener.VariableNameString;
            TypeName typeName = TypeName.For("void"); // TODO
            AccessFlags accessFlags = AccessFlags.AccPublic;
            if (context.modifierList() != null)
            {
                ModifierListListener modifierListListener = new ModifierListListener();
                context.modifierList().EnterRule(modifierListListener);
                accessFlags = modifierListListener.AccessFlags;
            }

            FieldName fieldName = new FieldName(outerClassInfo.className, fieldNameString, typeName.Signature);
            FieldInfo fieldInfo = new FieldInfo(
                fieldName,
                outerClassInfo.className,
                accessFlags,
                new SourceCodeSnippet(context.GetFullText(), SourceCodeLanguage.Kotlin));
            
            outerClassInfo.Children.Add(fieldInfo);
        }
    }

    public class VariableDeclarationListener : KotlinParserBaseListener
    {
        public string VariableNameString;
        public override void EnterVariableDeclaration(KotlinParser.VariableDeclarationContext context)
        {
            VariableNameString = context.simpleIdentifier().GetText();
        }
    }
}