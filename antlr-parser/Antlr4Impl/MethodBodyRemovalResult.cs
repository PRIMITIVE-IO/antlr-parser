using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace antlr_parser.Antlr4Impl
{
    public class MethodBodyRemovalResult
    {
        public readonly string ShortenedSource;
        public readonly string OriginalSource;
        readonly List<Tuple<int, int>> BlocksToRemove;

        /// <summary>
        /// Remove blocks from original text. Indices are zero-based. Both start and end index are included (both will be removed)
        /// </summary>
        public static MethodBodyRemovalResult From(string originalSource, List<Tuple<int, int>> blocksToRemove)
        {
            List<Tuple<int, int>> topLevelBlocksToRemove = RemoveNested(blocksToRemove.ToSortedSet(x => x.Item1));
            string shortenedSource = RemoveBlocks(originalSource, topLevelBlocksToRemove);
            return new MethodBodyRemovalResult(shortenedSource, originalSource, topLevelBlocksToRemove);
        }

        /// Is used when have to remove blocks from shortened source.
        /// Results 'connects' original source and shortened-shortened one :)
        /// <param name="blocksToRemove"> indices from shortened text</param>
        public MethodBodyRemovalResult RemoveFromShortened(List<Tuple<int, int>> blocksToRemove)
        {
            IEnumerable<Tuple<int, int>> restoredBlocksToRemove = blocksToRemove
                .Select(r => new Tuple<int, int>(RestoreIdx(r.Item1), RestoreIdx(r.Item2)));

            List<Tuple<int, int>> newBlocksToRemove =
                RemoveNested(BlocksToRemove.Concat(restoredBlocksToRemove).ToSortedSet(x => x.Item1));

            return new MethodBodyRemovalResult(
                RemoveBlocks(OriginalSource, newBlocksToRemove),
                OriginalSource,
                newBlocksToRemove
            );
        }

        static string RemoveBlocks(string source, List<Tuple<int, int>> removeFromTo)
        {
            if (!removeFromTo.Any()) return source;

            StringBuilder sb = new StringBuilder();

            sb.Append(source[..removeFromTo.First().Item1]);

            for (int i = 0; i < removeFromTo.Count - 1; i++)
            {
                sb.Append(source[(removeFromTo[i].Item2 + 1)..(removeFromTo[i + 1].Item1)]);
            }

            if (removeFromTo.Last().Item2 + 1 < source.Length)
            {
                sb.Append(source[(removeFromTo.Last().Item2 + 1)..]);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Removes nested blocks for removal. For example:
        /// for input: [1..10, 2..5] this method return: [1..10], since
        /// second block (2..5) is in first block (1..10).  
        /// </summary>
        /// <param name="blocksForRemoval"></param>
        /// <returns></returns>
        static List<Tuple<int, int>> RemoveNested(SortedSet<Tuple<int, int>> blocksForRemoval)
        {
            List<Tuple<int, int>> res = new();
            int lastIndexForRemoval = -1;
            foreach (Tuple<int, int> blockForRemoval in blocksForRemoval)
            {
                (int fromIndex, int toIndex) = blockForRemoval;

                if (fromIndex <= lastIndexForRemoval) continue;

                res.Add(blockForRemoval);
                lastIndexForRemoval = toIndex;
            }

            return res.ToList();
        }

        MethodBodyRemovalResult(
            string shortenedSource,
            string originalSource,
            List<Tuple<int, int>> blocksToRemove
        )
        {
            ShortenedSource = shortenedSource;
            BlocksToRemove = blocksToRemove;
            OriginalSource = originalSource;
        }

        /// <summary>
        /// Restores original indices based on indices from "shortened" (without method bodies) source code 
        /// </summary>
        /// <param name="idx">index in "shortened" source</param>
        /// <returns>index from an original source code, including removed blocks</returns>
        public int RestoreIdx(int idx)
        {
            int acc = idx;
            foreach ((int fromIdx, int toIdx) in BlocksToRemove) //TODO rework to log(n) using binary search?
            {
                if (acc < fromIdx) break;
                acc += toIdx - fromIdx + 1;
            }

            return acc;
        }
    }
}