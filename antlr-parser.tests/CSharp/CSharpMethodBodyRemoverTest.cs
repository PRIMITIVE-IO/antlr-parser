using System;
using System.Collections.Generic;
using antlr_parser.Antlr4Impl;
using antlr_parser.Antlr4Impl.CSharp;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using Xunit;

namespace antlr_parser.tests.CSharp;

public class CSharpMethodBodyRemoverTest
{
    [Fact]
    public void SmokeTest()
    {
        string source = @"
                using PrimitiveCodebaseElements.Primitive;
                using Xunit;

                namespace antlr_parser.tests.CSharp
                {
                    public static class T 
                    {
                        public static List<Tuple<int, int>> FindBlocksToRemove(string source) 
                        {
                            should be removed
                        }
                        static void Swap<T>(ref T lhs, ref T rhs) 
                        {
                            should be removed
                        }
                        public static void TestSwap()
                        {
                            should be removed
                        }    
                    } 
                }
            ".TrimIndent2();
        List<Tuple<int, int>> blocksToRemove = CSharpMethodBodyRemover.FindBlocksToRemove(source);
        MethodBodyRemovalResult methodBodyRemovalResult = MethodBodyRemovalResult.From(source, blocksToRemove);

        methodBodyRemovalResult.ShortenedSource.Should().Be(@"
                using PrimitiveCodebaseElements.Primitive;
                using Xunit;

                namespace antlr_parser.tests.CSharp
                {
                    public static class T 
                    {
                        public static List<Tuple<int, int>> FindBlocksToRemove(string source) 
                        {}
                        static void Swap<T>(ref T lhs, ref T rhs) 
                        {}
                        public static void TestSwap()
                        {}    
                    } 
                }
            ".TrimIndent2());
    }
}