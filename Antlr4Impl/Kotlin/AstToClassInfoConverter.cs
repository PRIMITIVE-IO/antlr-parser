using System.Collections.Generic;
using System.IO;
using System.Linq;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.Kotlin
{
    public static class AstToClassInfoConverter
    {
        static readonly TypeName VoidType = TypeName.For("void");

        public static ClassInfo ToClassInfo(Ast.File astFile)
        {
            string classNameFromFile = Path.GetFileNameWithoutExtension(astFile.Name);
            ClassName className = new ClassName(
                new FileName(astFile.Name),
                new PackageName(astFile.Package.Name),
                classNameFromFile);

            return new ClassInfo(
                className,
                astFile.Methods.Select(method => ToMethodInfo(method, className)).ToList(),
                astFile.Fields.Select(field => ToFieldInfo(field, className)),
                AccessFlags.AccPublic,
                astFile.Classes.Select(klass =>
                    ToClassInfo(klass, astFile.Name, astFile.Package.Name, classNameFromFile)),
                new SourceCodeSnippet("", SourceCodeLanguage.Kotlin),
                false);
        }

        static ClassInfo ToClassInfo(Ast.Klass klass, string fileName, string package, string parentClassName)
        {
            string stringClassName = parentClassName switch
            {
                null => klass.Name,
                _ => $"{parentClassName}${klass.Name}"
            };

            ClassName className = new ClassName(
                new FileName(fileName),
                new PackageName(package),
                stringClassName);

            return new ClassInfo(
                className,
                klass.Methods.Select(method => ToMethodInfo(method, className)),
                klass.Fields.Select(field => ToFieldInfo(field, className)),
                ToAccessFlag(klass.Modifier),
                klass.InnerClasses.Select(inner => ToClassInfo(inner, fileName, package, stringClassName)),
                new SourceCodeSnippet("", SourceCodeLanguage.Kotlin),
                false);
        }

        static FieldInfo ToFieldInfo(Ast.Field field, ClassName className)
        {
            return new FieldInfo(
                new FieldName(className, field.Name, VoidType.Signature),
                className,
                ToAccessFlag(field.AccFlag),
                new SourceCodeSnippet(field.SourceCode, SourceCodeLanguage.Kotlin));
        }

        static MethodInfo ToMethodInfo(Ast.Method method, ClassName className)
        {
            return new MethodInfo(
                new MethodName(className, method.Name, VoidType.Signature, new List<Argument>()),
                ToAccessFlag(method.AccFlag),
                className,
                new List<Argument>(),
                VoidType,
                new SourceCodeSnippet(method.SourceCode, SourceCodeLanguage.Kotlin)
            );
        }

        static AccessFlags ToAccessFlag(string accFlag)
        {
            return accFlag switch
            {
                null => AccessFlags.AccPublic,
                "public" => AccessFlags.AccPublic,
                "private" => AccessFlags.AccPrivate,
                "internal" => AccessFlags.AccProtected,
                _ => AccessFlags.AccPublic
            };
        }
    }
}