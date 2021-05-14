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
            FilePathsFrom(args.InputPath)
                .AsParallel()
                .Where(filePath => ParserHandler.SupportedParsableFiles.Contains(Path.GetExtension(filePath)))
                .SelectMany(ParseFile)
                .ForAll(PrintClass);
        }

        static IEnumerable<string> FilePathsFrom(string inputPath)
        {
            if (File.GetAttributes(inputPath).HasFlag(FileAttributes.Directory))
            {
                return Directory.GetFiles(inputPath, "*.*", SearchOption.AllDirectories);
            }

            return new[] {inputPath};
        }

        static IEnumerable<ClassInfo> ParseFile(string filePath)
        {
            return ParserHandler.ClassInfoFromSourceText(
                filePath,
                Path.GetExtension(filePath),
                ParserHandler.GetTextFromFilePath(filePath));
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