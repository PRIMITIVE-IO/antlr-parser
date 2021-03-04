using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.Solidity
{
    public static class AntlrParseSolidity
    {
        public static IEnumerable<ClassInfo> OuterClassInfosFromSource(string source, string filePath)
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

                // a sourceUnit is the highest level container -> start there
                // do not call parser.sourceUnit() more than once
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
            public readonly List<ClassInfo> ClassInfos = new List<ClassInfo>();
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