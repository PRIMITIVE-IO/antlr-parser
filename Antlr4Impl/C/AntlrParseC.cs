using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Antlr4.Runtime;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.C
{
    public static class AntlrParseC
    {
        public static IEnumerable<ClassInfo> OuterClassInfosFromSource(string source, string filePath)
        {
            try
            {
                ImmutableList<Tuple<int,int>> blocksToRemove = RegexBasedCMethodBodyRemover.FindBlocksToRemove(source);
                MethodBodyRemovalResult methodBodyRemovalResult = MethodBodyRemovalResult.From(source, blocksToRemove);
                
                char[] codeArray = methodBodyRemovalResult.Source.ToCharArray();
                AntlrInputStream inputStream = new AntlrInputStream(codeArray, codeArray.Length);

                CLexer lexer = new CLexer(inputStream);
                CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
                CParser parser = new CParser(commonTokenStream);

                parser.RemoveErrorListeners();
                parser.AddErrorListener(new ErrorListener()); // add ours

                // a compilation unit is the highest level container -> start there
                // do not call parser.compilationUnit() more than once
                CParser.CompilationUnitContext compilationUnitContext = parser.compilationUnit();
                AstNode.FileNode astNode = compilationUnitContext.Accept(new CVisitor(filePath, methodBodyRemovalResult)) as AstNode.FileNode;

                return new List<ClassInfo> {AstToClassInfoConverter.ToClassInfo(astNode, SourceCodeLanguage.C)};
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return new List<ClassInfo>();
        }
    }
}