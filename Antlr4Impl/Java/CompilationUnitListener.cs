using System.Collections.Generic;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.Java
{
    /// <summary>
    /// Top level listener for Java files - call this first
    /// </summary>
    public class CompilationUnitListener : JavaParserBaseListener
    {
        public readonly List<ClassInfo> OuterClassInfos = new List<ClassInfo>();
        readonly string filePath;

        public CompilationUnitListener(string filePath)
        {
            this.filePath = filePath;
        }

        public override void EnterCompilationUnit(JavaParser.CompilationUnitContext context)
        {
            PackageDeclarationListener packageDeclarationListener = new PackageDeclarationListener();
            if (context.packageDeclaration() != null)
            {
                context.packageDeclaration().EnterRule(packageDeclarationListener);
            }

            string qualifiedName = packageDeclarationListener.QualifiedName;

            TypeDeclarationListener typeDeclarationListener = new TypeDeclarationListener(filePath, qualifiedName);
            foreach (JavaParser.TypeDeclarationContext typeDeclarationContext in context.typeDeclaration())
            {
                typeDeclarationContext.EnterRule(typeDeclarationListener);

                // this type declaration can be a class, enum, or interface
                if (typeDeclarationListener.OuterClassInfo != null)
                {
                    OuterClassInfos.Add(typeDeclarationListener.OuterClassInfo);
                    typeDeclarationListener.OuterClassInfo = null;
                }

                if (typeDeclarationListener.InterfaceInfo != null)
                {
                    OuterClassInfos.Add(typeDeclarationListener.InterfaceInfo);
                    typeDeclarationListener.InterfaceInfo = null;
                }

                if (typeDeclarationListener.EnumInfo != null)
                {
                    OuterClassInfos.Add(typeDeclarationListener.EnumInfo);
                    typeDeclarationListener.EnumInfo = null;
                }
            }
        }

        class PackageDeclarationListener : JavaParserBaseListener
        {
            public string QualifiedName = "";

            public override void EnterPackageDeclaration(JavaParser.PackageDeclarationContext context)
            {
                QualifiedName = context.qualifiedName().GetText();
            }
        }
    }
}