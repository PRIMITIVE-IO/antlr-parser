using System;
using System.Collections.Generic;
using antlr_parser.Antlr4Impl.JavaScript;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using Xunit;

namespace antlr_parser.tests.JavaScript
{
    public class RegexBasedJavaScriptMethodBodyRemoverTest
    {
        [Fact]
        public void RemoveCurly()
        {
            string source = "function f(x){}";
            List<Tuple<int, int>> findBlocksToRemove = RegexBasedJavaScriptMethodBodyRemover.FindBlocksToRemove(source);

            findBlocksToRemove.Count.Should().Be(1);
            findBlocksToRemove[0].Item1.Should().Be(13);
            findBlocksToRemove[0].Item2.Should().Be(14);
        }

        [Fact]
        public void RemoveSpacesBeforeCurly()
        {
            string source = "function f(x) {}";
            List<Tuple<int, int>> blocksToRemove = RegexBasedJavaScriptMethodBodyRemover.FindBlocksToRemove(source);

            blocksToRemove.Count.Should().Be(1);
            blocksToRemove[0].Item1.Should().Be(13);
            blocksToRemove[0].Item2.Should().Be(15);
        }

        [Fact]
        public void FunctionDeclarationWithDoubleQuotes()
        {
            string source =
                @"function generateAssets(repo, outputName, commit = """", diffs = [], iGenerateDB = false, createDBcallback = null) {
}".Unindent();
            List<Tuple<int, int>> blocksToRemove = RegexBasedJavaScriptMethodBodyRemover.FindBlocksToRemove(source);

            blocksToRemove.Count.Should().Be(1);
        }
    }
}