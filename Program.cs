using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using PrimitiveCodebaseElements.Primitive;
using Argument = System.CommandLine.Argument;

namespace antlr_parser
{
    static class Program
    {
        class Args
        {
            public string InputPath { get; set; }
        }

        static void Main(string[] args)
        {
            // Create a root command with some options 
            RootCommand rootCommand = new RootCommand
            {
                new Argument("InputPath")
                {
                    ArgumentType = typeof(string)
                }
            };

            rootCommand.Description = "Parse Files";

            // Note that the parameters of the handler method are matched according to the names of the options 
            rootCommand.Handler = CommandHandler.Create<Args>(Parse);

            rootCommand.Invoke(args);
        }

        static void Parse(Args args)
        {
            string intputPath = args.InputPath;

            if (intputPath.Contains('.'))
            {
                string filePath = intputPath;
                string sourceText = ParserHandler.GetTextFromFilePath(filePath);
                List<ClassInfo> classInfos = ParserHandler.ClassInfoFromSourceText(
                    filePath,
                    Path.GetExtension(filePath),
                    sourceText).ToList();

                foreach (ClassInfo classInfo in classInfos)
                {
                    PrintClass(classInfo);
                }
            }
            else
            {
                string[] allFiles = Directory.GetFiles(intputPath, "*.*", SearchOption.AllDirectories);

                foreach (string filePath in allFiles)
                {
                    string sourceText = ParserHandler.GetTextFromFilePath(filePath);
                    List<ClassInfo> classInfos = ParserHandler.ClassInfoFromSourceText(
                        filePath,
                        Path.GetExtension(filePath),
                        sourceText).ToList();

                    foreach (ClassInfo classInfo in classInfos)
                    {
                        PrintClass(classInfo);
                    }
                }
            }
        }

        static void PrintClass(ClassInfo classInfo)
        {
            Console.WriteLine(classInfo.className.ShortName);
            foreach (ICodebaseElementInfo infoChild in classInfo.Children)
            {
                Console.WriteLine(
                    $"-{infoChild.Name.ShortName}");
            }

            foreach (ClassInfo innerClass in classInfo.InnerClasses)
            {
                PrintClass(innerClass);
            }
        }
    }
}