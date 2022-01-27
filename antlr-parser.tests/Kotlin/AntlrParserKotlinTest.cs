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
            ";
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
            ";
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
            ".TrimIndent();

            FileDto res = AntlrParseKotlin.Parse(source, "path");
            res.Classes.Should().HaveCount(1);
            ClassDto topClass = res.Classes.First();
            topClass.Header.Should().Be(@"
                package x
                import y
                /**comment*/
                class X {
            ".TrimIndent().Trim());
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
            ".TrimIndent();
            FileDto res = AntlrParseKotlin.Parse(source, "path");
            res.Classes.Should().HaveCount(2);
            ClassDto innerClass = res.Classes[1];
            innerClass.Header.Should().Be(@"
                /**comment*/
                class Y {
            ".TrimIndent().Trim());
        }

        [Fact]
        void SecondClassHeader()
        {
            string source = @"
                class X {}
                /**comment*/
                class Y {}
            ";
            FileDto res = AntlrParseKotlin.Parse(source, "path");
            res.Classes.Should().HaveCount(2);
            ClassDto secondClass = res.Classes[1];
            secondClass.Header.Should().Be(@"
                /**comment*/
                class Y {}
            ".TrimIndent().Trim());
        }
    }
}