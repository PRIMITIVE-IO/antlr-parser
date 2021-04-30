using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.Kotlin
{
    public static class AntlrParseKotlin
    {
        public static IEnumerable<ClassInfo> OuterClassInfosFromSource(string source, string filePath)
        {
            try
            {
                MethodBodyRemovalResult removalMethodBodyRemovalResult =
                    MethodBodyRemover.RemoveMethodBodyWithBraces(source, SourceCodeLanguage.Kotlin);
                char[] codeArray = removalMethodBodyRemovalResult.Source.ToCharArray();
                AntlrInputStream inputStream = new AntlrInputStream(codeArray, codeArray.Length);

                KotlinLexer lexer = new KotlinLexer(inputStream);
                CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
                KotlinParser parser = new KotlinParser(commonTokenStream);

                parser.RemoveErrorListeners();
                parser.AddErrorListener(new ErrorListener()); // add ours 

                // a KotlinFile is the highest level container -> start there
                // do not call parser.kotlinFile() more than once
                KotlinParser.KotlinFileContext kotlinFileContext = parser.kotlinFile();
                Ast.File astFile = kotlinFileContext.Accept(
                    new KotlinVisitor(filePath, removalMethodBodyRemovalResult)) as Ast.File;

                return new List<ClassInfo> {AstToClassInfoConverter.ToClassInfo(astFile)};
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return null;
        }
    }
}