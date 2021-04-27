using System;
using System.Collections.Generic;
using System.Diagnostics;
using Antlr4.Runtime;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.Kotlin
{
    public static class AntlrParseKotlin
    {
        public static long time = 0;
        public static IEnumerable<ClassInfo> OuterClassInfosFromSource(string source, string filePath)
        {
            try
            {
                char[] codeArray = source.ToCharArray();
                AntlrInputStream inputStream = new AntlrInputStream(codeArray, codeArray.Length);

                KotlinLexer lexer = new KotlinLexer(inputStream);
                CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
                KotlinParser parser = new KotlinParser(commonTokenStream);

                parser.RemoveErrorListeners();
                parser.AddErrorListener(new ErrorListener()); // add ours 

                // a KotlinFile is the highest level container -> start there
                // do not call parser.kotlinFile() more than once
                var stopwatch = Stopwatch.StartNew();
                var kotlinFileContext = parser.kotlinFile();
                stopwatch.Stop();
                time += stopwatch.ElapsedMilliseconds;
                var astFile = kotlinFileContext.Accept(new KotlinVisitor(filePath)) as Ast.File;

                return new List<ClassInfo> { AstToClassInfoConverter.ToClassInfo(astFile) };
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return null;
        }
    }
}