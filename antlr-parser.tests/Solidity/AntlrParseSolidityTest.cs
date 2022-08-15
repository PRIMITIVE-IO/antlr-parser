using antlr_parser.Antlr4Impl.Solidity;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
using Xunit;

namespace antlr_parser.tests.Solidity;

public class AntlrParseSolidityTest
{
    [Fact]
    public void SmokeTest()
    {
        string source = @"
                pragma solidity >=0.4.0 <0.6.0;
                contract SimpleStorage {
                   uint storedData;
                   function set(uint x) public {
                      storedData = x;
                   }
                   function get() public view returns (uint) {
                      return storedData;
                   }
                }
            ".Unindent();
        FileDto fileDto = AntlrParseSolidity.Parse(source, "some/path");

        fileDto.Classes.Should().HaveCount(1);
        ClassDto classDto = fileDto.Classes[0];
        classDto.Name.Should().Be("SimpleStorage");

        classDto.Fields.Should().HaveCount(1);
        FieldDto fieldDto = classDto.Fields[0];
        fieldDto.Name.Should().Be("storedData");

        classDto.Methods.Should().HaveCount(2);
        MethodDto methodDto = classDto.Methods[0];
        methodDto.Name.Should().Be("set");
    }

    [Fact]
    public void ParseConstructor()
    {
        string source = @"
                pragma solidity ^0.5.0;
                contract SolidityTest {
                   constructor() public{
                   }
                   function getResult() public view returns(uint){
                      uint a = 1;
                      uint b = 2;
                      uint result = a + b;
                      return result;
                   }
                }".Unindent();
        FileDto fileDto = AntlrParseSolidity.Parse(source, "some/path");

        fileDto.Classes[0].Methods.Should().HaveCount(2);
        MethodDto constructor = fileDto.Classes[0].Methods[0];
        constructor.Name.Should().Be("constructor");
        constructor.CodeRange.Should().Be(TestUtils.CodeRange(3, 24, 5, 4));
        constructor.AccFlag.Should().Be(AccessFlags.AccPublic);
    }

    [Fact]
    public void ContractHeader()
    {
        string source = @"
                pragma solidity >=0.4.0 <0.6.0;
                /*comment*/
                contract SimpleStorage {
                }
            ".TrimIndent2();
        FileDto fileDto = AntlrParseSolidity.Parse(source, "some/path");
        ClassDto classDto = fileDto.Classes[0];
        classDto.CodeRange.Of(source).Should().Be(@"
                pragma solidity >=0.4.0 <0.6.0;
                /*comment*/
                contract SimpleStorage {
            ".TrimIndent2());
    }

    [Fact]
    public void SecondContractHeader()
    {
        string source = @"
                pragma solidity >=0.4.0 <0.6.0;
                contract SimpleStorage1 {

                }
                /*comment2*/
                contract SimpleStorage2 {

                }
            ".TrimIndent2();
        FileDto fileDto = AntlrParseSolidity.Parse(source, "some/path");

        ClassDto classDto = fileDto.Classes[1];
        classDto.CodeRange.Of(source).Should().Be(@"
                |
                |/*comment2*/
                |contract SimpleStorage2 {
            ".TrimMargin());
    }

    [Fact]
    public void MethodShouldIncludeComment()
    {
        string source = @"
                pragma solidity ^0.5.0;
                contract SolidityTest {
                   /*comment*/
                   function getResult() public view returns(uint){
                      uint a = 1;
                      uint b = 2;
                      uint result = a + b;
                      return result;
                   }
                }
            ".TrimIndent2();
        FileDto fileDto = AntlrParseSolidity.Parse(source, "some/path");

        MethodDto methodDto = fileDto.Classes[0].Methods[0];
        methodDto.CodeRange.Of(source).Should().Be(@"
                   |
                   |   /*comment*/
                   |   function getResult() public view returns(uint){
                   |      uint a = 1;
                   |      uint b = 2;
                   |      uint result = a + b;
                   |      return result;
                   |   }
            ".TrimMargin());
    }
}