using System.Collections.Generic;
using System.Linq;
using PrimitiveCodebaseElements.Primitive;
using CodeRange = PrimitiveCodebaseElements.Primitive.dto.CodeRange;

namespace antlr_parser.Antlr4Impl
{
    public class AstNode
    {
        public virtual List<AstNode> AsList()
        {
            return new List<AstNode> { this };
        }

        public class FileNode : AstNode
        {
            public readonly string Path;
            public readonly PackageNode? PackageNode;
            public readonly List<ClassNode> Classes;
            public readonly List<FieldNode> Fields;
            public readonly List<MethodNode> Methods;
            public readonly List<Namespace> Namespaces;
            public readonly SourceCodeLanguage Language;
            public readonly bool IsTest;
            public readonly CodeRange? CodeRange;

            public FileNode(string path,
                PackageNode? packageNode,
                List<ClassNode> classes,
                List<FieldNode> fields,
                List<MethodNode> methods,
                List<Namespace> namespaces,
                SourceCodeLanguage language,
                bool isTest,
                CodeRange? codeRange)
            {
                Path = path;
                PackageNode = packageNode;
                Classes = classes;
                Fields = fields;
                Methods = methods;
                Namespaces = namespaces;
                Language = language;
                IsTest = isTest;
                CodeRange = codeRange;
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
            public readonly CodeRange? CodeRange;

            public ClassNode(string name,
                List<MethodNode> methods,
                List<FieldNode> fields,
                List<ClassNode> innerClasses,
                AccessFlags modifier,
                int startIdx,
                CodeRange? codeRange
            )
            {
                Name = name;
                Methods = methods;
                Fields = fields;
                InnerClasses = innerClasses;
                Modifier = modifier;
                StartIdx = startIdx;
                CodeRange = codeRange;
            }
        }

        public class MethodNode : AstNode
        {
            public readonly string Name;
            public readonly AccessFlags AccFlag;
            public readonly int StartIdx;
            public readonly CodeRange? CodeRange;
            public readonly List<ArgumentNode> Arguments;
            public readonly string ReturnType;
            public readonly ArgumentNode? Receiver;

            public MethodNode(
                string name,
                AccessFlags accFlag,
                int startIdx,
                CodeRange? codeRange,
                List<ArgumentNode> arguments,
                string returnType,
                ArgumentNode? receiver = default
            )
            {
                Name = name;
                AccFlag = accFlag;
                CodeRange = codeRange;
                StartIdx = startIdx;
                Arguments = arguments;
                ReturnType = returnType;
                Receiver = receiver;
            }
        }

        public class ArgumentNode : AstNode
        {
            public readonly string Name;
            public readonly string Type;

            public ArgumentNode(string name, string type)
            {
                Name = name;
                Type = type;
            }
        }

        public class FieldNode : AstNode
        {
            public readonly string Name;
            public readonly AccessFlags AccFlag;
            public readonly int StartIdx;
            public readonly CodeRange CodeRange;

            public FieldNode(
                string name,
                AccessFlags accFlag,
                int startIdx,
                CodeRange codeRange
            )
            {
                Name = name;
                AccFlag = accFlag;
                StartIdx = startIdx;
                CodeRange = codeRange;
            }
        }

        public class Namespace : AstNode
        {
            public readonly string Name;
            public readonly List<ClassNode> Classes;
            public readonly List<FieldNode> Fields;
            public readonly List<MethodNode> Methods;
            public readonly List<Namespace> Namespaces;

            public Namespace(
                string name,
                List<ClassNode> classes,
                List<FieldNode> fields,
                List<MethodNode> methods,
                List<Namespace> namespaces
            )
            {
                Name = name;
                Classes = classes;
                Fields = fields;
                Methods = methods;
                Namespaces = namespaces;
            }
        }

        public class Comment : AstNode
        {
            public readonly int EndIdx;

            public Comment(int endIdx)
            {
                EndIdx = endIdx;
            }
        }

        public class NodeList : AstNode
        {
            public readonly List<AstNode> Nodes;

            public NodeList(List<AstNode> nodes)
            {
                Nodes = nodes;
            }

            public override List<AstNode> AsList()
            {
                return Nodes;
            }

            public static AstNode Combine(AstNode? aggregate, AstNode? element)
            {
                if (aggregate == null)
                {
                    return element;
                }

                if (element == null)
                {
                    return aggregate;
                }

                if (aggregate is NodeList list)
                {
                    return new NodeList(list.Nodes.Concat(new List<AstNode> { element }).ToList());
                }

                return new NodeList(new List<AstNode> { aggregate, element });
            }
        }
    }
}