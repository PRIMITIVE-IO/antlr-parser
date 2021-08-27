using System.Collections.Generic;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.dto
{
    public class MethodDto
    {
        public readonly string Name;
        public readonly AccessFlags AccFlag;
        public readonly List<ArgumentDto> Arguments;
        public readonly string ReturnType;
        public readonly string SourceCode;
        public readonly int StartIdx;
        public readonly int EndIdx;

        public MethodDto(string name,
            AccessFlags accFlag,
            List<ArgumentDto> arguments,
            string returnType,
            string sourceCode,
            int startIdx,
            int endIdx)
        {
            Name = name;
            AccFlag = accFlag;
            SourceCode = sourceCode;
            Arguments = arguments;
            ReturnType = returnType;
            EndIdx = endIdx;
            StartIdx = startIdx;
        }
    }
}