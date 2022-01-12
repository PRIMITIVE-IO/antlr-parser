using System;
using System.Collections.Generic;
using antlr_parser.Antlr4Impl.dto.converter;
using Antlr4.Runtime;
using JetBrains.Annotations;
using PrimitiveCodebaseElements.Primitive.dto;

namespace antlr_parser.Antlr4Impl.Solidity
{
    public static class AntlrParseSolidity
    {

        [CanBeNull]
        public static FileDto Parse(string source, string filePath)
        {
            try
            {
                MethodBodyRemovalResult methodBodyRemovalResult = MethodBodyRemovalResult.From(source, new List<Tuple<int, int>>());

                char[] codeArray = source.ToCharArray();
                AntlrInputStream inputStream = new AntlrInputStream(codeArray, codeArray.Length);

                SolidityLexer lexer = new SolidityLexer(inputStream);
                CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
                SolidityParser parser = new SolidityParser(commonTokenStream);

                parser.RemoveErrorListeners();
                parser.AddErrorListener(new ErrorListener()); // add ours 

                // a sourceUnit is the highest level container -> start there
                // do not call parser.sourceUnit() more than once
                AstNode.FileNode fileNode = parser.sourceUnit().Accept(new SolidityAstVisitor(filePath, methodBodyRemovalResult)) as AstNode.FileNode;
                return AstNodeToClassDtoConverter.ToFileDto(fileNode, source);
            }
            catch (Exception e)
            {
                PrimitiveLogger.Logger.Instance().Error($"Failed to parse Solidity file {filePath}", e);
            }

            return null;
        }
    }
}