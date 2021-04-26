using System.Collections.Immutable;

namespace antlr_parser.Antlr4Impl.Kotlin
{
    public class Ast
    {
        public class File : Ast
        {
            public readonly string Name;
            public readonly Package Package;
            public readonly ImmutableList<Klass> Classes;
            public readonly ImmutableList<Field> Fields;
            public readonly ImmutableList<Method> Methods;

            public File(string name, Package package, ImmutableList<Klass> classes, ImmutableList<Field> fields, ImmutableList<Method> methods)
            {
                Name = name;
                Package = package;
                Classes = classes;
                Fields = fields;
                Methods = methods;
            }
        }
        public class Package : Ast
        {
            public readonly string Name;

            public Package(string name)
            {
                Name = name;
            }
        }

        public class Klass : Ast
        {
            public readonly string Name;
            public readonly ImmutableList<Method> Methods;
            public readonly ImmutableList<Field> Fields;
            public readonly ImmutableList<Klass> InnerClasses;
            public readonly string Modifier;

            public Klass(string name, ImmutableList<Method> methods, ImmutableList<Field> fields, ImmutableList<Klass> innerClasses,
                string modifier)
            {
                Name = name;
                Methods = methods;
                Fields = fields;
                InnerClasses = innerClasses;
                Modifier = modifier;
            }
        }

        public class Method : Ast
        {
            public readonly string Name;
            public readonly string AccFlag;
            public readonly string SourceCode;

            public Method(string name, string accFlag, string sourceCode)
            {
                Name = name;
                AccFlag = accFlag;
                SourceCode = sourceCode;
            }
        }

        public class Field : Ast
        {
            public readonly string Name;
            public readonly string AccFlag;
            public readonly string SourceCode;

            public Field(string name, string accFlag, string sourceCode)
            {
                Name = name;
                AccFlag = accFlag;
                SourceCode = sourceCode;
            }
        }
    }
}