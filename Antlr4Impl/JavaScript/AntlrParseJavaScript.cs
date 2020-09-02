using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.JavaScript
{
    public class AntlrParseJavaScript
    {
        public static IEnumerable<ClassInfo> OuterClassInfosFromJavaScriptSource(string source, string filePath)
        {
            try
            {
                char[] codeArray = source.ToCharArray();
                AntlrInputStream inputStream = new AntlrInputStream(codeArray, codeArray.Length);

                JavaScriptLexer lexer = new JavaScriptLexer(inputStream);
                CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
                JavaScriptParser parser = new JavaScriptParser(commonTokenStream);

                parser.RemoveErrorListeners();
                parser.AddErrorListener(new ErrorListener()); // add ours

                // a program is the highest level container -> start there
                ProgramListener programListener = new ProgramListener(filePath);
                parser.program().EnterRule(programListener);
                return new List<ClassInfo> {programListener.FileClassInfo};
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return new List<ClassInfo>();
        }
    }
}