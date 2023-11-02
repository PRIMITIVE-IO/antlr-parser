using antlr_parser.Antlr4Impl;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
using Xunit;
using CodeRange = PrimitiveCodebaseElements.Primitive.dto.CodeRange;

namespace antlr_parser.tests;

public class CodeRangeCalculatorTest
{
    [Fact]
    public void Test()
    {
        string source = @"
                |    <- 4 spaces there
                |4 spaces there ->    
            ".TrimMargin();

        CodeRangeCalculator calc = new CodeRangeCalculator(source);

        CodeRange firstLineCodeRange = CodeRange.Of(1, 1, 1, 21);
        CodeRange secondLineCodeRange = CodeRange.Of(2, 1, 2, 21);
        CodeRange wholeDocCodeRange = CodeRange.Of(1, 1, 2, 21);

        // just to be sure that original code extracted properly
        firstLineCodeRange.Of(source).Should().Be("    <- 4 spaces there");
        //secondLineCodeRange.Of(source).Should().Be("4 spaces there ->    ");
        //wholeDocCodeRange.Of(source).Should().Be(source.PlatformSpecific());

        // then
        calc.Trim(firstLineCodeRange).Of(source).Should().Be("<- 4 spaces there");
        //calc.Trim(secondLineCodeRange).Of(source).Should().Be("4 spaces there ->");
        //calc.Trim(wholeDocCodeRange).Of(source).Should()
          //  .Be("<- 4 spaces there\n4 spaces there ->".PlatformSpecific());
    }

    [Fact]
    public void TestNotEntirelyEmptyLine()
    {
        string source = @"
                |4 spaces there ->    
                |expected text
                |    <- 4 spaces there 
            ".TrimMargin();

        CodeRangeCalculator calc = new CodeRangeCalculator(source);

        CodeRange expectedTextWithWhitespaces = CodeRange.Of(1, 18, 3, 4);

        // just to be sure that original code extracted properly (contains spaces)
        /*
        expectedTextWithWhitespaces.Of(source).Should().Be(@"
                |    
                |expected text
                |    
            ".TrimMargin());
*/
        // then
        //calc.Trim(expectedTextWithWhitespaces).Of(source).Should().Be("expected text");
    }

    [Fact]
    public void SkipEmptyLines()
    {
        string source = @"
            |
            |   some text
            |    
        ".TrimMargin();

        CodeRangeCalculator calc = new CodeRangeCalculator(source);

        CodeRange wholeDocCodeRrange = CodeRange.Of(1, 1, 3, 4);

        // just to be sure that original code extracted properly
        //wholeDocCodeRrange.Of(source).Should().Be(source.PlatformSpecific());

        // then
        //calc.Trim(wholeDocCodeRrange).Of(source).Should().Be("some text");
    }

    [Fact]
    public void PreviousNonWhitespaceSameLine()
    {
        string source = @"
            |    class my class
            |    {
        ".TrimMargin();

        CodeRangeCalculator calc = new CodeRangeCalculator(source);

        CodeRange expectedRange = CodeRange.Of(1, 1, 1, 12);

        // just to be sure that original code extracted properly
        expectedRange.Of(source).Should().Be("    class my");

        CodeLocation codeLocation = new CodeLocation(1, 13);
        new CodeRange(new CodeLocation(1, 1), codeLocation).Of(source).Should().Be("    class my ");

        // then
        calc.PreviousNonWhitespace(codeLocation).Should().Be(expectedRange.End);
    }

    [Fact]
    public void FirstSymbol()
    {
        string source = @"
            |
            |X
        ".TrimMargin();

        CodeRangeCalculator calc = new CodeRangeCalculator(source);

        CodeLocation codeLocation = new CodeLocation(1, 1);

        // then
        calc.PreviousNonWhitespace(codeLocation).Should().Be(null);
    }

    [Fact]
    public void WeirdCase()
    {
        string source = @"
                int f(int a, int b)
                {
                #if 1
                    for(i = 0; i<0; i++){
                #else
                    for(j = 0; j<0; j++){
                #endif
                    };
                }
                int g(int a, int b)  
                {

                }
            ".TrimIndent2();

        CodeRangeCalculator calc = new CodeRangeCalculator(source);

        CodeLocation codeLocation = new CodeLocation(1, 1);

        // then

        string expected = @"
                int f(int a, int b)
                {
                #if 1
                    for(i = 0; i<0; i++){
                #else
                    for(j = 0; j<0; j++){
                #endif
                    };
                }
            ".TrimIndent2();
            
        //calc.Trim(CodeRange.Of(1, 1, 9, 2)).Of(source).Should().Be(expected);
    }
}