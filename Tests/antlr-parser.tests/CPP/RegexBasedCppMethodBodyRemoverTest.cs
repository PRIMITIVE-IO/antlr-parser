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
        
        [Fact]
        void ShouldRemoveBody()
        {
            string source = @"
                bool DecodeBase64PSBT(PartiallySignedTransaction& psbt, const std::string& base64_tx, std::string& error) const {REMOVE}
            ".TrimIndent();
            var blocksToRemove = RegexBasedCppMethodBodyRemover.FindBlocksToRemove(source);
            MethodBodyRemovalResult.From(source, blocksToRemove).ShortenedSource.Should().Be(@"
                bool DecodeBase64PSBT(PartiallySignedTransaction& psbt, const std::string& base64_tx, std::string& error) const {}
            ".TrimIndent());
        }
    }
}