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
                new SourceCodeSnippet(context.GetFullText(), SourceCodeLanguage.JavaScript),
                false);

            outerClassInfo.Children.Add(classInfo);

            // get class members
            ClassTailListener classTailListener = new ClassTailListener(classInfo);
            context.classTail().EnterRule(classTailListener);
        }
    }

    public class ClassTailListener : TypeScriptParserBaseListener
    {
        readonly ClassInfo classInfo;

        public ClassTailListener(ClassInfo classInfo)
        {
            this.classInfo = classInfo;
        }

        public override void EnterClassTail(TypeScriptParser.ClassTailContext context)
        {
            foreach (TypeScriptParser.ClassElementContext classElementContext in context.classElement())
            {
                var x = 1;
            }
        }
    }
}