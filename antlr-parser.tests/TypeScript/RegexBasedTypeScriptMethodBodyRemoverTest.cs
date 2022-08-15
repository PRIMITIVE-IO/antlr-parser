using System;
using System.Collections.Generic;
using antlr_parser.Antlr4Impl.TypeScript;
using FluentAssertions;
using Xunit;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.tests.TypeScript;

public class RegexBasedTypeScriptMethodBodyRemoverTest
{
    [Fact]
    public void KeepCurly()
    {
        string source = "function f(x){ return 10 }";
        List<Tuple<int, int>> findBlocksToRemove = RegexBasedTypeScriptMethodBodyRemover.FindBlocksToRemove(source);

        findBlocksToRemove.Count.Should().Be(1);
        findBlocksToRemove[0].Item1.Should().Be(14);
        findBlocksToRemove[0].Item2.Should().Be(24);

        source.Substring(findBlocksToRemove[0]).Should().Be(" return 10 ");
    }

    [Fact]
    public void KeepCurly2()
    {
        string source = "function f(x)  { return 10 }";
        List<Tuple<int, int>> findBlocksToRemove = RegexBasedTypeScriptMethodBodyRemover.FindBlocksToRemove(source);

        source.Substring(findBlocksToRemove[0]).Should().Be(" return 10 ");
    }

    [Fact]
    public void RemoveNothing()
    {
        string source = "function f(x) {}";
        List<Tuple<int, int>> blocksToRemove = RegexBasedTypeScriptMethodBodyRemover.FindBlocksToRemove(source);

        blocksToRemove.Count.Should().Be(0);
    }
}