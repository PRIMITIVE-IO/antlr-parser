using antlr_parser.Antlr4Impl.TypeScript;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
using Xunit;

namespace antlr_parser.tests.TypeScript;

public class AntlrParseTypeScriptTest
{
    [Fact]
    public void SmokeTest()
    {
        string source = @"
                namespace testNamespace {
                    class Employee {
                        public empCode: number;
                        empName: string;
                    
                        constructor(code: number, name: string) {
                                this.empName = name;
                                this.empCode = code;
                        }
                    
                        private getSalary() : number {
                            return 10000;
                        }
                    }
                } 
                ".TrimIndent2();

        FileDto res = AntlrParseTypeScript.Parse(
            source: source,
            filePath: "repo/path"
        );

        res.Classes.Count.Should().Be(1);
        res.Classes[0].Name.Should().Be("Employee");
        res.Classes[0].Fields.Count.Should().Be(2);
        res.Classes[0].CodeRange.Of(source).Should().Be(
            @"
                class Employee {
            ".TrimIndent2()
        );

        FieldDto empCodeField = res.Classes[0].Fields[0];
        empCodeField.Name.Should().Be("empCode");
        empCodeField.CodeRange.Of(source).Should().Be(
            @"
                public empCode: number;
            ".TrimIndent2()
        );
        empCodeField.AccFlag.Should().Be(AccessFlags.AccPublic);

        FieldDto empNameField = res.Classes[0].Fields[1];
        empNameField.Name.Should().Be("empName");
        empNameField.AccFlag.Should().Be(AccessFlags.None);

        res.Classes[0].Methods.Count.Should().Be(2);
        res.Classes[0].Methods[0].Name.Should().Be("constructor");
        res.Classes[0].Methods[0].CodeRange.Of(source).Should().Be(
            @"
                constructor(code: number, name: string) {
                                this.empName = name;
                                this.empCode = code;
                        }
            ".TrimIndent2()
        );

        res.Classes[0].Methods[1].Name.Should().Be("getSalary");
        res.Classes[0].Methods[1].CodeRange.Of(source).Should().Be(
            @"
                private getSalary() : number {
                            return 10000;
                        }
            ".TrimIndent2()
        );

        res.Classes[0].Methods[1].AccFlag.Should().Be(AccessFlags.AccPrivate);
    }

    [Fact]
    public void CreateFakeClasses()
    {
        FileDto res = AntlrParseTypeScript.Parse(
            source: @"
                    // Named function
                    function add(x, y) {
                      return x + y;
                    }
                     
                    // Anonymous function
                    let myAdd = function (x, y) {
                      return x + y;
                    };
                    class MyClass{}
                ",
            filePath: "repo/path"
        );
        res.Functions[0].Name.Should().Be("add");
        res.Fields.Should().BeEmpty();// myAdd method is treated as method
        res.Functions[1].Name.Should().Be("myAdd");
    }

    [Fact]
    public void EmptyFile()
    {
        string source = @"
            /// some comment
        ".TrimIndent2();
        
        FileDto res = AntlrParseTypeScript.Parse(
            source: source,
            filePath: "repo/path"
        );
        res.Should().NotBeNull();
    }
}