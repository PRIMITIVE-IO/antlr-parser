using System.Collections.Generic;
using System.Collections.Immutable;

namespace antlr_parser.Antlr4Impl
{
    public class AstNode
    {
        public class FileNode : AstNode
        {
            public readonly string Name;
            public readonly PackageNode PackageNode;
            public readonly ImmutableList<ClassNode> Classes;
            public readonly ImmutableList<FieldNode> Fields;
            public readonly ImmutableList<MethodNode> Methods;

            public FileNode(string name, 
                PackageNode packageNode, 
                ImmutableList<ClassNode> classes,
                ImmutableList<FieldNode> fields, 
                ImmutableList<MethodNode> methods)
            {
                Name = name;
                PackageNode = packageNode;
                Classes = classes;
                Fields = fields;
                Methods = methods;
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
            public readonly ImmutableList<MethodNode> Methods;
            public readonly ImmutableList<FieldNode> Fields;
            public readonly ImmutableList<ClassNode> InnerClasses;
            public readonly string Modifier;

            public ClassNode(string name,
                ImmutableList<MethodNode> methods,
                ImmutableList<FieldNode> fields,
                ImmutableList<ClassNode> innerClasses,
                string modifier)
            {
                Name = name;
                Methods = methods;
                Fields = fields;
                InnerClasses = innerClasses;
                Modifier = modifier;
            }
        }

        public class MethodNode : AstNode
        {
            public readonly string Name;
            public readonly string AccFlag;
            public readonly string SourceCode;

            public MethodNode(string name, string accFlag, string sourceCode)
            {
                Name = name;
                AccFlag = accFlag;
                SourceCode = sourceCode;
            }
        }

        public class FieldNode : AstNode
        {
            public readonly string Name;
            public readonly string AccFlag;
            public readonly string SourceCode;

            public FieldNode(string name, string accFlag, string sourceCode)
            {
                Name = name;
                AccFlag = accFlag;
                SourceCode = sourceCode;
            }
        }
    }
}