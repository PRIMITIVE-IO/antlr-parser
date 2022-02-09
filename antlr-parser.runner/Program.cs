using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using log4net;
using log4net.Config;
using log4net.Repository;
using PrimitiveCodebaseElements.Primitive.dto;
using Argument = System.CommandLine.Argument;
using FileInfo = System.IO.FileInfo;

namespace antlr_parser.runner
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
            };

            rootCommand.Description = "Parse Files";

            // Note that the parameters of the handler method are matched according to the names of the options 
            rootCommand.Handler = CommandHandler.Create<Args>(Parse);

            rootCommand.Invoke(args);
        }

        static void Parse(Args args)
        {
            FilePathsFrom(args.InputPath)
                .Where(filePath => ParserHandler.SupportedParsableFiles.Contains(Path.GetExtension(filePath)))
                .Select(ParseFile)
                .Where(it => it != null)
                .ToList()
                .ForEach(PrintFileDto);
        }

        static IEnumerable<string> FilePathsFrom(string inputPath)
        {
            if (File.GetAttributes(inputPath).HasFlag(FileAttributes.Directory))
            {
                return Directory.GetFiles(inputPath, "*.*", SearchOption.AllDirectories);
            }

            return new[] {inputPath};
        }

        [CanBeNull]
        static FileDto ParseFile(string filePath)
        {
            return ParserHandler.FileDtoFromSourceText(
                filePath,
                Path.GetExtension(filePath),
                ParserHandler.GetTextFromFilePath(filePath));
        }

        static void PrintFileDto(FileDto fileDto)
        {

            foreach (ClassDto classDto in fileDto.Classes)
            {
                Console.WriteLine("class: "+classDto.FullyQualifiedName);

                foreach (FieldDto field in classDto.Fields)
                {
                    Console.WriteLine($"    field: {field.Name}: {field.SourceCode}");
                }

                foreach (MethodDto method in classDto.Methods)
                {
                    Console.WriteLine($"    method: {method.Name}: {method.SourceCode}");
                }
            }
        }

        static void SetupLogger()
        {
            ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
        }
    }
}