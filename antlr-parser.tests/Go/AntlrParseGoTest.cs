using antlr_parser.Antlr4Impl.Go;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
using Xunit;

namespace antlr_parser.tests.Go;

public class AntlrParseGoTest
{
    [Fact]
    public void ParseStruct()
    {
        string source = @"
            package main
            import ""fmt""
            type person struct {
                name string
                age  int
            }
        ".TrimIndent2();

        FileDto? res = AntlrParseGo.Parse(source, "some/path");

        ClassDto cls = res.Classes[0];
        cls.Name.Should().Be("person");
        cls.FullyQualifiedName.Should().Be("main.person");

        cls.CodeRange.Of(source).Should().Be("type person struct {");

        cls.Fields[0].Name.Should().Be("name");
        cls.Fields[0].CodeRange.Of(source).Should().Be("name string");
        cls.Fields[1].Name.Should().Be("age");
        cls.Fields[1].CodeRange.Of(source).Should().Be("age  int");
    }

    [Fact]
    public void ParseMethods()
    {
        string source = @"
            package main

            import (
	            ""fmt""
                ""math""
            )

            type Vertex struct {
                X, Y float64
            }

            func (v Vertex) Abs() float64 {
                return math.Sqrt(v.X*v.X + v.Y*v.Y)
            }

            func main() {
                v := Vertex{3, 4}
                fmt.Println(v.Abs())
            }
        ".TrimIndent2();

        FileDto? res = AntlrParseGo.Parse(source, "some/path");

        ClassDto fakeClass = res.Classes[0];
        fakeClass.Name.Should().Be("path");

        fakeClass.CodeRange.Of(source).Should().Be(@"
            package main

            import (
	            ""fmt""
                ""math""
            )
        ".TrimIndent2());

        MethodDto method = fakeClass.Methods[0];
        method.Name.Should().Be("Abs");

        MethodDto function = fakeClass.Methods[1];
        function.Name.Should().Be("main");

        ClassDto vertexClass = res.Classes[1];
        vertexClass.Name.Should().Be("Vertex");

        vertexClass.Fields[0].Name.Should().Be("X");
        vertexClass.Fields[0].CodeRange.Of(source).Should().Be("X, Y float64");
        vertexClass.Fields[1].Name.Should().Be("Y");
        vertexClass.Fields[1].CodeRange.Of(source).Should().Be("X, Y float64");
    }

    [Fact]
    public void ParseFunction()
    {
        string source = @"
            package main
            func f(x int, y double) float {
                
            }
        ".TrimIndent2();

        FileDto? res = AntlrParseGo.Parse(source, "some/path");

        MethodDto func = res.Classes[0].Methods[0];
        func.Name.Should().Be("f");
        func.Signature.Should().Be("main.path.f()");
        
        ArgumentDto arg1 = func.Arguments[0];
        arg1.Name.Should().Be("x");
        arg1.Type.Should().Be("int");
        
        ArgumentDto arg2 = func.Arguments[1];
        arg2.Name.Should().Be("y");
        arg2.Type.Should().Be("double");
        
        func.ReturnType.Should().Be("float");
        func.CodeRange.Of(source).Should().Be(@"
            func f(x int, y double) float {
                
            }
        ".TrimIndent2());
    }
}