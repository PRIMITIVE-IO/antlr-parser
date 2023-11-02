using System.Linq;
using antlr_parser.Antlr4Impl.Kotlin;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
using Xunit;

namespace antlr_parser.tests.Kotlin;

public class AntlrParserKotlinTest
{
    [Fact]
    void DoNotCreateFakeClassIfNotNecessary()
    {
        string source = @"
                class X
            ".TrimIndent2();
        FileDto res = AntlrParseKotlin.Parse(source, "path");
        res.Classes.Should().HaveCount(1);
        ClassDto topClass = res.Classes.First();
        topClass.Name.Should().Be("X");
    }

    [Fact]
    void CreateFakeClass()
    {
        string source = @"
                class X{}
                fun f(){}
            ".TrimIndent2();
        FileDto res = AntlrParseKotlin.Parse(source, "path");
        res.Classes.Should().HaveCount(1);

        res.Functions.Count().Should().Be(1);
        res.Classes[0].Name.Should().Be("X");
    }


    [Fact]
    void FirstRealClassHasFullHeader()
    {
        string source = @"
                package x
                import y
                /**comment*/
                class X {
                    fun f(){}
                }
            ".TrimIndent2();

        FileDto res = AntlrParseKotlin.Parse(source, "path");
        res.Classes.Should().HaveCount(1);
        ClassDto topClass = res.Classes.First();
        topClass.CodeRange.Of(source).Should().Be(@"
                package x
                import y
                /**comment*/
                class X {
            ".TrimIndent2());
    }

    [Fact]
    void InnerClassHeader()
    {
        string source = @"
                class X {
                    /**comment*/
                    class Y {
                        fun g()
                    }
                }
            ".TrimIndent2();
        FileDto res = AntlrParseKotlin.Parse(source, "path");
        res.Classes.Should().HaveCount(2);
        ClassDto innerClass = res.Classes[1];
        innerClass.CodeRange.Of(source).Should().Be(
            @"
                |/**comment*/
                |    class Y {
            ".TrimMargin()
        );
    }

    [Fact]
    void SecondClassHeader()
    {
        string source = @"
                class X {}
                /**comment*/
                class Y {}
            ".TrimIndent2();
        FileDto res = AntlrParseKotlin.Parse(source, "path");
        res.Classes.Should().HaveCount(2);
        ClassDto secondClass = res.Classes[1];
        secondClass.CodeRange.Of(source).Should().Be(
            @"
                /**comment*/
                class Y {}
            ".TrimIndent2()
        );
    }

    [Fact]
    void ParseObject()
    {
        string source = @"
                // comment
                object X {
                    val y = 10
                    fun f(x: Int): Int { 
                        return 10 
                    }
                }
            ".TrimIndent2();
        FileDto res = AntlrParseKotlin.Parse(source, "path");
        res.Classes.Should().HaveCount(1);
        ClassDto obj = res.Classes[0];
        obj.CodeRange.Of(source).Should().Be(
            @"
                // comment
                object X {
            ".TrimIndent2()
        );
        obj.Fields.Should().HaveCount(1);
        FieldDto field = obj.Fields.First();
        field.Name.Should().Be("y");
        obj.Methods.Should().HaveCount(1);
        MethodDto method = obj.Methods.First();
        method.Name.Should().Be("f");
    }
    
    
    [Fact]
    void ParseInterface()
    {
        string source = @"
                // comment
                interface X {
                    fun f(x: Int)
                }
            ".TrimIndent2();
        FileDto res = AntlrParseKotlin.Parse(source, "path");
        res.Classes.Should().HaveCount(1);
        ClassDto obj = res.Classes[0];
        obj.CodeRange.Of(source).Should().Be(
            @"
                // comment
                interface X {
            ".TrimIndent2()
        );
        obj.Methods.Should().HaveCount(1);
        MethodDto method = obj.Methods.First();
        method.Name.Should().Be("f");
    }
}