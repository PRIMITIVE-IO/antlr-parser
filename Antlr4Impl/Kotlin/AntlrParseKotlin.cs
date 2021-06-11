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
                List<Tuple<int, int>> blocksToRemove = ClassBasedMethodBodyRemover.FindBlocksToRemove(source);
                MethodBodyRemovalResult removalMethodBodyRemovalResult = MethodBodyRemovalResult.From(source, blocksToRemove);

                char[] codeArray = removalMethodBodyRemovalResult.ShortenedSource.ToCharArray();
                AntlrInputStream inputStream = new AntlrInputStream(codeArray, codeArray.Length);

                KotlinLexer lexer = new KotlinLexer(inputStream);
                CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
                KotlinParser parser = new KotlinParser(commonTokenStream);

                parser.RemoveErrorListeners();
                parser.AddErrorListener(new ErrorListener()); // add ours 

                // a KotlinFile is the highest level container -> start there
                // do not call parser.kotlinFile() more than once
                KotlinParser.KotlinFileContext kotlinFileContext = parser.kotlinFile();
                AstNode.FileNode astFileNode = kotlinFileContext.Accept(
                    new KotlinVisitor(filePath, removalMethodBodyRemovalResult)) as AstNode.FileNode;

                return AstToClassInfoConverter.ToClassInfo(astFileNode, SourceCodeLanguage.Kotlin);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return null;
        }
    }
}