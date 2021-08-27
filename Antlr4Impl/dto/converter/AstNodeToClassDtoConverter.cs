using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace antlr_parser.Antlr4Impl.dto.converter
{
    public class AstNodeToClassDtoConverter
    {

        public static FileDto ToFileDto(AstNode.FileNode fileNode, string sourceText)
        {
            return new FileDto(
                fileName: fileNode.Path,
                text: sourceText,
                path: fileNode.Path,
                isTest: false,
                classes: ExtractClassDtos(fileNode),
                language: fileNode.Language
            );
        }

        static List<ClassDto> ExtractClassDtos(AstNode.FileNode fileNode)
        {
            return fileNode.Classes.SelectMany(classNode => ToDto(classNode, fileNode, null)).ToList();
        }

        static List<ClassDto> ToDto(AstNode.ClassNode classNode, AstNode.FileNode fileNode,
            [CanBeNull] string parentFqn)
        {
            string fullyQualifiedName = String.Join(".",
                new List<string> { parentFqn ?? fileNode.PackageNode.Name, classNode.Name }.Where(it => it != null));

            return new List<ClassDto>()
            {
                new ClassDto(
                    fileNode.Path,
                    fileNode.PackageNode.Name,
                    classNode.Name,
                    fullyQualifiedName,
                    classNode.Methods.Select(it => ToDto(it)).ToList(),
                    classNode.Fields.Select(it => ToDto(it)).ToList(),
                    classNode.Modifier,
                    classNode.StartIdx,
                    classNode.EndIdx,
                    classNode.Header
                )
            }.Concat(classNode.InnerClasses.SelectMany(it => ToDto(it, fileNode, fullyQualifiedName)).ToList()
            ).ToList();
        }

        static MethodDto ToDto(AstNode.MethodNode methodNode)
        {
            return new MethodDto(
                name: methodNode.Name,
                accFlag: methodNode.AccFlag,
                arguments: new List<ArgumentDto>(),
                returnType: "void",
                sourceCode: methodNode.SourceCode,
                startIdx: methodNode.StartIdx,
                endIdx: methodNode.EndIdx
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
                endIdx: fieldNode.EndIdx
            );
        }
    }
}