using System.Collections.Generic;
using antlr_parser.Antlr4Impl.Python;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using Xunit;

namespace antlr_parser.tests.Python
{
    public class LastNonWhitespaceIndexerTest
    {
        [Fact]
        public void SingleLineWithSpaces()
        {
            string source = @"
                1234 1234
            ".TrimIndent2();

            Dictionary<int, int> actual = LastNonWhitespaceIndexer.IdxToLastNonWhiteSpace(source);

            actual.Should().Contain(4, 3);
        }

        [Fact]
        public void Multiline()
        {
            string source = @"
                1234
                1234
            ".TrimIndent2();

            Dictionary<int, int> actual = LastNonWhitespaceIndexer.IdxToLastNonWhiteSpace(source);

            actual.Should().Contain(4, 3);
        }
    }
}