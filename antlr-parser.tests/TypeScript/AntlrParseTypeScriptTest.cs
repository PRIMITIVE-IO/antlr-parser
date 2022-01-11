using antlr_parser.Antlr4Impl;
using antlr_parser.Antlr4Impl.TypeScript;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
using Xunit;

namespace antlr_parser.tests.TypeScript
{
    public class AntlrParseTypeScriptTest
    {
        [Fact]
        public void SmokeTest()
        {
            FileDto res = AntlrParseTypeScript.Parse(
                source: @"
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
                ".TrimIndent(),
                filePath: "repo/path"
            );

            res.Classes.Count.Should().Be(1);
            res.Classes[0].Name.Should().Be("Employee");
            res.Classes[0].Fields.Count.Should().Be(2);
            res.Classes[0].Header.Should().Be("class Employee {");

            FieldDto empCodeField = res.Classes[0].Fields[0];
            empCodeField.Name.Should().Be("empCode");
            empCodeField.SourceCode.Should().Be("public empCode: number;");
            empCodeField.AccFlag.Should().Be(AccessFlags.AccPublic);

            FieldDto empNameField = res.Classes[0].Fields[1];
            empNameField.Name.Should().Be("empName");
            empNameField.AccFlag.Should().Be(AccessFlags.None);

            res.Classes[0].Methods.Count.Should().Be(2);
            res.Classes[0].Methods[0].Name.Should().Be("constructor");
            res.Classes[0].Methods[0].CodeRange.Should().Be(TestUtils.CodeRange(7, 9, 10, 9));
            res.Classes[0].Methods[0].SourceCode.Should().Be(@"constructor(code: number, name: string) {
                                this.empName = name;
                                this.empCode = code;
                        }".TrimIndent());

            res.Classes[0].Methods[1].Name.Should().Be("getSalary");
            res.Classes[0].Methods[1].CodeRange.Should().Be(TestUtils.CodeRange(12, 9, 14, 9));
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
            res.Classes[0].Methods[0].Name.Should().Be("add");
            res.Classes[0].Name.Should().Be("path");
            res.Classes[0].Fields[0].Name.Should().Be("myAdd");
            res.Classes[0].ParentClassFqn.Should().Be(null);
            res.Classes[0].FullyQualifiedName.Should().Be("repo/path");
            res.Classes[1].Name.Should().Be("MyClass");
            res.Classes[1].ParentClassFqn.Should().Be("repo/path");
        }
    }
}