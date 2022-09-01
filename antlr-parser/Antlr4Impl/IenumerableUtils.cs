using System;
using System.Collections.Generic;

namespace antlr_parser.Antlr4Impl
{
    public static class IenumerableUtils
    {
        public static SortedSet<T> ToSortedSet<T>(this IEnumerable<T> source) where T : IComparable<T>
        {
            return new SortedSet<T>(source);
        }

        public static SortedSet<T> ToSortedSet<T, Y>(this IEnumerable<T> source, Func<T, Y> by) where Y : IComparable<Y>
        {
            return new SortedSet<T>(source, new ExtractionCompaprer<T, Y>(by));
        }
    }

    class ExtractionCompaprer<T, Y> : Comparer<T> where Y : IComparable<Y>
    {
        readonly Func<T, Y> extractor;

        public ExtractionCompaprer(Func<T, Y> extractor)
        {
            this.extractor = extractor;
        }

        public override int Compare(T? x, T? y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            return extractor(x).CompareTo(extractor(y));
        }
    }
}