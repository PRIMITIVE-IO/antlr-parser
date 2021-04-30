using System.Collections.Generic;

namespace antlr_parser.Antlr4Impl.Kotlin
{
    public class Ast
    {
        public class File : Ast
        {
            public readonly string Name;
            public readonly Package Package;
            public readonly List<Klass> Classes;
            public readonly List<Field> Fields;
            public readonly List<Method> Methods;

            public File(string name, 
                Package package, 
                List<Klass> classes,
                List<Field> fields, 
                List<Method> methods)
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
            public readonly List<Method> Methods;
            public readonly List<Field> Fields;
            public readonly List<Klass> InnerClasses;
            public readonly string Modifier;

            public Klass(string name,
                List<Method> methods,
                List<Field> fields,
                List<Klass> innerClasses,
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