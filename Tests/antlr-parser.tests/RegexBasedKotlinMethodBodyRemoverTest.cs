using System;
using System.Collections.Generic;
using antlr_parser.Antlr4Impl;
using antlr_parser.Antlr4Impl.Kotlin;
using FluentAssertions;
using Xunit;

namespace antlr_parser.tests
{
    public class RegexBasedKotlinMethodBodyRemoverTest
    {

        [Fact]
        public void ShouldReturnPositionsForRemoval()
        {

            string source = @"
                fun f(){ REMOVE }
                fun g() { REMOVE }
            ".TrimIndent();

            //Act
            List<Tuple<int,int>> blocksToRemove = RegexBasedKotlinMethodBodyRemover.FindBlocksToRemove(source);

            //Verify
            blocksToRemove.Count.Should().Be(2);
            blocksToRemove[0].Should().Be(new Tuple<int, int>(8, 17)); //first '{ REMOVE }' block
            blocksToRemove[1].Should().Be(new Tuple<int, int>(26, 36));//second ' { REMOVE }' block including leading space
        }
    }
}
