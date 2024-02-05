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
    static void Main(string[] args)
    {
        // Create a root command with some options 
        RootCommand rootCommand =
        [
            new Option<bool>(
                ["--verbose", "-v"],
                () => false,
                "Verbose"),

            new Argument<string>("InputPath")
        ];

        rootCommand.Description = "Parse Files";

        // Note that the parameters of the handler method are matched according to the names of the options 
        rootCommand.Handler = CommandHandler.Create<string, bool>(Parse);

        SetupLogger();
        rootCommand.Invoke(args);
        
        Environment.Exit(0);
    }

    static void Parse(string inputPath, bool verbose)
    {
        IEnumerable<FileDto> fileDtos = FilePathsFrom(inputPath)
            .Where(filePath => PrimitiveAntlrParser.SupportedParsableFiles.Contains(Path.GetExtension(filePath)))
            .SelectNotNull(filePath => ParseFile(filePath, verbose));

        if (verbose)
        {
            foreach (FileDto fileDto in fileDtos)
            {
                PrintFileDto(fileDto);
            }
        }
    }

    static IEnumerable<string> FilePathsFrom(string inputPath)
    {
        return File.GetAttributes(inputPath).HasFlag(FileAttributes.Directory) 
            ? Directory.GetFiles(inputPath, "*.*", SearchOption.AllDirectories) 
            : new[] { inputPath };
    }

    static FileDto? ParseFile(string filePath, bool verbose)
    {
        return PrimitiveAntlrParser.FileDtoFromSourceText(
            filePath,
            PrimitiveAntlrParser.GetTextFromFilePath(filePath),
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