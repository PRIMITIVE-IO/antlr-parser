using System;
using System.Collections.Generic;
using System.Linq;
using antlr_parser.Antlr4Impl.dto;
using antlr_parser.Antlr4Impl.dto.converter;
using Antlr4.Runtime;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.TypeScript
{
    public static class AntlrParseTypeScript
    {
        public static IEnumerable<ClassInfo> OuterClassInfosFromSource(string source, string filePath)
        {
            try
            {
                char[] codeArray = source.ToCharArray();
                AntlrInputStream inputStream = new AntlrInputStream(codeArray, codeArray.Length);

                TypeScriptLexer lexer = new TypeScriptLexer(inputStream);
                CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
                TypeScriptParser parser = new TypeScriptParser(commonTokenStream);

                parser.RemoveErrorListeners();
                parser.AddErrorListener(new ErrorListener()); // add ours

                // a program is the highest level container -> start there
                // do not call parser.program() more than once
                ProgramListener programListener = new ProgramListener(filePath);
                parser.program().EnterRule(programListener);
                return new List<ClassInfo> {programListener.FileClassInfo};
            }
            catch (Exception e)
            {
                PrimitiveLogger.Logger.Instance().Error($"Failed to parse TS file {filePath}", e);
            }

            return new List<ClassInfo>();
        }
        
        public static FileDto Parse(string source, string filePath)
        {
            return ClassInfoToClassDtoConverter.ToParsingResultDto(OuterClassInfosFromSource(source, filePath).ToList(), source, filePath);
        }

    }
}