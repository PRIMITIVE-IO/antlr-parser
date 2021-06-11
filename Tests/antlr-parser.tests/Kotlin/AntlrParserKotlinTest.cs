using System.Linq;
using antlr_parser.Antlr4Impl;
using antlr_parser.Antlr4Impl.Kotlin;
using FluentAssertions;
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
            var res = AntlrParseKotlin.OuterClassInfosFromSource(source, "path");
            var topClass = res.First();
            topClass.Name.ShortName.Should().Be("X");
            topClass.InnerClasses.Should().BeEmpty();
        }
        
        [Fact]
        void CreateFakeClass()
        {
            string source = @"
                class X{}
                fun f(){}
            ";
            var res = AntlrParseKotlin.OuterClassInfosFromSource(source, "path");
            var fakeClass = res.First();
            fakeClass.Name.ShortName.Should().Be("path");
            fakeClass.Methods.Count().Should().Be(1);
            fakeClass.InnerClasses.First().Name.ShortName.Should().Be("X");
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
            
            var res = AntlrParseKotlin.OuterClassInfosFromSource(source, "path");
            var topClass = res.First();
            topClass.SourceCode.Text.Should().Be(@"
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
            var res = AntlrParseKotlin.OuterClassInfosFromSource(source, "path");
            var innerClass = res.First().InnerClasses.First();
            innerClass.SourceCode.Text.Should().Be(@"
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
            var res = AntlrParseKotlin.OuterClassInfosFromSource(source, "path");
            var secondClass = res.Skip(1).First();
            secondClass.SourceCode.Text.Should().Be(@"
                /**comment*/
                class Y {}
            ".TrimIndent().Trim());
        }
    }
}