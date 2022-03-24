using System;
using antlr_parser.Antlr4Impl;
using antlr_parser.Antlr4Impl.Java;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
using Xunit;
using CodeRange = PrimitiveCodebaseElements.Primitive.dto.CodeRange;

namespace antlr_parser.tests.Java
{
    public class AntlrParseJavaTest
    {
        [Fact]
        public void SmokeTest()
        {
            string source = @"
                package MyPackage;

                // My class comment
                public class MyClass {
                    //field comment
                    public String myField;
                    //constructor comment
                    public MyClass(){
                    }
                    //method comment
                    public String myMethod(){
                        return """";
                    }               
                }
            ".TrimIndent();

            FileDto fileDto = AntlrParseJava.Parse(source, "some/path");

            fileDto.Classes.Should().HaveCount(1);
            ClassDto myClass = fileDto.Classes[0];
            myClass.Name.Should().Be("MyClass");
            myClass.Modifier.Should().Be(AccessFlags.AccPublic);
            myClass.Fields.Should().HaveCount(1);
            myClass.Header.Should().Be(@"package MyPackage;

                // My class comment
                public class MyClass {".TrimIndent());
            myClass.CodeRange.Should().Be(TestUtils.CodeRange(1, 1, 5, 22));

            FieldDto myField = myClass.Fields[0];
            myField.Name.Should().Be("myField");
            myField.AccFlag.Should().Be(AccessFlags.AccPublic);
            myField.SourceCode.Should().Be(@"//field comment
                    public String myField;".TrimIndent());
            myField.CodeRange.Should().Be(TestUtils.CodeRange(5, 23, 7, 26));

            myClass.Methods.Should().HaveCount(2);
            MethodDto constructor = myClass.Methods[0];
            constructor.Name.Should().Be("MyClass");
            constructor.AccFlag.Should().Be(AccessFlags.AccPublic);
            constructor.SourceCode.Should().Be(@"//constructor comment
                    public MyClass(){
                    }".TrimIndent());
            constructor.CodeRange.Should().Be(TestUtils.CodeRange(7, 27, 10, 5));

            MethodDto myMethod = myClass.Methods[1];
            myMethod.Name.Should().Be("myMethod");
            myMethod.AccFlag.Should().Be(AccessFlags.AccPublic);
            myMethod.SourceCode.Should().Be(@"//method comment
                    public String myMethod(){
                        return """";
                    }".TrimIndent());
            myMethod.CodeRange.Should().Be(TestUtils.CodeRange(10, 6, 14, 5));
        }

        [Fact]
        public void ParseEnum()
        {
            string source = @"
                package MyPackage;

                // enum comment
                public enum MyEnum {
                    ONE,
                    TWO,
                    THREE;
                    //field comment
                    public String myField;
                    //constructor comment
                    public MyEnum(){
                    }
                    //method comment
                    public String myMethod(){
                        return """";
                    }               
                }
            ".TrimIndent();

            FileDto fileDto = AntlrParseJava.Parse(source, "some/path");

            fileDto.Classes.Should().HaveCount(1);
            ClassDto myEnum = fileDto.Classes[0];
            myEnum.Header.Should().Be(@"package MyPackage;

                // enum comment
                public enum MyEnum {
                    ONE,
                    TWO,
                    THREE".TrimIndent());
            myEnum.CodeRange.Should().Be(TestUtils.CodeRange(1, 1, 8, 9));

            myEnum.Fields.Should().HaveCount(1);
            myEnum.Methods.Should().HaveCount(2); //method and constructor
            MethodDto constructor = myEnum.Methods[0];
            constructor.Name.Should().Be("MyEnum");
        }

        [Fact]
        public void ParseInnerClass()
        {
            string source = @"
                package MyPackage;

                // enum comment
                public class MyClass {
                    //field comment
                    public String myField;
                    //inner class comment
                    static class InnerClass {
                    }
                }
            ".TrimIndent();

            FileDto fileDto = AntlrParseJava.Parse(source, "some/path");

            fileDto.Classes.Should().HaveCount(2);
            ClassDto myClass = fileDto.Classes[0];
            myClass.Name.Should().Be("MyClass");
            ClassDto innerClass = fileDto.Classes[1];
            innerClass.ParentClassFqn.Should().Be("MyPackage.MyClass");
            innerClass.Name.Should().Be("InnerClass");
            innerClass.FullyQualifiedName.Should().Be("MyPackage.MyClass$InnerClass");
            innerClass.Header.Should().Be(@"//inner class comment
                    static class InnerClass {".TrimIndent());
            innerClass.CodeRange.Should().Be(TestUtils.CodeRange(7, 27, 9, 29));
        }

        [Fact]
        public void ParseInterface()
        {
            string source = @"
                package MyPackage;

                //  comment
                public interface MyInterface {
                    //method comment
                    public void myMethod();
                }
            ".TrimIndent();

            FileDto fileDto = AntlrParseJava.Parse(source, "some/path");
            fileDto.Classes.Should().HaveCount(1);
            ClassDto myInterface = fileDto.Classes[0];
            myInterface.Name.Should().Be("MyInterface");
            myInterface.Modifier.Should().Be(AccessFlags.AccPublic);
            myInterface.Methods.Should().HaveCount(1);
            
            MethodDto myMethod = myInterface.Methods[0];
            myMethod.Name.Should().Be("myMethod");
            myMethod.AccFlag.Should().Be(AccessFlags.AccPublic);
            myMethod.SourceCode.Should().Be(@"//method comment
                    public void myMethod();".TrimIndent());
            myMethod.CodeRange.Should().Be(TestUtils.CodeRange(5, 31, 7, 27));
            
            myInterface.Header.Should().Be(@"package MyPackage;

                //  comment
                public interface MyInterface {".TrimIndent());
            myInterface.CodeRange.Should().Be(TestUtils.CodeRange(1, 1, 5, 30));
        }
    }
}