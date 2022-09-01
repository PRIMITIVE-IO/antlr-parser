using System.Linq;
using antlr_parser.Antlr4Impl.CSharp;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
using Xunit;
using static antlr_parser.tests.TestUtils;

namespace antlr_parser.tests.CSharp;

public class AntlrParseCSharpTest
{
    [Fact]
    public void SmokeTest()
    {
        string source = @"
                namespace X 
                {
                    public class MyClass
                    {
                        public int MyField;
                        private void MyMethod()
                        {
                        
                        }
                    }
                }
            ";
        var fileDto = AntlrParseCSharp.Parse(source, "some/path");

        fileDto.Classes.Should().HaveCount(1);
        var classDto = fileDto.Classes[0];
        classDto.PackageName.Should().Be("X");
        classDto.FullyQualifiedName.Should().Be("X.MyClass");
        classDto.Name.Should().Be("MyClass");
        classDto.Fields.Should().HaveCount(1);
        classDto.Fields[0].Name.Should().Be("MyField");
        classDto.Methods.Should().HaveCount(1);
        classDto.Methods[0].Name.Should().Be("MyMethod");
    }

    [Fact]
    public void FirstClassHeaderStartFromFileBegin()
    {
        string source = @"
                using Y;
                namespace X 
                {
                    ///comment
                    public class MyClass
                    {

                    }
                }
            ".TrimIndent2();
        FileDto fileDto = AntlrParseCSharp.Parse(source, "some/path");

        ClassDto classDto = fileDto.Classes.Single();

        classDto.CodeRange.Of(source).Should().Be(@"
                using Y;
                namespace X 
                {
                    ///comment
                    public class MyClass
            ".TrimIndent2());
    }


    [Fact]
    public void SecondClassHeaderStartFromFileBegin()
    {
        string source = @"
                using Y;
                namespace X 
                {
                    ///comment
                    public class MyClass
                    {

                    }
                    ///comment
                    public class SecondClass
                    {
                    }
                }
            ".TrimIndent2();
        var fileDto = AntlrParseCSharp.Parse(source, "some/path");

        ClassDto classDto = fileDto.Classes[1];

        classDto.CodeRange.Of(source).Should().Be(@"
                    |///comment
                    |    public class SecondClass
            ".TrimMargin());
    }

    [Fact]
    public void InnerClasses()
    {
        string source = @"
                using Y;
                namespace X 
                {
                    ///comment
                    public class MyClass
                    {
                        ///comment
                        public class SecondClass
                        {
                        }
                    }
                }
            ".TrimIndent2();
        var fileDto = AntlrParseCSharp.Parse(source, "some/path");

        ClassDto topLevelClass = fileDto.Classes[0];
        topLevelClass.FullyQualifiedName.Should().Be("X.MyClass");

        ClassDto nestedClass = fileDto.Classes[1];

        nestedClass.ParentClassFqn.Should().Be("X.MyClass");
        nestedClass.FullyQualifiedName.Should().Be("X.MyClass$SecondClass");

        nestedClass.CodeRange.Of(source).Should().Be(@"
                    |///comment
                    |        public class SecondClass
            ".TrimMargin());
    }

    [Fact]
    public void MethodHeaderIncludesComment()
    {
        string source = @"
                namespace X 
                {
                    ///comment
                    public class MyClass
                    {
                        ///comment
                        public int MyMethod1()  
                        {
                        }

                        ///comment
                        public int MyMethod2()
                        {
                        }
                    }
                }
            ".TrimIndent2();
        FileDto fileDto = AntlrParseCSharp.Parse(source, "some/path");

        ClassDto classDto = fileDto.Classes[0];
        MethodDto method1 = classDto.Methods[0];
        method1.CodeRange.Of(source).Should().Be(@"
                |///comment
                |        public int MyMethod1()  
                |        {
                |        }
            ".TrimMargin());

        MethodDto method2 = classDto.Methods[1];
        method2.CodeRange.Of(source).Should().Be(@"
                |///comment
                |        public int MyMethod2()
                |        {
                |        }
            ".TrimMargin());
    }

    [Fact]
    public void FieldHeaderIncludesComment()
    {
        string source = @"
                namespace X 
                {
                    ///comment
                    public class MyClass
                    {
                        ///comment
                        public int MyField1;

                        ///comment
                        public int MyField2;
                    }
                }
            ".TrimIndent2();
        FileDto fileDto = AntlrParseCSharp.Parse(source, "some/path");

        ClassDto classDto = fileDto.Classes[0];
        FieldDto field1 = classDto.Fields[0];
        field1.CodeRange.Of(source).Should().Be(@"
                |///comment
                |        public int MyField1;
            ".TrimMargin());
        FieldDto field2 = classDto.Fields[1];
        field2.CodeRange.Of(source).Should().Be(@"
                |///comment
                |        public int MyField2;
            ".TrimMargin());
    }

    [Fact]
    public void ParseInterface()
    {
        string source = @"
                namespace X 
                {
                    ///comment
                    public interface IMyClass
                    {
                        ///comment
                        public int MyMethod1();

                        ///comment
                        public int MyMethod2();
                    }
                }
            ".TrimIndent2();
        var fileDto = AntlrParseCSharp.Parse(source, "some/path");

        var classDto = fileDto.Classes[0];
        classDto.Name.Should().Be("IMyClass");

        var method1 = classDto.Methods[0];
        method1.Name.Should().Be("MyMethod1");
        method1.CodeRange.Of(source).Should().Be(@"
                |///comment
                |        public int MyMethod1();
            ".TrimMargin());

        var method2 = classDto.Methods[1];
        method2.Name.Should().Be("MyMethod2");
        method2.CodeRange.Of(source).Should().Be(@"
                |///comment
                |        public int MyMethod2();
            ".TrimMargin());
    }

    [Fact]
    public void ParseEnum()
    {
        string source = @"
                namespace X 
                {
                    ///comment
                    public enum MyEnum
                    {
                        A=1,
                        B=2,
                        C=3
                    }
                }
            ".TrimIndent2();
        var fileDto = AntlrParseCSharp.Parse(source, "some/path");
        var classDto = fileDto.Classes[0];
        classDto.Name.Should().Be("MyEnum");
        classDto.CodeRange.Of(source).Should().Be(@"
                namespace X 
                {
                    ///comment
                    public enum MyEnum
                    {
                        A=1,
                        B=2,
                        C=3
                    }
            ".TrimIndent2());

        classDto.CodeRange.Should().Be(CodeRange(1, 1, 9, 5));
    }


    [Fact]
    public void ParseStruct()
    {
        string source = @"
                /// <summary>
                /// comment
                /// </summary>
                [Serializable]
                public struct MyStruct
                {

                    /// <summary>
                    /// comment
                    /// </summary>
                    public string MyField1;
                    /// <summary>
                    /// comment
                    /// </summary>
                    public string MyField2;

                    public MyStruct(
                        string myField1,
                        string myField2)
                    {
                        MyField1 = myField1;
                        MyField2 = myField2;
                    }

                    ///Comment
                    public void f(){
                    }
                }
            ".TrimIndent2();
        var fileDto = AntlrParseCSharp.Parse(source, "some/path");

        fileDto.Classes.Should().HaveCount(1);
        fileDto.Classes[0].Fields.Should().HaveCount(2);

        FieldDto field1 = fileDto.Classes[0].Fields[0];
        field1.Name.Should().Be("MyField1");
        field1.AccFlag.Should().Be(AccessFlags.AccPublic);
        field1.CodeRange.Of(source).Should().Be(@"
                |/// <summary>
                |    /// comment
                |    /// </summary>
                |    public string MyField1;
            ".TrimMargin());

        FieldDto field2 = fileDto.Classes[0].Fields[1];
        field2.Name.Should().Be("MyField2");
        field2.AccFlag.Should().Be(AccessFlags.AccPublic);
        field2.CodeRange.Of(source).Should().Be(@"
                |/// <summary>
                |    /// comment
                |    /// </summary>
                |    public string MyField2;
            ".TrimMargin());

        MethodDto method = fileDto.Classes[0].Methods[0];

        method.Name.Should().Be("f");
        method.CodeRange.Of(source).Should().Be(@"
                |///Comment
                |    public void f(){
                |    }
            ".TrimMargin());
    }

    [Fact]
    public void PropertiesWithArrow()
    {
        string source = @"
                class C {
                    internal string Path
                    {
                        get => Type == RequestType.http ? HttpRequest?.Path : RequestPath;
                        set => HttpRequest.Path = value;
                    }
                }
            ".TrimIndent2();

        var fileDto = AntlrParseCSharp.Parse(source, "some/path");

        var field = fileDto.Classes[0].Fields[0];

        field.Name.Should().Be("Path");
        field.CodeRange.Of(source).Should().Be(@"
               |internal string Path
               |    {
               |        get => Type == RequestType.http ? HttpRequest?.Path : RequestPath;
               |        set => HttpRequest.Path = value;
               |    }
            ".TrimMargin());
    }

    [Fact]
    public void Directives()
    {
        string source = Resource("TestCsharp.txt");
        var fileDto = AntlrParseCSharp.Parse(source, "some/path");

        fileDto.Classes.Single().Methods.Single(x => x.Name == "StaticReset")
            .CodeRange.Of(source).Should().Be(@"
                    |#if SUPPORTED_UNITY
                    |
                    |        #if UNITY_2019_4_OR_NEWER
                    |
                    |        /// <summary>
                    |        /// Resets statics for Domain Reload
                    |        /// </summary>
                    |        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
                    |        static void StaticReset()
                    |        {
                    |            AppQuits = false;
                    |        }
                ".TrimMargin());
    }

    [Fact]
    public void Properties()
    {
        string source = @"
                using System;
                using UnityEngine.InputSystem.Composites;
                using UnityEngine.InputSystem.LowLevel;

                namespace UnityEngine.InputSystem.Layouts
                {

                    /// public class MyComposite : InputBindingComposite&lt;float&gt;
                    /// {
                    ///     //...
                    /// }
                    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
                    public sealed class InputControlAttribute : PropertyAttribute
                    {
                        /// <summary>
                        public string alias { get; set; }
                    }
                }
            ".TrimIndent2();

        var fileDto = AntlrParseCSharp.Parse(source, "some/path");

        var field = fileDto.Classes.Single().Fields.Single();
        field.Name.Should().Be("alias");
        field.CodeRange.Of(source).Should().Be(@"
                |/// <summary>
                |        public string alias { get; set; }
            ".TrimMargin());
    }

    [Fact]
    public void EventAsField()
    {
        string source = @"
                public sealed unsafe class InputEventTrace 
                {

                    /// comment
                    public event Action<InputEventPtr> onEvent
                    {
                        add
                        {
                            if (!m_EventListeners.Contains(value))
                                m_EventListeners.Append(value);
                        }
                        remove => m_EventListeners.Remove(value);
                    }

                    public bool GetNextEvent(ref InputEventPtr current)
                    {

                    }
                }
            ".TrimIndent2();
        var fileDto = AntlrParseCSharp.Parse(source, "some/path");

        FieldDto fieldDto = fileDto.Classes.Single().Fields.Single();
        fieldDto.Name.Should().Be("onEvent");
            
        fieldDto.CodeRange.Of(source).Should().Be(@"
                |/// comment
                |    public event Action<InputEventPtr> onEvent
                |    {
                |        add
                |        {
                |            if (!m_EventListeners.Contains(value))
                |                m_EventListeners.Append(value);
                |        }
                |        remove => m_EventListeners.Remove(value);
                |    }
            ".TrimMargin());
            
        fieldDto.AccFlag.Should().Be(AccessFlags.AccPublic);
    }
}