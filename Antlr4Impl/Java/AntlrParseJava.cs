using System;
using System.Collections.Generic;
using System.Linq;
using antlr_parser.Antlr4Impl.dto;
using antlr_parser.Antlr4Impl.dto.converter;
using Antlr4.Runtime;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.Java
{
    public static class AntlrParseJava
    {
        public static IEnumerable<ClassInfo> OuterClassInfosFromSource(string source, string filePath)
        {
            try
            {
                char[] codeArray = source.ToCharArray();
                AntlrInputStream inputStream = new AntlrInputStream(codeArray, codeArray.Length);

                JavaLexer lexer = new JavaLexer(inputStream);
                CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
                JavaParser parser = new JavaParser(commonTokenStream);

                parser.RemoveErrorListeners();
                parser.AddErrorListener(new ErrorListener()); // add ours

                // a compilation unit is the highest level container -> start there
                // do not call parser.compilationUnit() more than once
                CompilationUnitListener compilationUnitListener = new CompilationUnitListener(filePath);
                parser.compilationUnit().EnterRule(compilationUnitListener);
                return compilationUnitListener.OuterClassInfos;
            }
            catch (Exception e)
            {
                PrimitiveLogger.Logger.Instance().Error($"Failed to parse Java file {filePath}", e);
            }

            return new List<ClassInfo>();
        }

        public static FileDto Parse(string source, string filePath)
        {
            return ClassInfoToClassDtoConverter.ToParsingResultDto(OuterClassInfosFromSource(source, filePath).ToList(),
                source, filePath);
        }
    }
}