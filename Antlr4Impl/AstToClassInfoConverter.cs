using System.Collections.Generic;
using System.IO;
using System.Linq;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl
{
    public static class AstToClassInfoConverter
    {
        static readonly TypeName VoidType = TypeName.For("void");

        public static List<ClassInfo> ToClassInfo(AstNode.FileNode astFileNode, SourceCodeLanguage language)
        {
            if (astFileNode.Methods.Any() || astFileNode.Fields.Any() || astFileNode.Classes.Count == 0)
            {
                string classNameFromFile = Path.GetFileNameWithoutExtension(astFileNode.Name);
                ClassName className = new ClassName(
                    new FileName(astFileNode.Name),
                    new PackageName(astFileNode.PackageNode.Name),
                    classNameFromFile);

                return new List<ClassInfo>
                {
                    new ClassInfo(
                        className,
                        astFileNode.Methods.Select(method => ToMethodInfo(method, className, language)).ToList(),
                        astFileNode.Fields.Select(field => ToFieldInfo(field, className, language)),
                        AccessFlags.AccPublic,
                        astFileNode.Classes.Select(klass => ToClassInfo(
                            klass,
                            astFileNode.Name,
                            astFileNode.PackageNode.Name,
                            classNameFromFile,
                            language)),
                        new SourceCodeSnippet(astFileNode.Header, language),
                        false)
                };
            }

            return astFileNode.Classes.Select(klass => ToClassInfo(
                    klass,
                    astFileNode.Name,
                    astFileNode.PackageNode.Name,
                    null,
                    language))
                .ToList();
        }

        static ClassInfo ToClassInfo(
            AstNode.ClassNode classNode,
            string fileName,
            string package,
            string parentClassName,
            SourceCodeLanguage language)
        {
            string stringClassName;
            switch (parentClassName)
            {
                case null:
                    stringClassName = classNode.Name;
                    break;
                default:
                    stringClassName = $"{parentClassName}${classNode.Name}";
                    break;
            }

            ClassName className = new ClassName(
                new FileName(fileName),
                new PackageName(package),
                stringClassName);

            IEnumerable<MethodInfo> methodInfos =
                classNode.Methods.Select(method => ToMethodInfo(method, className, language));
            IEnumerable<FieldInfo> fieldInfos =
                classNode.Fields.Select(field => ToFieldInfo(field, className, language));
            IEnumerable<ClassInfo> innerClasses = classNode.InnerClasses.Select(inner => ToClassInfo(
                inner,
                fileName,
                package,
                stringClassName,
                language));

            return new ClassInfo(
                className,
                methodInfos,
                fieldInfos,
                ToAccessFlag(classNode.Modifier),
                innerClasses,
                new SourceCodeSnippet(classNode.Header, language),
                false);
        }

        static FieldInfo ToFieldInfo(AstNode.FieldNode fieldNode, ClassName className, SourceCodeLanguage language)
        {
            return new FieldInfo(
                new FieldName(className, fieldNode.Name, VoidType.Signature),
                className,
                ToAccessFlag(fieldNode.AccFlag),
                new SourceCodeSnippet(fieldNode.SourceCode, language));
        }

        static MethodInfo ToMethodInfo(AstNode.MethodNode methodNode, ClassName className, SourceCodeLanguage language)
        {
            return new MethodInfo(
                new MethodName(className, methodNode.Name, VoidType.Signature, new List<Argument>()),
                ToAccessFlag(methodNode.AccFlag),
                className,
                new List<Argument>(),
                VoidType,
                new SourceCodeSnippet(methodNode.SourceCode, language)
            );
        }

        static AccessFlags ToAccessFlag(string accFlag)
        {
            switch (accFlag)
            {
                case null:
                case "public":
                    return AccessFlags.AccPublic;
                case "private":
                    return AccessFlags.AccPrivate;
                case "internal":
                case "protected":
                    return AccessFlags.AccProtected;
                default:
                    return AccessFlags.AccPublic;
            }
        }
    }
}