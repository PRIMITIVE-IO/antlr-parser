using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            string filePath = args[0];

            string sourceText = ParserHandler.GetTextFromFilePath(filePath);
            List<ClassInfo> classInfos = ParserHandler.ClassInfoFromSourceText(
                filePath,
                Path.GetExtension(filePath),
                sourceText).ToList();

            foreach (ClassInfo classInfo in classInfos)
            {
                Console.WriteLine(classInfo.className.ToJavaFullyQualified());
                foreach (ICodebaseElementInfo codebaseElementInfo in classInfo.Children)
                {
                    Console.WriteLine("Children:");
                    Console.WriteLine(codebaseElementInfo.Name);
                }
            }
        }
    }
}
