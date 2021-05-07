using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using Xunit;

namespace antlr_parser.Antlr4Impl.JavaScript
{
    public class AntlrParseJavaScriptTest
    {
        [Fact]
        public void TestJs()
        {
            string source = @"
                class C{
                    f(x){ return 10}
                }
            ".TrimIndent();
            IEnumerable<ClassInfo> res = AntlrParseJavaScript.OuterClassInfosFromSource(source, "any/path");
            res.Count().Should().Be(1);
            ClassInfo script = res.First();
            script.Name.ShortName.Should().Be("path");

            script.Children.Count.Should().Be(1);
            ClassInfo klass = script.Children[0] as ClassInfo;
            klass.Name.ShortName.Should().Be("C");
            klass.Methods.Count().Should().Be(1);
            MethodInfo method = klass.Methods.First();
            method.Name.ShortName.Should().Be("f");
        }
        
        [Fact]
        public void CanParseMethodsWithoutBodies()
        {
            string source = @"
                class C{
                    f(x)
                }
            ".TrimIndent();
            IEnumerable<ClassInfo> res = AntlrParseJavaScript.OuterClassInfosFromSource(source, "any/path");
            res.Count().Should().Be(1);
            ClassInfo script = res.First();
            script.Name.ShortName.Should().Be("path");

            script.Children.Count.Should().Be(1);
            ClassInfo klass = script.Children[0] as ClassInfo;
            klass.Name.ShortName.Should().Be("C");
            klass.Methods.Count().Should().Be(1);
            MethodInfo method = klass.Methods.First();
            method.Name.ShortName.Should().Be("f");
        }
    }
}