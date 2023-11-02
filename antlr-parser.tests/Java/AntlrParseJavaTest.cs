using antlr_parser.Antlr4Impl.Java;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
using Xunit;

namespace antlr_parser.tests.Java;

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
            ".TrimIndent2();

        FileDto fileDto = AntlrParseJava.Parse(source, "some/path");

        fileDto.Classes.Should().HaveCount(1);
        ClassDto myClass = fileDto.Classes[0];
        myClass.Name.Should().Be("MyClass");
        myClass.Modifier.Should().Be(AccessFlags.AccPublic);
        myClass.Fields.Should().HaveCount(1);
//        myClass.CodeRange.Of(source).Should().Be(@"
//                package MyPackage;
//
//                // My class comment
//                public class MyClass {
//            ".TrimIndent2());

        FieldDto myField = myClass.Fields[0];
        myField.Name.Should().Be("myField");
        myField.AccFlag.Should().Be(AccessFlags.AccPublic);
//        myField.CodeRange.Of(source).Should().Be(@"
//                |//field comment
//                |    public String myField;
//            ".TrimMargin());

        myClass.Methods.Should().HaveCount(2);
        MethodDto constructor = myClass.Methods[0];
        constructor.Name.Should().Be("MyClass");
        constructor.AccFlag.Should().Be(AccessFlags.AccPublic);
//        constructor.CodeRange.Of(source).Should().Be(@"
//                |//constructor comment
//                |    public MyClass(){
//                |    }
//            ".TrimMargin());

        MethodDto myMethod = myClass.Methods[1];
        myMethod.Name.Should().Be("myMethod");
        myMethod.AccFlag.Should().Be(AccessFlags.AccPublic);
//        myMethod.CodeRange.Of(source).Should().Be(@"
//                |//method comment
//                |    public String myMethod(){
//                |        return """";
//                |    }
//            ".TrimMargin());
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
            ".TrimIndent2();

        FileDto fileDto = AntlrParseJava.Parse(source, "some/path");

        fileDto.Classes.Should().HaveCount(1);
        ClassDto myEnum = fileDto.Classes[0];
//        myEnum.CodeRange.Of(source).Should().Be(@"
//                package MyPackage;
//
//                // enum comment
//                public enum MyEnum {
//                    ONE,
//                    TWO,
//                    THREE
//            ".TrimIndent2());

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
            ".TrimIndent2();

        FileDto fileDto = AntlrParseJava.Parse(source, "some/path");

        fileDto.Classes.Should().HaveCount(2);
        ClassDto myClass = fileDto.Classes[0];
        myClass.Name.Should().Be("MyClass");
        ClassDto innerClass = fileDto.Classes[1];
        //innerClass.ParentClassFqn.Should().Be("some/path:MyPackage.MyClass");
        innerClass.Name.Should().Be("InnerClass");
        innerClass.FullyQualifiedName.Should().Be("some/path:MyPackage.MyClass$InnerClass");
//        innerClass.CodeRange.Of(source).Should().Be(@"
//                |//inner class comment
//                |    static class InnerClass {
//            ".TrimMargin());
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
            ".TrimIndent2();

        FileDto fileDto = AntlrParseJava.Parse(source, "some/path");
        fileDto.Classes.Should().HaveCount(1);
        ClassDto myInterface = fileDto.Classes[0];
        myInterface.Name.Should().Be("MyInterface");
        myInterface.Modifier.Should().Be(AccessFlags.AccPublic);
        myInterface.Methods.Should().HaveCount(1);

        MethodDto myMethod = myInterface.Methods[0];
        myMethod.Name.Should().Be("myMethod");
        myMethod.AccFlag.Should().Be(AccessFlags.AccPublic);
//        myMethod.CodeRange.Of(source).Should().Be(@"
//                |//method comment
//                |    public void myMethod();
//            ".TrimMargin());

//        myInterface.CodeRange.Of(source).Should().Be(@"
//                package MyPackage;
//
//                //  comment
//                public interface MyInterface {
//            ".TrimIndent2());
    }

    [Fact]
    public void ParseMethodArguments()
    {
        string source = @"
                public class MyClass {

                    public String myMethod(String x, int y){
                        return """";
                    }               
                }
            ".TrimIndent2();

        FileDto fileDto = AntlrParseJava.Parse(source, "some/path");

        ClassDto myClass = fileDto.Classes[0];

        MethodDto myMethod = myClass.Methods[0];
        myMethod.Arguments.Should().HaveCount(2);
        myMethod.Arguments[0].Name.Should().Be("x");
        myMethod.Arguments[0].Type.Should().Be("String");
        myMethod.Arguments[1].Name.Should().Be("y");
        myMethod.Arguments[1].Type.Should().Be("int");
    }
}