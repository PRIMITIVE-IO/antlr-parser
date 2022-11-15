using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using PrimitiveCodebaseElements.Primitive.dto;

namespace antlr_parser
{
    [PublicAPI]
    [Obsolete(message: "PrimitiveAntlrParser should be used instead")]
    public static class ParserHandler
    {
        // see: https://github.com/dyne/file-extension-list
        public static readonly HashSet<string> SupportedParsableFiles = PrimitiveAntlrParser.SupportedParsableFiles;

        public static FileDto? FileDtoFromSourceText(
            string filePath,
            string sourceText,
            bool verbose = false)
        {
            return PrimitiveAntlrParser.FileDtoFromSourceText(filePath, sourceText, verbose);
        }

        public static string GetTextFromFilePath(string filePath, bool verbose = false)
        {
            return PrimitiveAntlrParser.GetTextFromFilePath(filePath, verbose);
        }
    }
}