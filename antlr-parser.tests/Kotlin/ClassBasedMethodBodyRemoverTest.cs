using System;
using System.Collections.Generic;
using antlr_parser.Antlr4Impl;
using antlr_parser.Antlr4Impl.Kotlin;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using Xunit;

namespace antlr_parser.tests.Kotlin;

public class ClassBasedMethodBodyRemoverTest
{
    [Fact]
    public void RemoveAllCurliesExceptClassOnes()
    {
        string source = @"
                { REMOVE }
                class {
                    fun f(){ REMOVE }
                    fun g() { REMOVE }
                    class {
                        fun h() { REMOVE }
                    }
                }
            ".Unindent();
        //Act
        List<Tuple<int, int>> blocksToRemove = ClassBasedMethodBodyRemover.FindBlocksToRemove(source);

        //Verify
        string expected = @"

                class {
                    fun f()
                    fun g()
                    class {
                        fun h()
                    }
                }
        ".Unindent();
        
        MethodBodyRemovalResult.From(source, blocksToRemove).ShortenedSource.Should().Be(expected);
    }

    [Fact]
    public void OnlyFullWord()
    {
        string source = @"
                class {
                    classes { REMOVE }
                    myclass { REMOVE }
                }
            ".TrimIndent2();

        //Act
        List<Tuple<int, int>> blocksToRemove = ClassBasedMethodBodyRemover.FindBlocksToRemove(source);

        //Verify
        MethodBodyRemovalResult.From(source, blocksToRemove).ShortenedSource.Should().Be(@"
                class {
                    classes
                    myclass
                }
        ".TrimIndent2());
    }
    
    [Fact]
    public void RemoveAllCurliesExceptObjectOnes()
    {
        string source = @"
                { REMOVE }
                object X {
                    fun f(){ REMOVE }
                    fun g() { REMOVE }
                    class {
                        fun h() { REMOVE }
                    }
                }
            ".Unindent();
        //Act
        List<Tuple<int, int>> blocksToRemove = ClassBasedMethodBodyRemover.FindBlocksToRemove(source);

        //Verify
        MethodBodyRemovalResult.From(source, blocksToRemove).ShortenedSource.Should().Be(@"

                object X {
                    fun f()
                    fun g()
                    class {
                        fun h()
                    }
                }
        ".Unindent());
    }
    [Fact]
    public void RemoveAllCurliesExceptInterfaceOnes()
    {
        string source = @"
                { REMOVE }
                interface X {
                    fun f(){ REMOVE }
                    fun g() { REMOVE }
                    class {
                        fun h() { REMOVE }
                    }
                }
            ".Unindent();
        //Act
        List<Tuple<int, int>> blocksToRemove = ClassBasedMethodBodyRemover.FindBlocksToRemove(source);

        //Verify
        MethodBodyRemovalResult.From(source, blocksToRemove).ShortenedSource.Should().Be(@"

                interface X {
                    fun f()
                    fun g()
                    class {
                        fun h()
                    }
                }
        ".Unindent());
    }
}