using System.Collections.Generic;
using System.IO;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.Kotlin
{
    public class KotlinFileListener : KotlinParserBaseListener
    {
        public ClassInfo FileClassInfo;
        readonly string filePath;

        public KotlinFileListener(string filePath)
        {
            this.filePath = filePath;
        }

        public override void EnterKotlinFile(KotlinParser.KotlinFileContext context)
        {
            PackageName packageName = new PackageName();
            if (context.preamble() != null)
            {
                PreambleListener preambleListener = new PreambleListener();
                context.preamble().EnterRule(preambleListener);
                if (!string.IsNullOrEmpty(preambleListener.ThisPackageName))
                {
                    packageName = new PackageName(preambleListener.ThisPackageName);
                }
            }

            ClassName fileClassName = new ClassName(
                new FileName(filePath),
                packageName,
                Path.GetFileNameWithoutExtension(filePath));

            FileClassInfo = new ClassInfo(
                fileClassName,
                new List<MethodInfo>(),
                new List<FieldInfo>(),
                AccessFlags.AccPublic,
                new List<ClassInfo>(),
                new SourceCodeSnippet("", SourceCodeLanguage.Kotlin),
                false);

            if (context.topLevelObject() != null)
            {
                foreach (KotlinParser.TopLevelObjectContext topLevelObjectContext in context.topLevelObject())
                {
                    TopLevelObjectListener topLevelObjectListener =
                        new TopLevelObjectListener(FileClassInfo, packageName);
                    topLevelObjectContext.EnterRule(topLevelObjectListener);
                }
            }
        }

        class PreambleListener : KotlinParserBaseListener
        {
            public string ThisPackageName = "";

            public override void EnterPreamble(KotlinParser.PreambleContext context)
            {
                if (context.packageHeader() != null)
                {
                    PackageHeaderListener packageHeaderListener = new PackageHeaderListener();
                    context.packageHeader().EnterRule(packageHeaderListener);

                    ThisPackageName = packageHeaderListener.ThisPackageName;
                }
            }

            class PackageHeaderListener : KotlinParserBaseListener
            {
                public string ThisPackageName = "";

                public override void EnterPackageHeader(KotlinParser.PackageHeaderContext context)
                {
                    ThisPackageName = context.identifier().GetText();
                }
            }
        }

        class TopLevelObjectListener : KotlinParserBaseListener
        {
            readonly ClassInfo fileClassInfo;

            public TopLevelObjectListener(ClassInfo fileClassInfo, PackageName packageName)
            {
                this.fileClassInfo = fileClassInfo;
            }

            public override void EnterTopLevelObject(KotlinParser.TopLevelObjectContext context)
            {
                if (context.classDeclaration() != null)
                {
                    ClassDeclarationListener classDeclarationListener = new ClassDeclarationListener(fileClassInfo);
                    context.classDeclaration().EnterRule(classDeclarationListener);
                }

                if (context.functionDeclaration() != null)
                {
                    FunctionDeclarationListener functionDeclarationListener =
                        new FunctionDeclarationListener(fileClassInfo);
                    context.functionDeclaration().EnterRule(functionDeclarationListener);
                }

                if (context.objectDeclaration() != null)
                {
                    ObjectDeclarationListener objectDeclarationListener = new ObjectDeclarationListener(fileClassInfo);
                    context.objectDeclaration().EnterRule(objectDeclarationListener);
                }
            }
        }
    }
}