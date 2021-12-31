using System.Collections.Generic;
using System.Linq;
using antlr_parser.tests;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
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

            ClassInfo klass = res.First();
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

            ClassInfo klass = res.First();
            klass.Name.ShortName.Should().Be("C");
            klass.Methods.Count().Should().Be(1);
            MethodInfo method = klass.Methods.First();
            method.Name.ShortName.Should().Be("f");
        }

        [Fact]
        public void ParseMethods()
        {
            string source = @"
                function f(x){ return 10; }
                class C{
                    g(x) { return 20; }
                }
            ".TrimIndent();
            IEnumerable<ClassInfo> res = AntlrParseJavaScript.OuterClassInfosFromSource(source, "any/path");
            res.Count().Should().Be(1);
            ClassInfo script = res.First();
            script.Name.ShortName.Should().Be("path");

            script.Methods.Count().Should().Be(1);
            script.Methods.First().Name.ShortName.Should().Be("f");
            script.Methods.First().SourceCode.Text.Should().Be("function f(x){ return 10; }");
            script.InnerClasses.Count().Should().Be(1);
            ClassInfo klass = script.InnerClasses.First();
            klass.Name.ShortName.Should().Be("C");
            klass.Methods.Count().Should().Be(1);
            MethodInfo method = klass.Methods.First();
            method.Name.ShortName.Should().Be("g");
            method.SourceCode.Text.Should().Be("g(x) { return 20; }");
        }

        [Fact]
        public void ParseFields()
        {
            string source = @"
                var x = 10;
            ".TrimIndent();
            IEnumerable<ClassInfo> res = AntlrParseJavaScript.OuterClassInfosFromSource(source, "any/path");
            res.Count().Should().Be(1);
            ClassInfo script = res.First();
            script.Name.ShortName.Should().Be("path");

            script.Fields.Count().Should().Be(1);
            script.Fields.First().Name.ShortName.Should().Be("x");
            script.Fields.First().SourceCode.Text.Should().Be("x = 10");
        }

        [Fact]
        public void ParseDeconstructedFields()
        {
            string source = @"
                const { y: { x } } = obj;
            ".TrimIndent();
            IEnumerable<ClassInfo> res = AntlrParseJavaScript.OuterClassInfosFromSource(source, "any/path");
            res.Count().Should().Be(1);
            ClassInfo script = res.First();
            script.Name.ShortName.Should().Be("path");

            script.Fields.Count().Should().Be(1);
            script.Fields.First().Name.ShortName.Should().Be("x");
            script.Fields.First().SourceCode.Text.Should().Be("{ y: { x } } = obj");
        }

        [Fact]
        public void ParseDeconstructedFieldsAsSingleFieldInfo()
        {
            string source = @"
                const { a: { x, y } } = obj;
            ".TrimIndent();
            IEnumerable<ClassInfo> res = AntlrParseJavaScript.OuterClassInfosFromSource(source, "any/path");
            res.Count().Should().Be(1);
            ClassInfo script = res.First();
            script.Name.ShortName.Should().Be("path");

            script.Fields.Count().Should().Be(1);
            script.Fields.First().Name.ShortName.Should().Be("x,y");
            script.Fields.First().SourceCode.Text.Should().Be("{ a: { x, y } } = obj");
        }

        [Fact]
        public void ParseDeconstructedArray()
        {
            string source = @"
                const [x, ...y] = obj;
            ".TrimIndent();
            IEnumerable<ClassInfo> res = AntlrParseJavaScript.OuterClassInfosFromSource(source, "any/path");
            res.Count().Should().Be(1);
            ClassInfo script = res.First();
            script.Name.ShortName.Should().Be("path");

            script.Fields.Count().Should().Be(1);
            script.Fields.First().Name.ShortName.Should().Be("x,y");
            script.Fields.First().SourceCode.Text.Should().Be("[x, ...y] = obj");
        }

        [Fact]
        public void ParseDeconstructedArrayAndObject()
        {
            string source = @"
                const { a: [x, ...y]} = obj;
            ".TrimIndent();
            IEnumerable<ClassInfo> res = AntlrParseJavaScript.OuterClassInfosFromSource(source, "any/path");
            res.Count().Should().Be(1);
            ClassInfo script = res.First();
            script.Name.ShortName.Should().Be("path");

            script.Fields.Count().Should().Be(1);
            script.Fields.First().Name.ShortName.Should().Be("x,y");
            script.Fields.First().SourceCode.Text.Should().Be("{ a: [x, ...y]} = obj");
        }

        [Fact]
        public void ParseDeconstructedArrayAndObject2()
        {
            string source = @"
                const { a: [{x}, ...y], z} = obj;
            ".TrimIndent();
            IEnumerable<ClassInfo> res = AntlrParseJavaScript.OuterClassInfosFromSource(source, "any/path");
            res.Count().Should().Be(1);
            ClassInfo script = res.First();
            script.Name.ShortName.Should().Be("path");

            script.Fields.Count().Should().Be(1);
            script.Fields.First().Name.ShortName.Should().Be("x,y,z");
            script.Fields.First().SourceCode.Text.Should().Be("{ a: [{x}, ...y], z} = obj");
        }

        [Fact]
        public void FirstClassHeader()
        {
            string source = @"
                requires('')
                /**comment*/
                class A {
                    f(x){return 10}
                }
            ".TrimIndent();
            IEnumerable<ClassInfo> res = AntlrParseJavaScript.OuterClassInfosFromSource(source, "any/path");
            res.Count().Should().Be(1);
            ClassInfo classInfo = res.First();

            classInfo.SourceCode.Text.Should().Be(@"
                /**comment*/
                class A {
            ".TrimIndent().Trim());
        }

        [Fact]
        public void SecondClassHeader()
        {
            string source = @"
                requires('')
                /**comment1*/
                class A {}

                /**comment2*/
                class B {}
            ".TrimIndent();
            IEnumerable<ClassInfo> res = AntlrParseJavaScript.OuterClassInfosFromSource(source, "any/path");

            ClassInfo classB = res.ToArray()[1];

            classB.SourceCode.Text.Should().Be(@"
                /**comment2*/
                class B {}
            ".TrimIndent().Trim());
        }

        [Fact]
        public void FakeClassHeader()
        {
            string source = @"
                requires('')
                /**comment1*/
                function f(){}
            ".TrimIndent();
            IEnumerable<ClassInfo> res = AntlrParseJavaScript.OuterClassInfosFromSource(source, "any/path");

            ClassInfo fakeClass = res.First();

            fakeClass.SourceCode.Text.Should().Be(@"
                requires('')
                /**comment1*/
            ".TrimIndent().Trim());
        }

        [Fact]
        public void InnerClassHeader()
        {
            string source = @"
                requires('')
                /**comment1*/
                function f(){}
                /**comment2*/
                class A {}
            ".TrimIndent();
            IEnumerable<ClassInfo> res = AntlrParseJavaScript.OuterClassInfosFromSource(source, "any/path");

            ClassInfo fakeClass = res.First();

            fakeClass.SourceCode.Text.Should().Be(@"
                requires('')
                /**comment1*/
            ".TrimIndent().Trim());

            fakeClass.InnerClasses.Single().SourceCode.Text.Should().Be(@"
                /**comment2*/
                class A {}
            ".TrimIndent().Trim());
        }

        [Fact]
        public void MultilineFunction()
        {
            string source = @"
                function f(){
                   return 10;
                }
            ".TrimIndent();
            FileDto res = AntlrParseJavaScript.Parse(source, "any/path");

            res.Classes[0].Methods[0].CodeRange.Should().Be(TestUtils.CodeRange(2, 1, 4, 2));
        }
    }
}