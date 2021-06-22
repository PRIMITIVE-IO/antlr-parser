using antlr_parser.Antlr4Impl;
using antlr_parser.Antlr4Impl.C;
using FluentAssertions;
using Xunit;

namespace antlr_parser.tests.CPP
{
    public class RegexBasedCppMethodBodyRemoverTest
    {
        [Fact]
        void ShouldKeepCurlies()
        {
            string source = @"
                fun f(){REMOVE}
            ".TrimIndent();
            var blocksToRemove = RegexBasedCppMethodBodyRemover.FindBlocksToRemove(source);
            MethodBodyRemovalResult.From(source, blocksToRemove).ShortenedSource.Should().Be(@"
                fun f(){}
            ".TrimIndent());
        }
    }
}