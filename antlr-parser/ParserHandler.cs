using System.Collections.Generic;
using System.IO;
using antlr_parser.Antlr4Impl.C;
using antlr_parser.Antlr4Impl.CPP;
using antlr_parser.Antlr4Impl.Java;
using antlr_parser.Antlr4Impl.JavaScript;
using antlr_parser.Antlr4Impl.Kotlin;
using antlr_parser.Antlr4Impl.Solidity;
using antlr_parser.Antlr4Impl.TypeScript;
using JetBrains.Annotations;
using PrimitiveCodebaseElements.Primitive.dto;

namespace antlr_parser
{
    public static class ParserHandler
    {
        // see: https://github.com/dyne/file-extension-list
        public static readonly HashSet<string> SupportedParsableFiles =
            new HashSet<string>
            {
                ".java", ".cs", ".h", ".hxx", ".hpp", ".cpp", ".c", ".cc", ".m", ".py", ".py3", ".js", ".jsx", ".kt",
                ".sol", ".ts"
            };

        static readonly HashSet<string> SupportedUnparsableFiles =
            new HashSet<string>
            {
                // files to be parsed in the future
                ".sc", ".rs", ".go", ".class", ".clj", ".cxx", ".el", ".lua", ".m4", ".php", ".pl", ".po", ".rb", ".sh",
                ".swift", ".vb",
                // other data formats
                ".txt", ".md", ".html", ".json", ".xml", ".sql", ".yaml", ".hbs", ".sh", ".vcxproj", ".xcodeproj",
                ".csproj", ".xml", ".diff", ".patch", ".log", ".rtf", ".tex", ".odt", ".org", ".pdf", ".rst", ".wpd",
                ".wps"
            };

        public static readonly HashSet<string> SupportedLibraryFiles =
            new HashSet<string>
            {
                ".jar", ".war", ".ear", // Java 
                ".dll", ".exe", // CLR
                ".so", ".lib", ".a" // Linux
            };

        [CanBeNull]
        public static FileDto FileDtoFromSourceText(
            string filePath,
            string sourceExtension,
            string sourceText)
        {
            if (!SupportedParsableFiles.Contains(sourceExtension)) return null;

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
                    return null;
                // cs
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
                case ".py3":
                    // python
                    return null;
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

        public static string GetTextFromFilePath(string filePath)
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