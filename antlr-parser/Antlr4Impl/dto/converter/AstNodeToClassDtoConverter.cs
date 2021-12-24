using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;

namespace antlr_parser.Antlr4Impl.dto.converter
{
    public class AstNodeToClassDtoConverter
    {
        public static FileDto ToFileDto(AstNode.FileNode fileNode, string sourceText)
        {
            return new FileDto(
                text: sourceText,
                path: fileNode.Path,
                isTest: false,
                classes: ExtractClassDtos(fileNode),
                language: fileNode.Language
            );
        }

        static List<ClassDto> ExtractClassDtos(AstNode.FileNode fileNode)
        {
            IEnumerable<AstNode.ClassNode> classesFromNestedNamespaces =
                AstToClassInfoConverter.ExtractNested(fileNode.Namespaces, it => it.Classes);

            List<ClassDto> classes = new List<ClassDto>();
            bool fakePresent = false;
            if (fileNode.Fields.Count + fileNode.Methods.Count != 0)
            {
                
                classes.Add(new ClassDto(
                    path: fileNode.Path,
                    packageName: fileNode.PackageNode.Name,
                    name: Path.GetFileNameWithoutExtension(fileNode.Path),
                    fullyQualifiedName: fileNode.Path,
                    methods: fileNode.Methods.Select(it => ToDto(it, fileNode.Path)).ToList(),
                    fields: fileNode.Fields.Select(it => ToDto(it)).ToList(),
                    modifier: AccessFlags.None,
                    startIdx: 0,
                    endIdx: fileNode.Header.Length - 1, //TODO
                    header: fileNode.Header,
                    codeRange: fileNode.CodeRange
                ));
                fakePresent = true;
            }

            classes.AddRange(fileNode.Classes
                .Concat(classesFromNestedNamespaces)
                .SelectMany(classNode => ToDto(classNode, fileNode, fakePresent ? fileNode.Path : null))
            );
            return classes;
        }

        static List<ClassDto> ToDto(AstNode.ClassNode classNode, AstNode.FileNode fileNode,
            [CanBeNull] string parentFqn)
        {
            string fullyQualifiedName = String.Join(".",
                new List<string> { parentFqn ?? fileNode.PackageNode.Name, classNode.Name }.Where(it => it != null));

            return new List<ClassDto>
                {
                    new ClassDto(
                        fileNode.Path,
                        fileNode.PackageNode.Name,
                        classNode.Name,
                        fullyQualifiedName,
                        classNode.Methods.Select(it => ToDto(it, classNode.Name)).ToList(),
                        classNode.Fields.Select(it => ToDto(it)).ToList(),
                        classNode.Modifier,
                        classNode.StartIdx,
                        classNode.EndIdx,
                        classNode.Header,
                        parentClassFqn: parentFqn,
                        codeRange: classNode.CodeRange
                    )
                }
                .Concat(classNode.InnerClasses.SelectMany(it => ToDto(it, fileNode, fullyQualifiedName)))
                .ToList();
        }

        static MethodDto ToDto(AstNode.MethodNode methodNode, string classFqn)
        {
            string signature = MethodDto.MethodSignature(classFqn, methodNode.Name, new List<ArgumentDto>());

            return new MethodDto(
                signature: signature,
                name: methodNode.Name,
                accFlag: methodNode.AccFlag,
                arguments: new List<ArgumentDto>(),
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