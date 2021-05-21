using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using antlr_parser.Antlr4Impl;
using antlr_parser.Antlr4Impl.C;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using Xunit;

namespace antlr_parser.tests.C
{
    public class AntlrParseCTest
    {
        [Fact]
        void ParseFunctions()
        {
            string source = @"
                int addNumbers(int a, int b)  
                {
                    int result;
                    result = a+b;
                    return result;
                }"
                .TrimIndent();

            IEnumerable<ClassInfo> classInfos = AntlrParseC.OuterClassInfosFromSource(source, "file/path");

            MethodInfo method = classInfos.Single().Methods.Single();
            method.Name.ShortName.Should().Be("addNumbers");
            method.SourceCode.Text.Should().Be(source.TrimStart('\n'));
        }

        [Fact]
        void ParseStructsAsClasses()
        {
            string source = @"
                struct structureName 
                {
                    dataType member1;
                };".TrimIndent();
            IEnumerable<ClassInfo> classInfos =
                AntlrParseC.OuterClassInfosFromSource(source, "file/path").ToImmutableList();

            classInfos.Single().InnerClasses.Count().Should().Be(1);
            ClassInfo classInfo = classInfos.Single().InnerClasses.Single();
            classInfo.className.ShortName.Should().Be("structureName");
            classInfo.Fields.First().FieldName.ShortName.Should().Be("member1");
        }
        [Fact]
        void ParseStructArrayField()
        {
            string source = @"
                struct structureName 
                {
                    dataType member1[10];
                };".TrimIndent();
            IEnumerable<ClassInfo> classInfos =
                AntlrParseC.OuterClassInfosFromSource(source, "file/path").ToImmutableList();

            classInfos.Single().InnerClasses.Count().Should().Be(1);
            ClassInfo classInfo = classInfos.Single().InnerClasses.Single();
            classInfo.className.ShortName.Should().Be("structureName");
            classInfo.Fields.First().FieldName.ShortName.Should().Be("member1");
        }

        [Fact]
        void ParseNestedStructs()
        {
            string source = @"
                struct Employee  
                {     
                   struct Date  
                    {  
                      int dd;  
                    }doj;  
                };".TrimIndent();
            IEnumerable<ClassInfo> classInfos =
                AntlrParseC.OuterClassInfosFromSource(source, "file/path").ToImmutableList();

            classInfos.Single().InnerClasses.Count().Should().Be(1);
            ClassInfo employeeClass = classInfos.Single().InnerClasses.Single();
            employeeClass.className.ShortName.Should().Be("Employee");
            employeeClass.Fields.Single().FieldName.ShortName.Should().Be("doj");
            employeeClass.InnerClasses.Count().Should().Be(1);
            ClassInfo dateClass = employeeClass.InnerClasses.First();
            dateClass.className.ShortName.Should().Be("Date");
        }
    }
}
