using System.Collections.Generic;
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
                classes: fileNode.Classes
                    .SelectMany(classNode => ToDto(
                        classNode: classNode,
                        fileNode: fileNode,
                        parentFqn: null,
                        nameSpace: null
                    ))
                    .Concat(ExtractNested(fileNode.Namespaces, fileNode))
                    .ToList(),
                functions: ToDtos(fileNode.Methods, FileFqn(fileNode.Path, fileNode.PackageNode?.Name)),
                fields: fileNode.Fields.Select(ToDto).ToList(),
                language: fileNode.Language
            );
        }

        static string FileFqn(string path, string? package)
        {
            return EnumerableOfNotNull(path, package).JoinToString(":");
        }

        /// <summary>
        /// extracts nested fields, methods, or classes from list of namespaces
        /// </summary>
        /// <returns></returns>
        static IEnumerable<ClassDto> ExtractNested(IEnumerable<AstNode.Namespace> namespaces, AstNode.FileNode fileNode)
        {
            return namespaces.SelectMany(nameSpace =>
                {
                    IEnumerable<ClassDto> classes =
                        nameSpace.Classes.SelectMany(it => ToDto(
                            classNode: it,
                            fileNode: fileNode,
                            parentFqn: null,
                            nameSpace: nameSpace
                        ));
                    IEnumerable<ClassDto> nested = ExtractNested(nameSpace.Namespaces, fileNode);

                    return classes.Concat(nested);
                }
            );
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

        static List<MethodDto> ToDtos(IEnumerable<AstNode.MethodNode> methodNodes, string parentFullyQualifiedName)
        {
            List<MethodDto> dtos = methodNodes.Select(it => ToDto(it, parentFullyQualifiedName)).ToList();

            Dictionary<string, int> duplicatedSignaturesToCounter = dtos
                .GroupBy(methodDto => methodDto.Signature)
                .Select(methodDtos => new { signature = methodDtos.Key, count = methodDtos.Count() })
                .Where(arg => arg.count > 1)
                .ToDictionary(arg => arg.signature, x => 1);

            // rename `func init()` to `func init#1()` in case of duplicates 
            return dtos.Select(methodDto =>
                    {
                        int counter = duplicatedSignaturesToCounter.GetValueOrDefault(methodDto.Signature);
                        if (counter == 0) return methodDto;
                        duplicatedSignaturesToCounter[methodDto.Signature] = counter + 1;
                        return new MethodDto(
                            signature: methodDto.Signature.Replace("(", $"#{counter}("),
                            name: methodDto.Name,
                            accFlag: methodDto.AccFlag,
                            arguments: methodDto.Arguments,
                            returnType: methodDto.ReturnType,
                            codeRange: methodDto.CodeRange,
                            methodReferences: methodDto.MethodReferences,
                            cyclomaticScore: methodDto.CyclomaticScore
                        );
                    }
                )
                .ToList();
        }

        static string FullyQualifiedName(string path, string className, string? parentFqn, string? packageName)
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