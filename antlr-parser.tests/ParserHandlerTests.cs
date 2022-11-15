using System.Linq;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
using Xunit;

namespace antlr_parser.tests;

public class ParserHandlerTests
{
    readonly string testJavaClassSourceCode;

    public ParserHandlerTests()
    {
        testJavaClassSourceCode = System.IO.File.ReadAllText(@"Resources/TestJavaClass.java");
    }


    [Fact]
    public void ParserHandlerShouldReturnCollectionWithOneElement()
    {
        //Act
        FileDto result = PrimitiveAntlrParser.FileDtoFromSourceText("test.java", testJavaClassSourceCode)!;

        //Verify
        result.Should().NotBeNull();
        result.Classes.Count().Should().Be(2);
    }

    [Fact]
    public void ParserHandlerShouldReturnCollectionWithAnyClassInfoWithClassName()
    {
        //Act
        FileDto result = PrimitiveAntlrParser.FileDtoFromSourceText("test.java", testJavaClassSourceCode)!;

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

            ".TrimIndent2();

        FileDto result = PrimitiveAntlrParser.FileDtoFromSourceText("kotlin.kt",  source)!;

        //Verify
        ClassDto fileInfo = result.Classes.First();
        fileInfo.CodeRange.Of(source).Should().Be(
            @"
                  package pkg
                  
                  /** comment */
            ".TrimIndent2()
        );

        ClassDto classInfo = result.Classes[1];
        classInfo.Name.Should().Be("C");
        classInfo.CodeRange.Of(source).Should().Be(
            @"
                package pkg
                
                /** comment */
                class C {
            ".TrimIndent2()
        );

        classInfo.Methods.Single().CodeRange.Of(source).Should().Be(
            @"
                fun method() {
                    println()
                  }
            ".TrimIndent2()
        );
    }
}