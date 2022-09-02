using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.IO;
using System.Linq;
using System.Reflection;
using log4net;
using log4net.Config;
using log4net.Repository;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
using FileInfo = System.IO.FileInfo;

namespace antlr_parser.runner;

static class Program
{
    class Args
    {
        public bool Verbose { get; set; }
        public string InputPath { get; set; }
    }

    static void Main(string[] args)
    {
        // Create a root command with some options 
        RootCommand rootCommand = new RootCommand
        {
            new Option<bool>(
                new[] { "--verbose", "-v" },
                () => false,
                "Verbose"),
            new Argument<string>("InputPath")
        };

        rootCommand.Description = "Parse Files";

        // Note that the parameters of the handler method are matched according to the names of the options 
        rootCommand.Handler = CommandHandler.Create<Args>(Parse);

        SetupLogger();
        rootCommand.Invoke(args);
    }

    static void Parse(Args args)
    {
        IEnumerable<FileDto> fileDtos = FilePathsFrom(args.InputPath, args.Verbose)
            .Where(filePath => ParserHandler.SupportedParsableFiles.Contains(Path.GetExtension(filePath)))
            .SelectNotNull(filePath => ParseFile(filePath, args.Verbose));

        if (args.Verbose)
        {
            foreach (FileDto fileDto in fileDtos)
            {
                PrintFileDto(fileDto);
            }
        }
    }

    static IEnumerable<string> FilePathsFrom(string inputPath, bool verbose)
    {
        return File.GetAttributes(inputPath).HasFlag(FileAttributes.Directory) 
            ? Directory.GetFiles(inputPath, "*.*", SearchOption.AllDirectories) 
            : new[] { inputPath };
    }

    static FileDto? ParseFile(string filePath, bool verbose)
    {
        return ParserHandler.FileDtoFromSourceText(
            filePath,
            Path.GetExtension(filePath),
            ParserHandler.GetTextFromFilePath(filePath),
            verbose);
    }

    static void PrintFileDto(FileDto fileDto)
    {
        foreach (ClassDto classDto in fileDto.Classes)
        {
            PrimitiveLogger.Logger.Instance().Info("class: " + classDto.FullyQualifiedName);

            foreach (FieldDto field in classDto.Fields)
            {
                PrimitiveLogger.Logger.Instance().Info($"-field: {field.Name}");
            }

            foreach (MethodDto method in classDto.Methods)
            {
                PrimitiveLogger.Logger.Instance().Info($"-method: {method.Name}");
            }
        }
    }

    static void SetupLogger()
    {
        ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
        XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
    }
}