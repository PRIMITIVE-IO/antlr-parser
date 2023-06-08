using System.Collections.Generic;
using System.IO;
using antlr_parser.Antlr4Impl.C;
using antlr_parser.Antlr4Impl.Cobol;
using antlr_parser.Antlr4Impl.CPP;
using antlr_parser.Antlr4Impl.CSharp;
using antlr_parser.Antlr4Impl.Go;
using antlr_parser.Antlr4Impl.Java;
using antlr_parser.Antlr4Impl.JavaScript;
using antlr_parser.Antlr4Impl.Kotlin;
using antlr_parser.Antlr4Impl.Python;
using antlr_parser.Antlr4Impl.Solidity;
using antlr_parser.Antlr4Impl.TypeScript;
using PrimitiveCodebaseElements.Primitive.dto;

namespace antlr_parser
{
    public static class PrimitiveAntlrParser
    {
        // see: https://github.com/dyne/file-extension-list
        public static readonly HashSet<string> SupportedParsableFiles = new()
        {
            ".java", ".cs", ".h", ".hxx", ".hpp", ".cpp", ".c", ".cc", ".m", ".py", ".py3", ".js", ".jsx", ".kt",
            ".sol", ".ts", ".go", ".cbl"
        };

        public static FileDto? FileDtoFromSourceText(
            string filePath,
            string sourceText,
            bool verbose = false)
        {
            string sourceExtension = Path.GetExtension(filePath);
            if (!SupportedParsableFiles.Contains(sourceExtension)) return null;

            if (verbose)
            {
                PrimitiveLogger.Logger.Instance().Info($"Parsing: {filePath}");
            }

            return sourceExtension switch
            {
                ".java" => AntlrParseJava.Parse(sourceText, filePath),
                ".js" or ".jsx" => AntlrParseJavaScript.Parse(sourceText, filePath),
                ".ts" => AntlrParseTypeScript.Parse(sourceText, filePath),
                ".cs" =>
                    // cs
                    AntlrParseCSharp.Parse(sourceText, filePath),
                ".h" or ".c" =>
                    // C
                    AntlrParseC.Parse(sourceText, filePath),
                ".cpp" or ".hxx" or ".hpp" or ".m" or ".cc" =>
                    //cpp
                    AntlrParseCpp.Parse(sourceText, filePath),
                ".py" =>
                    // python
                    AntlrParsePython3.Parse(sourceText, filePath),
                ".kt" => AntlrParseKotlin.Parse(sourceText, filePath),
                ".sol" => AntlrParseSolidity.Parse(sourceText, filePath),
                ".go" => AntlrParseGo.Parse(sourceText, filePath),
                ".cbl" => AntlrParseCobol.Parse(sourceText, filePath),
                _ => null
            };
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