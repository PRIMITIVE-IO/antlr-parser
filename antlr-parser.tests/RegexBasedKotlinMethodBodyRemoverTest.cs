using System;
using System.Collections.Generic;
using antlr_parser.Antlr4Impl;
using antlr_parser.Antlr4Impl.Kotlin;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using Xunit;

namespace antlr_parser.tests;

public class RegexBasedKotlinMethodBodyRemoverTest
{
    [Fact]
    public void ShouldReturnPositionsForRemoval()
    {
        string source = @"
                fun f(){ REMOVE }
                fun g() { REMOVE }
            ".Unindent();

        //Act
        List<Tuple<int, int>> blocksToRemove = RegexBasedKotlinMethodBodyRemover.FindBlocksToRemove(source);

        //Verify
        MethodBodyRemovalResult.From(source, blocksToRemove).ShortenedSource
            .TrimIndent2()
            .Should().Be(@"
                fun f()
                fun g()
            ".TrimIndent2());
    }
}