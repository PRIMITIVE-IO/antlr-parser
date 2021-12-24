using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl
{
    public static class AstToClassInfoConverter
    {
        static readonly TypeName VoidType = TypeName.For("void");

        /// <summary>
        /// extracts nested fields,methods or classes from list of namespaces
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="extractor"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<T> ExtractNested<T>(List<AstNode.Namespace> ns, Func<AstNode.Namespace, IEnumerable<T>> extractor)
        {
            return ns.SelectMany(it => extractor(it).Concat(ExtractNested(it.Namespaces, extractor)));
        }

        public static List<ClassInfo> ToClassInfo(AstNode.FileNode astFileNode, SourceCodeLanguage language)
        {
            List<AstNode.MethodNode> nsMethods = ExtractNested(astFileNode.Namespaces, ns => ns.Methods).ToList();
            List<AstNode.ClassNode> nsclasses = ExtractNested(astFileNode.Namespaces, ns => ns.Classes).ToList();
            List<AstNode.FieldNode> nsFields = ExtractNested(astFileNode.Namespaces, ns => ns.Fields).ToList();

            //combine file and namespace members
            List<AstNode.MethodNode> methods = astFileNode.Methods.Concat(nsMethods).ToList();
            List<AstNode.FieldNode> fields = astFileNode.Fields.Concat(nsFields).ToList();
            List<AstNode.ClassNode> classes = astFileNode.Classes.Concat(nsclasses).ToList();

            if (methods.Any() || fields.Any() || classes.Count == 0)
            {
                string classNameFromFile = Path.GetFileNameWithoutExtension(astFileNode.Path);
                ClassName className = new ClassName(
                    new FileName(astFileNode.Path),
                    new PackageName(astFileNode.PackageNode.Name),
                    classNameFromFile);

                return new List<ClassInfo>
                {
                    new ClassInfo(
                        className,
                        methods.Select(method => ToMethodInfo(method, className, language)).ToList(),
                        fields.Select(field => ToFieldInfo(field, className, language)),
                        AccessFlags.AccPublic,
                        classes.Select(klass => ToClassInfo(
                            klass,
                            astFileNode.Path,
                            astFileNode.PackageNode.Name,
                            classNameFromFile,
                            language)),
                        new SourceCodeSnippet(astFileNode.Header, language),
                        false)
                };
            }

            return classes.Select(klass => ToClassInfo(
                    klass,
                    astFileNode.Path,
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
                classNode.Modifier,
                innerClasses,
                new SourceCodeSnippet(classNode.Header, language),
                false);
        }

        static FieldInfo ToFieldInfo(AstNode.FieldNode fieldNode, ClassName className, SourceCodeLanguage language)
        {
            return new FieldInfo(
                new FieldName(className, fieldNode.Name, VoidType.Signature),
                className,
                fieldNode.AccFlag,
                new SourceCodeSnippet(fieldNode.SourceCode, language));
        }

        static MethodInfo ToMethodInfo(AstNode.MethodNode methodNode, ClassName className, SourceCodeLanguage language)
        {
            return new MethodInfo(
                new MethodName(className, methodNode.Name, VoidType.Signature, new List<Argument>()),
                methodNode.AccFlag,
                className,
                new List<Argument>(),
                VoidType,
                new SourceCodeSnippet(methodNode.SourceCode, language)
            );
        }
    }
}