using antlr_parser.Antlr4Impl;
using antlr_parser.Antlr4Impl.Solidity;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using Xunit;

namespace antlr_parser.tests.Solidity
{
    public class AntlrParseSolidityTest
    {
        [Fact]
        public void SmokeTest()
        {
            var source = @"
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
            ".TrimIndent();
            var fileDto = AntlrParseSolidity.Parse(source, "some/path");

            fileDto.Classes.Should().HaveCount(1);
            var classDto = fileDto.Classes[0];
            classDto.Name.Should().Be("SimpleStorage");

            classDto.Fields.Should().HaveCount(1);
            var fieldDto = classDto.Fields[0];
            fieldDto.Name.Should().Be("storedData");

            classDto.Methods.Should().HaveCount(2);
            var methodDto = classDto.Methods[0];
            methodDto.Name.Should().Be("set");
        }

        [Fact]
        public void ParseConstructor()
        {
            var source = @"
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
                }".TrimIndent();
            var fileDto = AntlrParseSolidity.Parse(source, "some/path");

            fileDto.Classes[0].Methods.Should().HaveCount(2);
            var constructor = fileDto.Classes[0].Methods[0];
            constructor.Name.Should().Be("constructor");
            constructor.CodeRange.Should().Be(TestUtils.CodeRange(3, 24, 5, 4));
            constructor.AccFlag.Should().Be(AccessFlags.AccPublic);
        }

        [Fact]
        public void ContractHeader()
        {
            var source = @"
                pragma solidity >=0.4.0 <0.6.0;
                /*comment*/
                contract SimpleStorage {
                }
            ".TrimIndent();
            var fileDto = AntlrParseSolidity.Parse(source, "some/path");
            var classDto = fileDto.Classes[0];
            classDto.Header.Should().Be(@"pragma solidity >=0.4.0 <0.6.0;
                /*comment*/
                contract SimpleStorage {".TrimIndent());
            classDto.CodeRange.Should().Be(TestUtils.CodeRange(1, 1, 4, 24));
        }

        [Fact]
        public void SecondContractHeader()
        {
            var source = @"
                pragma solidity >=0.4.0 <0.6.0;
                contract SimpleStorage1 {

                }
                /*comment2*/
                contract SimpleStorage2 {

                }
            ".TrimIndent();
            var fileDto = AntlrParseSolidity.Parse(source, "some/path");

            var classDto = fileDto.Classes[1];
            classDto.Header.Should().Be(@"
                /*comment2*/
                contract SimpleStorage2 {
            ".Trim().TrimIndent());
            classDto.CodeRange.Should().Be(TestUtils.CodeRange(5, 2, 7, 25));
        }

        [Fact]
        public void MethodShouldIncludeComment()
        {
            var source = @"
                pragma solidity ^0.5.0;
                contract SolidityTest {
                   /*comment*/
                   function getResult() public view returns(uint){
                      uint a = 1;
                      uint b = 2;
                      uint result = a + b;
                      return result;
                   }
                }".TrimIndent();
            var fileDto = AntlrParseSolidity.Parse(source, "some/path");

            var methodDto = fileDto.Classes[0].Methods[0];
            methodDto.SourceCode.Should().Be(@"/*comment*/
                   function getResult() public view returns(uint){
                      uint a = 1;
                      uint b = 2;
                      uint result = a + b;
                      return result;
                   }".TrimIndent());
            methodDto.CodeRange.Should().Be(TestUtils.CodeRange(3, 24, 10, 4));
        }
    }
}