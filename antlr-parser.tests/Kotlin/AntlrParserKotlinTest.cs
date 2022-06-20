using System.Linq;
using antlr_parser.Antlr4Impl.Kotlin;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
using Xunit;

namespace antlr_parser.tests.Kotlin
{
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
            res.Classes.Should().HaveCount(2);

            ClassDto fakeClass = res.Classes[0];
            fakeClass.Name.Should().Be("path");
            fakeClass.Methods.Count().Should().Be(1);
            res.Classes[1].Name.Should().Be("X");
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
                |package x
                |import y
                |/**comment*/
                |class X {
                |    
            ".TrimMargin());
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
            innerClass.CodeRange.Of(source).Should().Be(@"
                |
                |    /**comment*/
                |    class Y {
                |        
            ".TrimMargin());
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
            secondClass.CodeRange.Of(source).Should().Be(@"
                |
                |/**comment*/
                |class Y {}
            ".TrimMargin());
        }
    }
}