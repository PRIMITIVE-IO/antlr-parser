using System;
using System.Collections.Generic;
using antlr_parser.Antlr4Impl;
using antlr_parser.Antlr4Impl.C;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using Xunit;

namespace antlr_parser.tests.C
{
    public class DirectivesRemoverTest
    {
        [Fact]
        void RemoveElseDirectives()
        {
            string source = @"
                #if 1
                    textToKeep
                #elif 1
                    textToRemove
                #else
                    textToRemove
                #endif
            ".Unindent();

            List<Tuple<int, int>> blocksToRemove = DirectivesRemover.FindBlocksToRemove(source);

            MethodBodyRemovalResult.From(source, blocksToRemove).ShortenedSource.Should().Be("\n    textToKeep\n");
        }

        [Fact]
        void RemoveNestedDirectives()
        {
            string source = @"
                #if 1
                #if 2
                    textToKeep
                #else
                    textToRemove1
                #endif
                #elif 1
                    textToRemove2
                #else
                    textToRemove3
                #endif
            ".Unindent();

            List<Tuple<int, int>> blocksToRemove = DirectivesRemover.FindBlocksToRemove(source);

            MethodBodyRemovalResult.From(source, blocksToRemove).ShortenedSource.Should().Be("\n    textToKeep\n");
        }

        [Fact]
        void RemoveNestedDirectivesLevel2()
        {
            string source = @"
                #if 1
                #if 2
                #if 3
                    textToKeep
                #endif
                    textToKeep2
                #else
                    textToRemove1
                #endif
                #elif 1
                    textToRemove2
                #else
                #if 4
                    textToRemove3
                #endif
                    textToRemove4
                #endif
            ".Unindent();

            List<Tuple<int, int>> blocksToRemove = DirectivesRemover.FindBlocksToRemove(source);

            MethodBodyRemovalResult.From(source, blocksToRemove).ShortenedSource.Should()
                .Be("\n    textToKeep\n    textToKeep2\n");
        }

        [Fact]
        void KeepSameLevelDirectives()
        {
            string source = @"
                #if 1
                #if 2
                    textToKeep
                #else
                    textToRemove1
                #endif
                #elif 1
                    textToRemove2
                #else
                    textToRemove3
                #endif
                #if 3
                    textToKeep2
                #endif
            ".Unindent();

            List<Tuple<int, int>> blocksToRemove = DirectivesRemover.FindBlocksToRemove(source);

            MethodBodyRemovalResult.From(source, blocksToRemove).ShortenedSource.Should()
                .Be("\n    textToKeep\n    textToKeep2\n");
        }
    }
}