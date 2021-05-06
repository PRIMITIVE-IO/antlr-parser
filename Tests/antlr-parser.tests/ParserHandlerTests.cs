using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace antlr_parser.tests
{
    public class ParserHandlerTests
    {
        private readonly string testJavaClassSourceCode;

        public ParserHandlerTests()
        {
            testJavaClassSourceCode = System.IO.File.ReadAllText(@"Resources/TestJavaClass.java");
        }


        [Fact]
        public void ParserHandlerShouldReturnCollectionWithOneElement()
        {
            //Act
            var result = ParserHandler.ClassInfoFromSourceText("test.java", ".java", testJavaClassSourceCode);

            //Verify
            result.Should().NotBeNull();
            result.Count().Should().Be(1);
        }

        [Fact]
        public void ParserHandlerShouldReturnCollectionWithAnyClassInfoWithClassName()
        {
            //Act
            var result = ParserHandler.ClassInfoFromSourceText("test.java", ".java", testJavaClassSourceCode);

            //Verify
            result.ToList().First().Children.Any(x => x.Name.ShortName == "doWork").Should().BeTrue();
        }
    }
}
