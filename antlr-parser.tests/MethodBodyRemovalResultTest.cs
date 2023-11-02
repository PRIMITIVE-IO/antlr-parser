using System;
using System.Collections.Generic;
using antlr_parser.Antlr4Impl;
using antlr_parser.Antlr4Impl.Kotlin;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using Xunit;

namespace antlr_parser.tests;

public class MethodBodyRemovalResultTest
{
    [Fact]
    public void CreateCorrectIndicesForRemovedBlocks()
    {
        string source = "\nfun f(){ REMOVE }\nfun g() { REMOVE }";

        List<Tuple<int, int>> blocksToRemove = new()
        {
            new Tuple<int, int>(8, 17), // first '{ REMOVE }' block
            new Tuple<int, int>(26, 36) // second ' { REMOVE }' block including leading space
        };

        //Act
        MethodBodyRemovalResult result = MethodBodyRemovalResult.From(source, blocksToRemove);

        //Verify
        string expectedSource = "\nfun f()\nfun g()";
        result.ShortenedSource.Should().Be(expectedSource);
    }
    
    [Fact]
    public void Smoke()
    {
        string source = "123456";

        List<Tuple<int, int>> blocksToRemove = new()
        {
            new (0, 0),
            new (2, 2),
            new (4, 4),
        };

        //Act
        MethodBodyRemovalResult result = MethodBodyRemovalResult.From(source, blocksToRemove);

        //Verify
        string expectedSource = "246";
        result.ShortenedSource.Should().Be(expectedSource);
    }
    
    
    [Fact]
    public void Smoke2()
    {
        string source = "1234567890";

        List<Tuple<int, int>> blocksToRemove = new()
        {
            new (0, 1),
            new (3, 4),
            new (6, 7),
        };

        //Act
        MethodBodyRemovalResult result = MethodBodyRemovalResult.From(source, blocksToRemove);

        //Verify
        string expectedSource = "3690";
        result.ShortenedSource.Should().Be(expectedSource);
    }

    [Fact]
    public void IgnoreNestedBlocks()
    {
        string source = @"
                fun f(){ fun h() {} }
            ".Unindent();
        var blocksToRemove = RegexBasedKotlinMethodBodyRemover.FindBlocksToRemove(source);

        //Act
        MethodBodyRemovalResult result = MethodBodyRemovalResult.From(source, blocksToRemove);

        //Verify
        string expectedSource = @"
                fun f()
            ".Unindent();
        result.ShortenedSource.Should().Be(expectedSource);
    }

    [Fact]
    public void RestoreOriginalCodeBlockBasedOnIndicesFromShortenedSource()
    {
        string source = @"
                fun f(){ REMOVE }
                fun g() { REMOVE }
                fun h() { REMOVE }
            ".Unindent();
        var blocksToRemove = RegexBasedKotlinMethodBodyRemover.FindBlocksToRemove(source);

        //Act
        MethodBodyRemovalResult result = MethodBodyRemovalResult.From(source, blocksToRemove);

        result.ShortenedSource.Should().Be(@"
                fun f()
                fun g()
                fun h()
            ".Unindent());
    }


    Tuple<int, int> block(string s, char c1, char c2)
    {
        return new Tuple<int, int>(s.IndexOf(c1), s.IndexOf(c2));
    }


    [Fact]
    public void RemoveFromShortened()
    {
        string original = @"
                textBefore*[
                    this should be removed last(
                        this should be removed first
                    )
                ]
                &textAfter
            ".TrimIndent2();


        List<Tuple<int, int>> blocksToRemove1 = new()
        {
            block(original, '(', ')')
        };

        MethodBodyRemovalResult rem = MethodBodyRemovalResult.From(original, blocksToRemove1);

        rem.ShortenedSource.Should().Be(@"
                textBefore*[
                    this should be removed last
                ]
                &textAfter
            ".TrimIndent2());
        List<Tuple<int, int>> blocksToRemove2 = new()
        {
            block(rem.ShortenedSource, '[', ']')
        };

        MethodBodyRemovalResult methodBodyRemovalResult2 = rem.RemoveFromShortened(blocksToRemove2);

        methodBodyRemovalResult2.ShortenedSource.Should().Be(@"
                textBefore*
                &textAfter
            ".TrimIndent2());

        (int from, int to) = block(methodBodyRemovalResult2.ShortenedSource, '*', '&');

        int originalFrom = methodBodyRemovalResult2.RestoreIdx(from);
        int originalTo = methodBodyRemovalResult2.RestoreIdx(to);

        original.Substring(Tuple.Create(originalFrom, originalTo)).Should().Be(@"
                *[
                    this should be removed last(
                        this should be removed first
                    )
                ]
                &
            ".TrimIndent2());
    }
}