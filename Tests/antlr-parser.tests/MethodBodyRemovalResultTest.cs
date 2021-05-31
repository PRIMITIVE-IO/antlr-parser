using System;
using System.Collections.Generic;
using antlr_parser.Antlr4Impl;
using FluentAssertions;
using Xunit;

namespace antlr_parser.tests
{
    public class MethodBodyRemovalResultTest
    {
        [Fact]
        public void CreateCorrectIndicesForRemovedBlocks()
        {
            string source = @"
                fun f(){ REMOVE }
                fun g() { REMOVE }
            ".TrimIndent();

            List<Tuple<int, int>> blocksToRemove = new List<Tuple<int, int>>
            {
                new Tuple<int, int>(8, 17), // first '{ REMOVE }' block
                new Tuple<int, int>(26, 36) // second ' { REMOVE }' block including leading space
            };

            //Act
            MethodBodyRemovalResult result = MethodBodyRemovalResult.From(source, blocksToRemove);

            //Verify
            string expectedSource = @"
                fun f()
                fun g()
            ".TrimIndent();
            result.Source.Should().Be(expectedSource);

            result.IdxToRemovedMethodBody.Count.Should().Be(2);
            result.IdxToRemovedMethodBody[7].Should().Be("{ REMOVE }"); // 7 - is a position of ')' in 'fun f()' 
            result.IdxToRemovedMethodBody[15].Should().Be(" { REMOVE }"); // 15 is a position of ')' in 'fun g()'
        }

        [Fact]
        public void IgnoreNestedBlocks()
        {
            string source = @"
                fun f(){ fun h() {} }
            ".TrimIndent();

            List<Tuple<int, int>> blocksToRemove = new List<Tuple<int, int>>
            {
                new Tuple<int, int>(8, 21), // outer fun f(){ fun h() {} }
                new Tuple<int, int>(18, 19) // inner fun h() {}
            };

            //Act
            MethodBodyRemovalResult result = MethodBodyRemovalResult.From(source, blocksToRemove);

            //Verify
            string expectedSource = @"
                fun f()
            ".TrimIndent();
            result.Source.Should().Be(expectedSource);

            result.IdxToRemovedMethodBody.Count.Should().Be(1);
            result.IdxToRemovedMethodBody[7].Should().Be("{ fun h() {} }"); // 7 - is a position of ')' in 'fun f()' 
        }
    }
}