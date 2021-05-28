using System;
using System.Collections.Immutable;
using antlr_parser.Antlr4Impl;
using antlr_parser.Antlr4Impl.C;
using FluentAssertions;
using Xunit;

namespace antlr_parser.tests.C
{
    public class DirectivesRemoverTest
    {
        [Fact]
        void Test()
        {
            string source = @"
                #if 1
                    textToKeep
                #elif 1
                    textToRemove
                #else
                    textToRemove
                #endif
            ".TrimIndent();

            ImmutableList<Tuple<int,int>> blocksToRemove = DirectivesRemover.FindBlocksToRemove(source);

            MethodBodyRemovalResult.From(source, blocksToRemove).Source.Should().Be("\n    textToKeep\n");
        }
        
    }
}