using System;
using System.Collections.Generic;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.dto
{
    public class FileDto
    {
        public readonly String Text;
        public readonly string Path;
        public readonly bool IsTest;
        public readonly List<ClassDto> Classes;
        public readonly SourceCodeLanguage Language;

        public FileDto(string fileName, string text, string path, bool isTest, List<ClassDto> classes, SourceCodeLanguage language)
        {
            Text = text;
            Path = path;
            IsTest = isTest;
            Classes = classes;
            Language = language;
        }
    }
}