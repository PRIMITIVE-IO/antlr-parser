using System.Collections.Generic;
using System.IO;
using System.Linq;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.dto.converter
{
    public class ClassInfoToClassDtoConverter
    {
        public static FileDto ToParsingResultDto(List<ClassInfo> classInfos, string sourceText, string path)
        {
            return new FileDto(
                path: path,
                text: sourceText,
                fileName: Path.GetFileName(path),
                isTest: classInfos.Any(it => it.IsTestClass),
                classes: classInfos.SelectMany(it => ToClassDtos(it)).ToList(),
                language: SourceCodeSnippet.LanguageFromExtension(Path.GetExtension(path))
            );
        }

        static List<ClassDto> ToClassDtos(ClassInfo classInfo)
        {
            return classInfo.InnerClasses
                .SelectMany(ToClassDtos).ToList()
                .Concat(new List<ClassDto>
                    {
                        new ClassDto(
                            path: classInfo.className.ContainmentFile().FilePath,
                            packageName: classInfo.className.ContainmentPackage.PackageNameString,
                            name: classInfo.className.originalClassName,
                            fullyQualifiedName: classInfo.className.ToJavaFullyQualified(),
                            methods: classInfo.Methods.Select(it => ToMethodDto(it)).ToList(),
                            fields: classInfo.Fields.Select(it => ToFieldDto(it)).ToList(),
                            modifier: classInfo.accessFlags,
                            startIdx: -1,
                            endIdx: -1,
                            header: classInfo.SourceCode.Text
                        )
                    }
                ).ToList();
        }

        static FieldDto ToFieldDto(FieldInfo fieldInfo)
        {
            return new FieldDto(
                name: fieldInfo.FieldName.ShortName,
                type: fieldInfo.FieldName.FieldType,
                accFlag: fieldInfo.AccessFlags,
                sourceCode: fieldInfo.SourceCode.Text,
                startIdx: -1,
                endIdx: -1
            );
        }

        static MethodDto ToMethodDto(MethodInfo methodInfo)
        {
            return new MethodDto(
                name: methodInfo.MethodName.ShortName,
                sourceCode: methodInfo.SourceCode.Text,
                accFlag: methodInfo.accessFlags,
                startIdx: -1,
                endIdx: -1,
                arguments: ToArgumentDto(methodInfo.Arguments.ToList()),
                returnType: methodInfo.ReturnType.Signature
            );
        }

        static List<ArgumentDto> ToArgumentDto(List<Argument> arguments)
        {
            List<ArgumentDto> acc = new List<ArgumentDto>();
            for (int i = 0; i < arguments.Count; i++)
            {
                acc.Add(new ArgumentDto(
                    index: i,
                    name: arguments[i].Name,
                    type: arguments[i].Type.Signature
                ));
            }

            return acc;
        }
    }
}