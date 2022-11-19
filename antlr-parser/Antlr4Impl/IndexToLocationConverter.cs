using System;
using System.Linq;
using PrimitiveCodebaseElements.Primitive.dto;

namespace antlr_parser.Antlr4Impl
{
    public class IndexToLocationConverter
    {
        readonly int[] LineLengths;

        public IndexToLocationConverter(string text)
        {
            string newLineChar = text.Contains("\r\n") ? "\r\n" : "\n";
            LineLengths = text.Split(newLineChar).Select(it => it.Length + newLineChar.Length).ToArray();
        }

        public CodeRange IdxToCodeRange(int startIdx, int endIdx)
        {
            //TODO do it in a single iteration
            return new CodeRange(
                IdxToLocation(startIdx),
                IdxToLocation(endIdx)
            );
        }

        public CodeLocation IdxToLocation(int idx)
        {
            int charCounter = 0;
            for (int lineIdx = 0; lineIdx < LineLengths.Length; lineIdx++)
            {
                if (charCounter + LineLengths[lineIdx] > idx)
                {
                    int col = idx - charCounter + 1;
                    return new CodeLocation(lineIdx + 1, col);
                }

                charCounter += LineLengths[lineIdx];
            }

            throw new Exception($"Cannot find index: {idx}, in string of length: {LineLengths.Sum()}");
        }
    }
}