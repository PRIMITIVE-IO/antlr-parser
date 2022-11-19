using antlr_parser.Antlr4Impl;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
using Xunit;

namespace antlr_parser.tests;

public class IndexToLocationConverterTest
{
    [Fact]
    public void IdxToCodeLocationConversionTest()
    {
        string source = @"
                fun f(){ REMOVE }
                fun h() { 
                    REMOVE 
                }
            ".Unindent().Replace("\r\n", "\n");
    
        IndexToLocationConverter converter = new IndexToLocationConverter(source);
    
        //Act
        converter.IdxToLocation(0).Should().Be(new CodeLocation(1, 1)); //very first \n symbol
    
        converter.IdxToLocation(1).Should().Be(new CodeLocation(2, 1)); // beginning of the second line
        converter.IdxToLocation(8).Should().Be(new CodeLocation(2, 8)); // { on second line
        converter.IdxToLocation(17).Should().Be(new CodeLocation(2, 17)); // } on second line
    
        converter.IdxToLocation(27).Should().Be(new CodeLocation(3, 9)); // { on third line
        converter.IdxToLocation(42).Should().Be(new CodeLocation(5, 1)); // } on fifth line
    }
}