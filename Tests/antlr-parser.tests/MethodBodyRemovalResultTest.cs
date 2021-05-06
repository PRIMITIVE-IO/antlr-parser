using System;
using System.Collections.Immutable;
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

            ImmutableList<Tuple<int, int>> blocksToRemove = ImmutableList.Create(
                new Tuple<int, int>(8, 17), // first '{ REMOVE }' block
                new Tuple<int, int>(26, 36)// second ' { REMOVE }' block including leading space
            );

            //Act
            MethodBodyRemovalResult result = MethodBodyRemovalResult.From(source, blocksToRemove);

            //Verify
            string expectedSource = @"
                fun f()
                fun g()
            ".TrimIndent();
            result.Source.Should().Be(expectedSource);

            result.IdxToRemovedMethodBody.Count.Should().Be(2);
            result.IdxToRemovedMethodBody[7].Should().Be("{ REMOVE }");// 7 - is a position of ')' in 'fun f()' 
            result.IdxToRemovedMethodBody[15].Should().Be(" { REMOVE }"); // 15 is a position of ')' in 'fun g()'
        }
    }
}
