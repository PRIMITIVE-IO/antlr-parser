using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
using static PrimitiveCodebaseElements.Primitive.IEnumerableUtils;

namespace antlr_parser.Antlr4Impl.dto.converter
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
            IEnumerable<ClassDto> classesFromNestedNamespaces = ExtractNested(fileNode.Namespaces, fileNode);

            List<ClassDto> classes = new List<ClassDto>();
            bool fakePresent = false;
            if (fileNode.Fields.Count + fileNode.Methods.Count != 0)
            {
                classes.Add(new ClassDto(
                    path: fileNode.Path,
                    packageName: fileNode.PackageNode?.Name,
                    name: Path.GetFileNameWithoutExtension(fileNode.Path),
                    fullyQualifiedName: fileNode.Path,
                    methods: fileNode.Methods.Select(it => ToDto(it, fileNode.Path)).ToList(),
                    fields: fileNode.Fields.Select(ToDto).ToList(),
                    modifier: AccessFlags.None,
                    startIdx: 0,
                    endIdx: fileNode.Header.Length - 1, //TODO
                    header: fileNode.Header,
                    codeRange: fileNode.CodeRange
                ));
                fakePresent = true;
            }

            IEnumerable<ClassDto> classes2 = fileNode.Classes
                .SelectMany(classNode => ToDto(
                    classNode: classNode,
                    fileNode: fileNode,
                    parentFqn: fakePresent ? fileNode.Path : null,
                    nameSpace: null));

            return classes
                .Concat(classes2)
                .Concat(classesFromNestedNamespaces)
                .ToList();
        }

        /// <summary>
        /// extracts nested fields,methods or classes from list of namespaces
        /// </summary>
        /// <param name="namespaces"></param>
        /// <param name="extractor"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        static IEnumerable<ClassDto> ExtractNested(List<AstNode.Namespace> namespaces, AstNode.FileNode fileNode)
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
            );
        }

        static List<ClassDto> ExtractFakeClass(AstNode.Namespace ns, AstNode.FileNode fileNode, string? parentFqn)
        {
            if (ns.Fields.Count + ns.Methods.Count > 0)
            {
                string fqn = EnumerableOfNotNull(parentFqn, ns.Name).JoinToString(".");
                return new List<ClassDto>
                {
                    //TODO implement fake classes for namespaces
                    new ClassDto(
                        path: fileNode.Path,
                        packageName: ns.Name,
                        name: ns.Name,
                        fullyQualifiedName: fqn, //fileNode.Path, //parent + ns.Name,
                        methods: ns.Methods.Select(it => ToDto(it, fileNode.Path)).ToList(),
                        fields: ns.Fields.Select(it => ToDto(it)).ToList(),
                        modifier: AccessFlags.None,
                        startIdx: 0, //ns.StartIdx, 
                        endIdx: fileNode.Header.Length, //ns.EndIdx,
                        header: fileNode.Header, //ns.Header,
                        codeRange: fileNode.CodeRange //ns.CodeRange
                    )
                };
            }

            return new List<ClassDto>();
        }

        static IEnumerable<ClassDto> ToDto(
            AstNode.ClassNode classNode,
            AstNode.FileNode fileNode,
            string? parentFqn,
            AstNode.Namespace? nameSpace
        )
        {
            string fullyQualifiedName;
            string? packageName = nameSpace?.Name ?? fileNode.PackageNode?.Name;

            if (parentFqn != null)
            {
                fullyQualifiedName = EnumerableOfNotNull(parentFqn, classNode.Name)
                    .JoinToString("$");
            }
            else
            {
                fullyQualifiedName = EnumerableOfNotNull(packageName, classNode.Name)
                    .JoinToString(".");
            }

            return new List<ClassDto>
                {
                    new ClassDto(
                        path: fileNode.Path,
                        packageName: packageName,
                        classNode.Name,
                        fullyQualifiedName,
                        classNode.Methods.Select(it => ToDto(it, fullyQualifiedName)).ToList(),
                        classNode.Fields.Select(ToDto).ToList(),
                        classNode.Modifier,
                        classNode.StartIdx,
                        classNode.EndIdx,
                        classNode.Header,
                        parentClassFqn: parentFqn,
                        codeRange: classNode.CodeRange
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

        static MethodDto ToDto(AstNode.MethodNode methodNode, string classFqn)
        {
            string signature = MethodDto.MethodSignature(classFqn, methodNode.Name, new List<ArgumentDto>());

            List<ArgumentDto> arguments = methodNode.Arguments
                .Select((arg, i) => new ArgumentDto(i, arg.Name, arg.Type))
                .ToList();

            return new MethodDto(
                signature: signature,
                name: methodNode.Name,
                accFlag: methodNode.AccFlag,
                arguments: arguments,
                returnType: "void",
                sourceCode: methodNode.SourceCode,
                startIdx: methodNode.StartIdx,
                endIdx: methodNode.EndIdx,
                codeRange: methodNode.CodeRange
            );
        }

        static FieldDto ToDto(AstNode.FieldNode fieldNode)
        {
            return new FieldDto(
                name: fieldNode.Name,
                type: "void",
                accFlag: fieldNode.AccFlag,
                sourceCode: fieldNode.SourceCode,
                startIdx: fieldNode.StartIdx,
                endIdx: fieldNode.EndIdx,
                codeRange: fieldNode.CodeRange
            );
        }
    }
}