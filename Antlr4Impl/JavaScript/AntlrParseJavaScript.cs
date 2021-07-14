using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.JavaScript
{
    public static class AntlrParseJavaScript
    {
        public static IEnumerable<ClassInfo> OuterClassInfosFromSource(string source, string filePath)
        {
            try
            {
                List<Tuple<int,int>> blocksToRemove = RegexBasedJavaScriptMethodBodyRemover.FindBlocksToRemove(source);
                MethodBodyRemovalResult removalResult = MethodBodyRemovalResult.From(source, blocksToRemove);

                char[] codeArray = removalResult.ShortenedSource.ToCharArray();
                AntlrInputStream inputStream = new AntlrInputStream(codeArray, codeArray.Length);

                JavaScriptLexer lexer = new JavaScriptLexer(inputStream);
                CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
                JavaScriptParser parser = new JavaScriptParser(commonTokenStream);

                parser.RemoveErrorListeners();
                parser.AddErrorListener(new ErrorListener()); // add ours

                // a program is the highest level container -> start there
                // do not call parser.program() more than once
                JavaScriptParser.ProgramContext programContext = parser.program();
                AstNode.FileNode astFile = programContext.Accept(new JavaScriptAstVisitor(filePath, removalResult)) as AstNode.FileNode;
                return AstToClassInfoConverter.ToClassInfo(astFile, SourceCodeLanguage.JavaScript);
            }
            catch (Exception e)
            {
                PrimitiveLogger.Logger.Instance().Error($"Failed to parse JavaScript file {filePath}", e);

                return new List<ClassInfo>();
            }
        }
    }
}