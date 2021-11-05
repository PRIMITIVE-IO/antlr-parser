using System.Collections.Generic;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl
{
    public class AstNode
    {
        public class FileNode : AstNode
        {
            public readonly string Header;
            public readonly string Path;
            public readonly PackageNode PackageNode;
            public readonly List<ClassNode> Classes;
            public readonly List<FieldNode> Fields;
            public readonly List<MethodNode> Methods;
            public readonly List<Namespace> Namespaces;
            public readonly SourceCodeLanguage Language;
            public readonly bool IsTest;

            public FileNode(string path,
                PackageNode packageNode,
                List<ClassNode> classes,
                List<FieldNode> fields,
                List<MethodNode> methods,
                string header,
                SourceCodeLanguage language,
                bool isTest) : this(path, packageNode, classes, fields, methods, header, new List<Namespace>(), language, isTest)
            {
            }

            public FileNode(string path,
                PackageNode packageNode,
                List<ClassNode> classes,
                List<FieldNode> fields,
                List<MethodNode> methods,
                string header,
                List<Namespace> namespaces,
                SourceCodeLanguage language,
                bool isTest)
            {
                Path = path;
                PackageNode = packageNode;
                Classes = classes;
                Fields = fields;
                Methods = methods;
                Header = header;
                Namespaces = namespaces;
                Language = language;
                IsTest = isTest;
            }
        }

        public class PackageNode : AstNode
        {
            public readonly string Name;

            public PackageNode(string name)
            {
                Name = name;
            }
        }

        public class ClassNode : AstNode
        {
            public readonly string Name;
            public readonly List<MethodNode> Methods;
            public readonly List<FieldNode> Fields;
            public readonly List<ClassNode> InnerClasses;
            public readonly AccessFlags Modifier;
            public readonly int StartIdx;
            public readonly int EndIdx;
            public readonly string Header;

            public ClassNode(string name,
                List<MethodNode> methods,
                List<FieldNode> fields,
                List<ClassNode> innerClasses,
                AccessFlags modifier,
                int startIdx,
                int endIdx,
                string header
            )
            {
                Name = name;
                Methods = methods;
                Fields = fields;
                InnerClasses = innerClasses;
                Modifier = modifier;
                StartIdx = startIdx;
                EndIdx = endIdx;
                Header = header;
            }
        }

        public class MethodNode : AstNode
        {
            public readonly string Name;
            public readonly AccessFlags AccFlag;
            public readonly string SourceCode;
            public readonly int StartIdx;
            public readonly int EndIdx;

            public MethodNode(string name, AccessFlags accFlag, string sourceCode, int startIdx, int endIdx)
            {
                Name = name;
                AccFlag = accFlag;
                SourceCode = sourceCode;
                EndIdx = endIdx;
                StartIdx = startIdx;
            }
        }

        public class FieldNode : AstNode
        {
            public readonly string Name;
            public readonly AccessFlags AccFlag;
            public readonly string SourceCode;
            public readonly int StartIdx;
            public readonly int EndIdx;

            public FieldNode(string name, AccessFlags accFlag, string sourceCode, int startIdx, int endIdx)
            {
                Name = name;
                AccFlag = accFlag;
                SourceCode = sourceCode;
                StartIdx = startIdx;
                EndIdx = endIdx;
            }
        }

        public class Namespace : AstNode
        {
            public readonly List<ClassNode> Classes;
            public readonly List<FieldNode> Fields;
            public readonly List<MethodNode> Methods;
            public readonly List<Namespace> Namespaces;
    
            public Namespace(List<ClassNode> classes, List<FieldNode> fields, List<MethodNode> methods, List<Namespace> namespaces)
            {
                Classes = classes;
                Fields = fields;
                Methods = methods;
                Namespaces = namespaces;
            }
        }
    }
}