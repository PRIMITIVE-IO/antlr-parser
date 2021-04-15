using System;
using System.Collections.Generic;
using System.IO;
using Antlr4.Runtime;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.CPP
{
    public static class AntlrParseCpp
    {
        public static IEnumerable<ClassInfo> OuterClassInfosFromSource(string source, string filePath)
        {
            try
            {
                char[] codeArray = source.ToCharArray();
                AntlrInputStream inputStream = new AntlrInputStream(codeArray, codeArray.Length);

                CPP14Lexer lexer = new CPP14Lexer(inputStream);
                CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
                CPP14Parser parser = new CPP14Parser(commonTokenStream);

                parser.RemoveErrorListeners();
                parser.AddErrorListener(new ErrorListener()); // add ours

                // a translationunit is the highest level container -> start there
                // do not call parser.translationUnit() more than once
                TranslationUnitListener translationUnitListener = new TranslationUnitListener(filePath);
                parser.translationUnit().EnterRule(translationUnitListener);


                return new List<ClassInfo> {translationUnitListener.FileClassInfo};
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return new List<ClassInfo>();
        }
        
        class TranslationUnitListener : CPP14ParserBaseListener
        {
            public ClassInfo FileClassInfo;
            readonly string filePath;

            public TranslationUnitListener(string filePath)
            {
                this.filePath = filePath;
            }

            public override void EnterTranslationUnit(CPP14Parser.TranslationUnitContext context)
            {
                ClassName fileClassName = new ClassName(
                    new FileName(filePath),
                    new PackageName(""),
                    Path.GetFileNameWithoutExtension(filePath));

                FileClassInfo = new ClassInfo(
                    fileClassName,
                    new List<MethodInfo>(),
                    new List<FieldInfo>(),
                    AccessFlags.AccPublic,
                    new List<ClassInfo>(),
                    new SourceCodeSnippet("", SourceCodeLanguage.Cpp),
                    false);
                
                DeclarationseqListener declarationseqListener = new DeclarationseqListener(FileClassInfo);
                context.declarationseq().EnterRule(declarationseqListener);
            }
        }
    }
}