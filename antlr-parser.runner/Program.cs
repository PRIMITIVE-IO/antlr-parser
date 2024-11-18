using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.IO;
using System.Linq;
using System.Reflection;
using antlr_parser;
using log4net;
using log4net.Config;
using log4net.Repository;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
using FileInfo = System.IO.FileInfo;

// Root command and options
RootCommand rootCommand =
[
    new Option<bool>(
        ["--verbose", "-v"],
        () => false,
        "Verbose"),
    new Argument<string>("InputPath")
];

rootCommand.Description = "Parse Files";

// Command handler
rootCommand.Handler = CommandHandler.Create<string, bool>(Parse);

// Logger setup
SetupLogger();

// Invoke the command
rootCommand.Invoke(args);

// Exit the script
Environment.Exit(0);
return;

// Methods used in the script
void Parse(string inputPath, bool verbose)
{
    IEnumerable<FileDto> fileDtos = FilePathsFrom(inputPath)
        .Where(filePath => PrimitiveAntlrParser.SupportedParsableFiles.Contains(Path.GetExtension(filePath)))
        .SelectNotNull(filePath => ParseFile(filePath, verbose));

    if (!verbose) return;
    
    foreach (FileDto fileDto in fileDtos)
    {
        PrintFileDto(fileDto);
    }
}

IEnumerable<string> FilePathsFrom(string inputPath)
{
    return File.GetAttributes(inputPath).HasFlag(FileAttributes.Directory) 
        ? Directory.GetFiles(inputPath, "*.*", SearchOption.AllDirectories) 
        : [inputPath];
}

FileDto? ParseFile(string filePath, bool verbose)
{
    return PrimitiveAntlrParser.FileDtoFromSourceText(
        filePath,
        PrimitiveAntlrParser.GetTextFromFilePath(filePath),
        verbose);
}

void PrintFileDto(FileDto fileDto)
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

void SetupLogger()
{
    ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
    XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
}
