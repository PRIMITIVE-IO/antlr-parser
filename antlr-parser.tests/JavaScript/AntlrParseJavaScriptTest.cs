using System.Linq;
using antlr_parser.Antlr4Impl.JavaScript;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
using Xunit;

namespace antlr_parser.tests.JavaScript;

public class AntlrParseJavaScriptTest
{
    [Fact]
    public void TestJs()
    {
        string source = @"
                class C{
                    f(x){ return 10}
                }
            ".Unindent();
        FileDto res = AntlrParseJavaScript.Parse(source, "any/path");
        res.Classes.Should().HaveCount(1);

        ClassDto klass = res.Classes.First();
        klass.Name.Should().Be("C");
        klass.Methods.Count().Should().Be(1);
        MethodDto method = klass.Methods.First();
        method.Name.Should().Be("f");
    }

    [Fact]
    public void CanParseMethodsWithoutBodies()
    {
        string source = @"
                class C{
                    f(x)
                }
            ".Unindent();
        FileDto res = AntlrParseJavaScript.Parse(source, "any/path");
        res.Classes.Count().Should().Be(1);

        ClassDto klass = res.Classes.First();
        klass.Name.Should().Be("C");
        klass.Methods.Count().Should().Be(1);
        MethodDto method = klass.Methods.First();
        method.Name.Should().Be("f");
    }

    [Fact]
    public void ParseMethods()
    {
        string source = @"
                function f(x){ return 10; }
                class C{
                    g(x) { return 20; }
                }
            ".TrimIndent2();
        FileDto res = AntlrParseJavaScript.Parse(source, "any/path");
        res.Classes.Should().HaveCount(2);
        ClassDto script = res.Classes[0];
        script.Name.Should().Be("path");

        script.Methods.Count().Should().Be(1);
        script.Methods.First().Name.Should().Be("f");
        script.Methods.First().CodeRange.Of(source).Should().Be("function f(x){ return 10; }");
        ClassDto klass = res.Classes[1];
        klass.Name.Should().Be("C");
        klass.Methods.Count().Should().Be(1);
        MethodDto method = klass.Methods.First();
        method.Name.Should().Be("g");
        method.CodeRange.Of(source).Should().Be("g(x) { return 20; }");
    }

    [Fact]
    public void ParseFields()
    {
        string source = @"
                var x = 10;
            ".TrimIndent2();
        FileDto res = AntlrParseJavaScript.Parse(source, "any/path");
        res.Classes.Count().Should().Be(1);
        ClassDto script = res.Classes.First();
        script.Name.Should().Be("path");

        script.Fields.Count().Should().Be(1);
        script.Fields.First().Name.Should().Be("x");
        script.Fields.First().CodeRange.Of(source).Should().Be("x = 10");
    }

    [Fact]
    public void ParseDeconstructedFields()
    {
        string source = @"
                const { y: { x } } = obj;
            ".Unindent();
        FileDto res = AntlrParseJavaScript.Parse(source, "any/path");
        res.Classes.Count().Should().Be(1);
        ClassDto script = res.Classes.First();
        script.Name.Should().Be("path");

        script.Fields.Count().Should().Be(1);
        script.Fields.First().Name.Should().Be("x");
        script.Fields.First().CodeRange.Of(source).Should().Be("{ y: { x } } = obj");
    }

    [Fact]
    public void ParseDeconstructedFieldsAsSingleFieldInfo()
    {
        string source = @"
                const { a: { x, y } } = obj;
            ".Unindent();
        FileDto res = AntlrParseJavaScript.Parse(source, "any/path");
        res.Classes.Count().Should().Be(1);
        ClassDto script = res.Classes.First();
        script.Name.Should().Be("path");

        script.Fields.Count().Should().Be(1);
        script.Fields.First().Name.Should().Be("x,y");
        script.Fields.First().CodeRange.Of(source).Should().Be("{ a: { x, y } } = obj");
    }

    [Fact]
    public void ParseDeconstructedArray()
    {
        string source = @"
                const [x, ...y] = obj;
            ".Unindent();
        FileDto res = AntlrParseJavaScript.Parse(source, "any/path");
        res.Classes.Count().Should().Be(1);
        ClassDto script = res.Classes.First();
        script.Name.Should().Be("path");

        script.Fields.Count().Should().Be(1);
        script.Fields.First().Name.Should().Be("x,y");
        script.Fields.First().CodeRange.Of(source).Should().Be("[x, ...y] = obj");
    }

    [Fact]
    public void ParseDeconstructedArrayAndObject()
    {
        string source = @"
                const { a: [x, ...y]} = obj;
            ".Unindent();
        FileDto res = AntlrParseJavaScript.Parse(source, "any/path");
        res.Classes.Count().Should().Be(1);
        ClassDto script = res.Classes.First();
        script.Name.Should().Be("path");

        script.Fields.Count().Should().Be(1);
        script.Fields.First().Name.Should().Be("x,y");
        script.Fields.First().CodeRange.Of(source).Should().Be("{ a: [x, ...y]} = obj");
    }

    [Fact]
    public void ParseDeconstructedArrayAndObject2()
    {
        string source = @"
                const { a: [{x}, ...y], z} = obj;
            ".Unindent();
        FileDto res = AntlrParseJavaScript.Parse(source, "any/path");
        res.Classes.Count().Should().Be(1);
        ClassDto script = res.Classes.First();
        script.Name.Should().Be("path");

        script.Fields.Count().Should().Be(1);
        script.Fields.First().Name.Should().Be("x,y,z");
        script.Fields.First().CodeRange.Of(source).Should().Be("{ a: [{x}, ...y], z} = obj");
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
            ".TrimIndent2();
        FileDto res = AntlrParseJavaScript.Parse(source, "any/path");
        res.Classes.Count().Should().Be(1);
        ClassDto classInfo = res.Classes.First();

        classInfo.CodeRange.Of(source).Should().Be(
            @"
                /**comment*/
                class A {
            ".TrimIndent2()
        );
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
            ".TrimIndent2();
        FileDto res = AntlrParseJavaScript.Parse(source, "any/path");

        ClassDto classB = res.Classes.ToArray()[1];

        classB.CodeRange.Of(source).Should().Be(
            @"
                /**comment2*/
                class B {}
            ".TrimIndent2()
        );
    }

    [Fact]
    public void FakeClassHeader()
    {
        string source = @"
                requires('')
                /**comment1*/
                function f(){}
            ".TrimIndent2();
        FileDto res = AntlrParseJavaScript.Parse(source, "any/path");

        ClassDto fakeClass = res.Classes.First();

        fakeClass.CodeRange.Of(source).Should().Be(
            @"
                requires('')
                /**comment1*/
            ".TrimIndent2()
        );
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
            ".TrimIndent2();
        FileDto res = AntlrParseJavaScript.Parse(source, "any/path");

        ClassDto fakeClass = res.Classes[0];

        fakeClass.CodeRange.Of(source).Should().Be(
            @"
                requires('')
                /**comment1*/
            ".TrimIndent2()
        );

        res.Classes[1].CodeRange.Of(source).Should().Be(
            @"
                /**comment2*/
                class A {}
            ".TrimIndent2()
        );
    }

    [Fact]
    public void MultilineFunction()
    {
        string source = @"
                function f(){
                   return 10;
                }
            ".Unindent();
        FileDto res = AntlrParseJavaScript.Parse(source, "any/path");

        res.Classes[0].Methods[0].CodeRange.Of(source).Should().Be(
            @"
                function f(){
                   return 10;
                }
            ".TrimIndent2()
        );
    }
    
    
    [Fact]
    public void FieldWithLambda()
    {
        string source = @"
            class c{
                handleEdit=e=>{e.preventDefault();this.setState({isEditing:true,});}
            }
            ".Unindent();
        FileDto res = AntlrParseJavaScript.Parse(source, "any/path");

        MethodDto methodDto = res.Classes[0].Methods[0];
        methodDto.Name.Should().Be("handleEdit");
        methodDto.CodeRange.Of(source).Should()
            .Be("handleEdit=e=>{e.preventDefault();this.setState({isEditing:true,});}");
    }
}