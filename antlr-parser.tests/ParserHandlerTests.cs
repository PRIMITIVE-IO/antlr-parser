using System.Linq;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
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
            FileDto result = ParserHandler.FileDtoFromSourceText("test.java", ".java", testJavaClassSourceCode);

            //Verify
            result.Should().NotBeNull();
            result.Classes.Count().Should().Be(2);
        }

        [Fact]
        public void ParserHandlerShouldReturnCollectionWithAnyClassInfoWithClassName()
        {
            //Act
            FileDto result = ParserHandler.FileDtoFromSourceText("test.java", ".java", testJavaClassSourceCode);

            //Verify
            result.Classes[0].Methods.Any(x => x.Name == "doWork").Should().BeTrue();
        }

        [Fact]
        public void KotlinClassHasHeader()
        {
            //string source = System.IO.File.ReadAllText(@"Resources/KotlinExample.kt");
            //Act
            string source = @"
                package pkg
                
                /** comment */
                class C {
                  fun method() {
                    println()
                  }
                }
                
                fun outerFunction() { }

            ".TrimIndent(); 
            
            FileDto result = ParserHandler.FileDtoFromSourceText("kotlin.kt", ".kt", source);

            //Verify
            ClassDto fileInfo = result.Classes.First();
            fileInfo.Header.Should().Be(
                @"package pkg

                  /** comment */".TrimIndent());

            ClassDto classInfo = result.Classes[1];
            classInfo.Name.Should().Be("C");
            classInfo.Header.Should().Be(@"
                package pkg
                
                /** comment */
                class C {
            ".TrimIndent().Trim());

            classInfo.Methods.Single().SourceCode.Should().Be("fun method() {\n  println()\n}");
        }
    }
}