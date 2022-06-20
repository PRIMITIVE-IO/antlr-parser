using System;
using System.Collections.Generic;
using antlr_parser.Antlr4Impl;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using Xunit;

namespace antlr_parser.tests
{
    public class MethodBodyRemovalResultTest
    {
        [Fact]
        public void CreateCorrectIndicesForRemovedBlocks()
        {
            string source = @"
                fun f(){ REMOVE }
                fun g() { REMOVE }
            ".Unindent();

            List<Tuple<int, int>> blocksToRemove = new List<Tuple<int, int>>
            {
                new Tuple<int, int>(8, 17), // first '{ REMOVE }' block
                new Tuple<int, int>(26, 36) // second ' { REMOVE }' block including leading space
            };

            //Act
            MethodBodyRemovalResult result = MethodBodyRemovalResult.From(source, blocksToRemove);

            //Verify
            string expectedSource = @"
                fun f()
                fun g()
            ".Unindent();
            result.ShortenedSource.Should().Be(expectedSource);
        }

        [Fact]
        public void IgnoreNestedBlocks()
        {
            string source = @"
                fun f(){ fun h() {} }
            ".Unindent();

            List<Tuple<int, int>> blocksToRemove = new List<Tuple<int, int>>
            {
                new Tuple<int, int>(8, 21), // outer fun f(){ fun h() {} }
                new Tuple<int, int>(18, 19) // inner fun h() {}
            };

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

            List<Tuple<int, int>> blocksToRemove = new List<Tuple<int, int>>
            {
                new Tuple<int, int>(8, 17), // first '{ REMOVE }' block
                new Tuple<int, int>(26, 36), // second ' { REMOVE }' block including leading space
                new Tuple<int, int>(45, 55)
            };

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


            List<Tuple<int, int>> blocksToRemove1 = new List<Tuple<int, int>>
            {
                block(original, '(', ')')
            };

            var rem = MethodBodyRemovalResult.From(original, blocksToRemove1);

            rem.ShortenedSource.Should().Be(@"
                textBefore*[
                    this should be removed last
                ]
                &textAfter
            ".TrimIndent2());
            List<Tuple<int, int>> blocksToRemove2 = new List<Tuple<int, int>>
            {
                block(rem.ShortenedSource, '[', ']')
            };

            MethodBodyRemovalResult methodBodyRemovalResult2 = rem.RemoveFromShortened(blocksToRemove2);

            methodBodyRemovalResult2.ShortenedSource.Should().Be(@"
                textBefore*
                &textAfter
            ".TrimIndent2());

            var (from, to) = block(methodBodyRemovalResult2.ShortenedSource, '*', '&');

            var originalFrom = methodBodyRemovalResult2.RestoreIdx(from);
            var originalTo = methodBodyRemovalResult2.RestoreIdx(to);

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
}