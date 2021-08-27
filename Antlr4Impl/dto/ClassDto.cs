using System.Collections.Generic;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.dto
{
    public class ClassDto
    {
        public readonly string Path;
        public readonly string PackageName;
        public readonly string Name;
        public readonly string FullyQualifiedName;
        public readonly List<MethodDto> Methods;
        public readonly List<FieldDto> Fields;
        public readonly AccessFlags Modifier;
        public readonly int StartIdx;
        public readonly int EndIdx;
        public readonly string Header;

        public ClassDto(
            string path,
            string packageName,
            string name,
            string fullyQualifiedName,
            List<MethodDto> methods,
            List<FieldDto> fields,
            AccessFlags modifier,
            int startIdx,
            int endIdx,
            string header)
        {
            Path = path;
            PackageName = packageName;
            Name = name;
            FullyQualifiedName = fullyQualifiedName;
            Methods = methods;
            Fields = fields;
            Modifier = modifier;
            StartIdx = startIdx;
            EndIdx = endIdx;
            Header = header;
        }
    }
}