using System;
using System.Collections.Generic;
using System.IO;
using Antlr4.Runtime;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.Solidity
{
    public static class AntlrParseSolidity
    {
        public static IEnumerable<ClassInfo> OuterClassInfosFromKotlinSource(string source, string filePath)
        {
            try
            {
                char[] codeArray = source.ToCharArray();
                AntlrInputStream inputStream = new AntlrInputStream(codeArray, codeArray.Length);

                SolidityLexer lexer = new SolidityLexer(inputStream);
                CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
                SolidityParser parser = new SolidityParser(commonTokenStream);

                parser.RemoveErrorListeners();
                parser.AddErrorListener(new ErrorListener()); // add ours 

                // if the parser.kotlinFile() is called once it parses, if it is called twice it doesn't parse!
                SourceUnitListener sourceUnitListener = new SourceUnitListener(filePath);
                parser.sourceUnit().EnterRule(sourceUnitListener);

                return new List<ClassInfo> {sourceUnitListener.FileClassInfo};
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return null;
        }

        class SourceUnitListener : SolidityBaseListener
        {
            public ClassInfo FileClassInfo;
            readonly string filePath;

            public SourceUnitListener(string filePath)
            {
                this.filePath = filePath;
            }

            public override void EnterSourceUnit(SolidityParser.SourceUnitContext context)
            {
                ClassName fileClassName = new ClassName(
                    new FileName(filePath),
                    new PackageName(),
                    Path.GetFileNameWithoutExtension(filePath));

                FileClassInfo = new ClassInfo(
                    fileClassName,
                    new List<MethodInfo>(),
                    new List<FieldInfo>(),
                    AccessFlags.AccPublic,
                    new List<ClassInfo>(),
                    new SourceCodeSnippet("", SourceCodeLanguage.Solidity),
                    false);

                foreach (SolidityParser.ContractDefinitionContext contractDefinitionContext in context
                    .contractDefinition())
                {
                    ContractDefinitionListener contractDefinitionListener =
                        new ContractDefinitionListener(FileClassInfo);
                    contractDefinitionContext.EnterRule(contractDefinitionListener);
                }
            }
        }
    }
}