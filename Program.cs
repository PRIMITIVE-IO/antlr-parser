using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Reflection;
using log4net;
using log4net.Config;
using log4net.Repository;
using PrimitiveCodebaseElements.Primitive;
using Argument = System.CommandLine.Argument;
using FileInfo = System.IO.FileInfo;

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
            SetupLogger();

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
            PrimitiveLogger.Logger.Instance().Info(classInfo.className.ShortName);

            foreach (ICodebaseElementInfo infoChild in classInfo.Children)
            {
                PrimitiveLogger.Logger.Instance().Info($"-{infoChild.Name.ShortName}");
            }

            foreach (ClassInfo innerClass in classInfo.InnerClasses)
            {
                PrintClass(innerClass);
            }
        }

        static void SetupLogger()
        {
            ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
        }
    }
}