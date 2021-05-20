using System;
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
        }

        [Fact]
        void ParseStructs()
        {
            string source = @"
                struct structureName 
                {
                    dataType member1;
                    char name[50];
                };".TrimIndent();
            IEnumerable<ClassInfo> classInfos =
                AntlrParseC.OuterClassInfosFromSource(source, "file/path").ToImmutableList();

            classInfos.Single().InnerClasses.Count().Should().Be(1);
            ClassInfo classInfo = classInfos.Single().InnerClasses.Single();
            classInfo.className.ShortName.Should().Be("structureName");
            classInfo.Fields.Select(it => it.FieldName.ShortName).ToImmutableList().Should()
                .Contain(new List<string> {"member1", "name"});
        }
    }
}