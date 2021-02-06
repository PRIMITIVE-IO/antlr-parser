using System.Collections.Generic;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.JavaScript
{
    public class ClassDeclarationListener : JavaScriptParserBaseListener
    {
        readonly ClassInfo outerClassInfo;

        public ClassDeclarationListener(ClassInfo outerClassInfo)
        {
            this.outerClassInfo = outerClassInfo;
        }

        public override void EnterClassDeclaration(JavaScriptParser.ClassDeclarationContext context)
        {
            string classNameString =
                $"{outerClassInfo.className.ShortName}${context.identifier().Identifier().GetText()}";

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
                new SourceCodeSnippet(context.GetFullText(), SourceCodeLanguage.JavaScript),
                false);

            outerClassInfo.Children.Add(classInfo);

            // get class members
            ClassTailListener classTailListener = new ClassTailListener(classInfo);
            context.classTail().EnterRule(classTailListener);
        }
    }

    public class ClassTailListener : JavaScriptParserBaseListener
    {
        readonly ClassInfo classInfo;

        public ClassTailListener(ClassInfo classInfo)
        {
            this.classInfo = classInfo;
        }

        public override void EnterClassTail(JavaScriptParser.ClassTailContext context)
        {
            foreach (JavaScriptParser.ClassElementContext classElementContext in context.classElement())
            {
                if (classElementContext.methodDefinition() != null)
                {
                    MethodDefinitionListener methodDefinitionListener = new MethodDefinitionListener(classInfo);
                    classElementContext.methodDefinition().EnterRule(methodDefinitionListener);
                }
            }
        }
    }

    public class MethodDefinitionListener : JavaScriptParserBaseListener
    {
        readonly ClassInfo classInfo;

        public MethodDefinitionListener(ClassInfo classInfo)
        {
            this.classInfo = classInfo;
        }

        public override void EnterMethodDefinition(JavaScriptParser.MethodDefinitionContext context)
        {
            // TODO
            List<Argument> arguments = new List<Argument>();
            TypeName returnType = TypeName.For("void");

            MethodName expressionMethodName = new MethodName(
                classInfo.className,
                context.propertyName().GetText(),
                returnType.Signature,
                arguments);

            MethodInfo expressionMethodInfo = new MethodInfo(
                expressionMethodName,
                AccessFlags.AccPublic,
                classInfo.className,
                arguments,
                returnType,
                new SourceCodeSnippet(context.GetFullText(), SourceCodeLanguage.JavaScript));

            classInfo.Children.Add(expressionMethodInfo);
        }
    }
}