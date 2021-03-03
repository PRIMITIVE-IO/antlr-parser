using System;
using System.Collections.Generic;
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

                return sourceUnitListener.ClassInfos;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return null;
        }

        class SourceUnitListener : SolidityBaseListener
        {
            public List<ClassInfo> ClassInfos;
            readonly string filePath;

            public SourceUnitListener(string filePath)
            {
                this.filePath = filePath;
            }

            public override void EnterSourceUnit(SolidityParser.SourceUnitContext context)
            {
                foreach (SolidityParser.ContractDefinitionContext contractDefinitionContext in context
                    .contractDefinition())
                {
                    ContractDefinitionListener contractDefinitionListener =
                        new ContractDefinitionListener(filePath);
                    contractDefinitionContext.EnterRule(contractDefinitionListener);
                    ClassInfos.Add(contractDefinitionListener.ContractClassInfo);
                }
            }
        }
    }
}