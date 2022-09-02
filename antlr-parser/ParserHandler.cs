using System.Collections.Generic;
using System.IO;
using antlr_parser.Antlr4Impl.CSharp;
using antlr_parser.Antlr4Impl.C;
using antlr_parser.Antlr4Impl.CPP;
using antlr_parser.Antlr4Impl.Java;
using antlr_parser.Antlr4Impl.JavaScript;
using antlr_parser.Antlr4Impl.Kotlin;
using antlr_parser.Antlr4Impl.Python;
using antlr_parser.Antlr4Impl.Solidity;
using antlr_parser.Antlr4Impl.TypeScript;
using JetBrains.Annotations;
using PrimitiveCodebaseElements.Primitive.dto;

namespace antlr_parser
{
    [PublicAPI]
    public static class ParserHandler
    {
        // see: https://github.com/dyne/file-extension-list
        public static readonly HashSet<string> SupportedParsableFiles =
            new HashSet<string>
            {
                ".java", ".cs", ".h", ".hxx", ".hpp", ".cpp", ".c", ".cc", ".m", ".py", ".py3", ".js", ".jsx", ".kt",
                ".sol", ".ts"
            };

        public static FileDto? FileDtoFromSourceText(
            string filePath,
            string sourceExtension,
            string sourceText,
            bool verbose = false)
        {
            if (!SupportedParsableFiles.Contains(sourceExtension)) return null;

            if (verbose)
            {
                PrimitiveLogger.Logger.Instance().Info($"Parsing: {filePath}");
            }

            switch (sourceExtension)
            {
                case ".java":
                    return AntlrParseJava.Parse(
                        sourceText,
                        filePath);
                case ".js":
                case ".jsx":
                    return AntlrParseJavaScript.Parse(
                        sourceText,
                        filePath);
                case ".ts":
                    return AntlrParseTypeScript.Parse(
                        sourceText,
                        filePath);
                case ".cs":
                    // cs
                    return AntlrParseCSharp.Parse(
                        sourceText,
                        filePath);
                case ".h":
                case ".c":
                    // C
                    return AntlrParseC.Parse(sourceText, filePath);
                case ".cpp":
                case ".hxx":
                case ".hpp":
                case ".m":
                case ".cc":
                    //cpp
                    return AntlrParseCpp.Parse(sourceText, filePath);
                case ".py":
                    // python
                    return AntlrParsePython3.Parse(sourceText, filePath);
                case ".kt":
                    return AntlrParseKotlin.Parse(
                        sourceText,
                        filePath);
                case ".sol":
                    return AntlrParseSolidity.Parse(
                        sourceText,
                        filePath);
            }

            return null;
        }

        public static string GetTextFromFilePath(string filePath, bool verbose = false)
        {
            string ext = Path.GetExtension(filePath);
            try
            {
                return SupportedParsableFiles.Contains(ext)
                    ? File.ReadAllText(filePath)
                    : "binary data";
            }
            catch (IOException e)
            {
                return e.Message;
            }
        }
    }
}