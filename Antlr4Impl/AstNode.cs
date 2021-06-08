using System.Collections.Generic;

namespace antlr_parser.Antlr4Impl
{
    public class AstNode
    {
        public class FileNode : AstNode
        {
            public readonly string Header;
            public readonly string Name;
            public readonly PackageNode PackageNode;
            public readonly List<ClassNode> Classes;
            public readonly List<FieldNode> Fields;
            public readonly List<MethodNode> Methods;

            public FileNode(string name,
                PackageNode packageNode,
                List<ClassNode> classes,
                List<FieldNode> fields,
                List<MethodNode> methods,
                string header
            )
            {
                Name = name;
                PackageNode = packageNode;
                Classes = classes;
                Fields = fields;
                Methods = methods;
                Header = header;
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
            public readonly string Modifier;
            public readonly int StartIdx;
            public readonly int EndIdx;
            public readonly string Header;

            public ClassNode(string name,
                List<MethodNode> methods,
                List<FieldNode> fields,
                List<ClassNode> innerClasses,
                string modifier,
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
            public readonly string AccFlag;
            public readonly string SourceCode;
            public readonly int StartIdx;
            public readonly int EndIdx;

            public MethodNode(string name, string accFlag, string sourceCode, int startIdx, int endIdx)
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
            public readonly string AccFlag;
            public readonly string SourceCode;
            public readonly int StartIdx;
            public readonly int EndIdx;

            public FieldNode(string name, string accFlag, string sourceCode, int startIdx, int endIdx)
            {
                Name = name;
                AccFlag = accFlag;
                SourceCode = sourceCode;
                StartIdx = startIdx;
                EndIdx = endIdx;
            }
        }
    }
}