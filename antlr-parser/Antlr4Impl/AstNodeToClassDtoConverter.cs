using System.Collections.Generic;
using System.IO;
using System.Linq;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
using static PrimitiveCodebaseElements.Primitive.IEnumerableUtils;

namespace antlr_parser.Antlr4Impl
{
    public static class AstNodeToClassDtoConverter
    {
        public static FileDto ToFileDto(AstNode.FileNode fileNode, string sourceText)
        {
            return new FileDto(
                text: sourceText,
                path: fileNode.Path,
                isTest: fileNode.IsTest,
                classes: ExtractClassDtos(fileNode),
                language: fileNode.Language
            );
        }

        static List<ClassDto> ExtractClassDtos(AstNode.FileNode fileNode)
        {
            List<ClassDto> classes = new List<ClassDto>();
            bool fakePresent = false;
            string? fakeClassFqn = null;
            List<ClassDto> classesFromNestedNamespaces = ExtractNested(fileNode.Namespaces, fileNode);

            if (fileNode.Fields.Any() || fileNode.Methods.Any() ||
                !fileNode.Classes.Any() && !classesFromNestedNamespaces.Any())
            {
                // a fake class is a representation of the file with members -> put this first
                string fakeClassName = Path.GetFileNameWithoutExtension(fileNode.Path);

                fakeClassFqn = FullyQualifiedName(
                    path: fileNode.Path,
                    className: fakeClassName,
                    parentFqn: null,
                    packageName: fileNode.PackageNode?.Name
                );

                classes.Add(new ClassDto(
                    path: fileNode.Path,
                    packageName: fileNode.PackageNode?.Name,
                    name: fakeClassName,
                    fullyQualifiedName: fakeClassFqn,
                    methods: ToDtos(fileNode.Methods, fakeClassFqn),
                    fields: fileNode.Fields.Select(ToDto).ToList(),
                    modifier: AccessFlags.None,
                    codeRange: fileNode.CodeRange,
                    referencesFromThis: new List<ClassReferenceDto>(),
                    parentClassFqn: null
                ));
                fakePresent = true;
            }

            IEnumerable<ClassDto> classesDeclaredInFile = fileNode.Classes
                .SelectMany(classNode => ToDto(
                    classNode: classNode,
                    fileNode: fileNode,
                    parentFqn: fakePresent ? fakeClassFqn : null,
                    nameSpace: null));

            return classes
                .Concat(classesDeclaredInFile)
                .Concat(classesFromNestedNamespaces)
                .ToList();
        }

        /// <summary>
        /// extracts nested fields, methods, or classes from list of namespaces
        /// </summary>
        /// <returns></returns>
        static List<ClassDto> ExtractNested(List<AstNode.Namespace> namespaces, AstNode.FileNode fileNode)
        {
            return namespaces.SelectMany(nameSpace =>
                {
                    List<ClassDto> fakeClass = ExtractFakeClass(nameSpace, fileNode, null);

                    IEnumerable<ClassDto> classes =
                        nameSpace.Classes.SelectMany(it => ToDto(
                            classNode: it,
                            fileNode: fileNode,
                            parentFqn: null,
                            nameSpace: nameSpace
                        ));
                    IEnumerable<ClassDto> nested = ExtractNested(nameSpace.Namespaces, fileNode);

                    return fakeClass.Concat(classes).Concat(nested);
                }
            ).ToList();
        }

        static List<ClassDto> ExtractFakeClass(AstNode.Namespace ns, AstNode.FileNode fileNode, string? parentFqn)
        {
            if (!ns.Fields.Any() && !ns.Methods.Any()) return new List<ClassDto>();

            string fqn = EnumerableOfNotNull(parentFqn, ns.Name).JoinToString(".");
            return new List<ClassDto>
            {
                //TODO implement fake classes for namespaces
                new(
                    path: fileNode.Path,
                    packageName: ns.Name,
                    name: ns.Name,
                    fullyQualifiedName: fqn, //fileNode.Path, //parent + ns.Name,
                    methods: ToDtos(ns.Methods, fqn),
                    fields: ns.Fields.Select(ToDto).ToList(),
                    modifier: AccessFlags.None,
                    codeRange: fileNode.CodeRange, //ns.CodeRange
                    referencesFromThis: new List<ClassReferenceDto>(),
                    parentClassFqn: null
                )
            };
        }

        static IEnumerable<ClassDto> ToDto(
            AstNode.ClassNode classNode,
            AstNode.FileNode fileNode,
            string? parentFqn,
            AstNode.Namespace? nameSpace
        )
        {
            string? packageName = nameSpace?.Name ?? fileNode.PackageNode?.Name;

            string fullyQualifiedName = FullyQualifiedName(fileNode.Path, classNode.Name, parentFqn, packageName);

            return new List<ClassDto>
                {
                    new(
                        path: fileNode.Path,
                        packageName: packageName,
                        name: classNode.Name,
                        fullyQualifiedName: fullyQualifiedName,
                        methods: ToDtos(classNode.Methods, fullyQualifiedName),
                        fields: classNode.Fields.Select(ToDto).ToList(),
                        modifier: classNode.Modifier,
                        parentClassFqn: parentFqn,
                        codeRange: classNode.CodeRange!,
                        referencesFromThis: new List<ClassReferenceDto>()
                    )
                }
                .Concat(classNode.InnerClasses.SelectMany(it => ToDto(
                    classNode: it,
                    fileNode: fileNode,
                    parentFqn: fullyQualifiedName,
                    nameSpace: nameSpace
                )))
                .ToList();
        }

        private static List<MethodDto> ToDtos(List<AstNode.MethodNode> methodNodes, string fullyQualifiedName)
        {
            List<MethodDto> dtos = methodNodes.Select(it => ToDto(it, fullyQualifiedName)).ToList();

            Dictionary<string, int> duplicatedSignaturesToCounter = dtos
                .GroupBy(x => x.Signature)
                .Select(x => new { signature = x.Key, count = x.Count() })
                .Where(x => x.count > 1)
                .ToDictionary(x => x.signature, x => 1);

            // rename `func init()` to `func init#1()` in case of duplicates 
            return dtos.Select(x =>
                    {
                        int counter = duplicatedSignaturesToCounter.GetValueOrDefault(x.Signature);
                        if (counter == 0) return x;
                        duplicatedSignaturesToCounter[x.Signature] = counter + 1;
                        return new MethodDto(
                            signature: x.Signature.Replace("(", $"#{counter}("),
                            name: x.Name,
                            accFlag: x.AccFlag,
                            arguments: x.Arguments,
                            returnType: x.ReturnType,
                            codeRange: x.CodeRange,
                            methodReferences: x.MethodReferences,
                            cyclomaticScore: x.CyclomaticScore
                        );
                    }
                )
                .ToList();
        }

        private static string FullyQualifiedName(string path, string className, string? parentFqn, string? packageName)
        {
            if (parentFqn == null)
            {
                return path + ":" + EnumerableOfNotNull(packageName, className)
                    .JoinToString(".");
            }

            return EnumerableOfNotNull(parentFqn, className)
                .JoinToString("$");
        }

        static MethodDto ToDto(AstNode.MethodNode methodNode, string classFqn)
        {
            List<ArgumentDto> arguments = methodNode.Arguments
                .Select((arg, i) => new ArgumentDto(i, arg.Name, arg.Type))
                .ToList();

            // args for signature include receiver argument as a first argument
            List<ArgumentDto> argsForSignature =
                EnumerableOfNotNull(methodNode.Receiver?.Let(r => new ArgumentDto(0, r.Name, r.Type)))
                    .Concat(arguments)
                    .ToList();

            string signature = MethodDto.MethodSignature(
                classFqn,
                methodNode.Name,
                argsForSignature
            );

            return new MethodDto(
                signature: signature,
                name: methodNode.Name,
                accFlag: methodNode.AccFlag,
                arguments: arguments,
                returnType: methodNode.ReturnType,
                codeRange: methodNode.CodeRange,
                methodReferences: new List<MethodReferenceDto>(),
                cyclomaticScore: null
            );
        }

        static FieldDto ToDto(AstNode.FieldNode fieldNode)
        {
            return new FieldDto(
                name: fieldNode.Name,
                type: "void",
                accFlag: fieldNode.AccFlag,
                codeRange: fieldNode.CodeRange
            );
        }
    }
}