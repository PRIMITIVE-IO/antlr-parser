using System;
using System.Collections.Generic;
using antlr_parser.Antlr4Impl.Kotlin;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using Xunit;

namespace antlr_parser.tests.Kotlin
{
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
            ".TrimIndent();
            //Act
            List<Tuple<int,int>> blocksToRemove = ClassBasedMethodBodyRemover.FindBlocksToRemove(source);
            
            //Verify
            blocksToRemove.Count.Should().Be(4);
            blocksToRemove[0].Should().Be(new Tuple<int, int>(1, 10));
            blocksToRemove[1].Should().Be(new Tuple<int, int>(31, 40));
            blocksToRemove[2].Should().Be(new Tuple<int, int>(53, 63));
            blocksToRemove[3].Should().Be(new Tuple<int, int>(92, 102));
        }
        
        [Fact]
        public void OnlyFullWord()
        {
            string source = @"
                class {
                    classes { REMOVE }
                    myclass { REMOVE }
                }
            ".TrimIndent();
            
            //Act
            List<Tuple<int,int>> blocksToRemove = ClassBasedMethodBodyRemover.FindBlocksToRemove(source);
            
            //Verify
            blocksToRemove.Count.Should().Be(2);
            blocksToRemove[0].Should().Be(new Tuple<int, int>(20, 30));
            blocksToRemove[1].Should().Be(new Tuple<int, int>(43, 53));
        }
    }
}
