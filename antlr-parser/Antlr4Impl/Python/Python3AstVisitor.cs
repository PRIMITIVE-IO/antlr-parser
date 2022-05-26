using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
using static antlr_parser.Antlr4Impl.AntlrUtil;
using CodeRange = PrimitiveCodebaseElements.Primitive.dto.CodeRange;

namespace antlr_parser.Antlr4Impl.Python
{
    public class Python3AstVisitor : Python3ParserBaseVisitor<AstNode>
    {
        private readonly string FilePath;
        private readonly Dictionary<int, int> IdxToLastNonWhiteSpace;
        private readonly MethodBodyRemovalResult MethodBodyRemovalResult;
        private readonly IndexToLocationConverter IndexToLocationConverter;

        public Python3AstVisitor(
            string filePath,
            Dictionary<int, int> idxToLastNonWhiteSpace,
            MethodBodyRemovalResult methodBodyRemovalResult,
            IndexToLocationConverter indexToLocationConverter
        )
        {
            FilePath = filePath;
            IdxToLastNonWhiteSpace = idxToLastNonWhiteSpace;
            MethodBodyRemovalResult = methodBodyRemovalResult;
            IndexToLocationConverter = indexToLocationConverter;
        }

        public override AstNode VisitFile_input(Python3Parser.File_inputContext context)
        {
            List<AstNode> children = context.children
                .SelectNotNull(x => x.Accept(this))
                .ToList();


            List<AstNode.ClassNode> classNodes = children.OfType<AstNode.ClassNode>().ToList();
            List<AstNode.FieldNode> fieldNodes = children.OfType<AstNode.FieldNode>().ToList();
            List<AstNode.MethodNode> methodNodes = children.OfType<AstNode.MethodNode>().ToList();

            var headerEnd = classNodes.Select(c => c.CodeRange.End)
                .Concat(fieldNodes.Select(f => f.CodeRange.End))
                .Concat(methodNodes.Select(m => m.CodeRange.End))
                .MinOrDefault();

            CodeRange codeRange = new CodeRange(new CodeLocation(1, 1), headerEnd);

            return new AstNode.FileNode(
                path: FilePath,
                packageNode: null,
                classes: classNodes,
                fields: fieldNodes,
                methods: methodNodes,
                header: "",
                namespaces: new List<AstNode.Namespace>(),
                language: SourceCodeLanguage.Python,
                isTest: false,
                codeRange: codeRange
            );
        }

        public override AstNode VisitClassdef(Python3Parser.ClassdefContext context)
        {
            List<AstNode> children = context.children
                .SelectNotNull(x => x.Accept(this))
                .SelectMany(x => x.AsList())
                .ToList();

            int startIdx = MethodBodyRemovalResult.RestoreIdx(context.Start.StartIndex);
            AstNode? firstChild = children.FirstOrDefault();
            int endIdx = firstChild switch
            {
                AstNode.Comment comment => comment.EndIdx,
                AstNode.FieldNode field => IdxToLastNonWhiteSpace[field.StartIdx - 1],
                AstNode.MethodNode method => IdxToLastNonWhiteSpace[method.StartIdx - 1],
                null => MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex),
                _ => throw new Exception($"Unexpected child node: {firstChild.GetType()} in class {context.GetText()}")
            };

            CodeRange codeRange = IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx);

            return new AstNode.ClassNode(
                name: context.NAME().GetText(),
                methods: children.OfType<AstNode.MethodNode>().ToList(),
                fields: children.OfType<AstNode.FieldNode>().ToList(),
                innerClasses: children.OfType<AstNode.ClassNode>().ToList(),
                modifier: AccessFlags.None,
                startIdx: startIdx,
                endIdx: endIdx,
                header: codeRange.Of(MethodBodyRemovalResult.OriginalSource),
                codeRange: codeRange
            );
        }

        public override AstNode VisitSimple_stmt(Python3Parser.Simple_stmtContext context)
        {
            string text = context.GetText();

            if (text.StartsWith("\"\"\""))
            {
                int commentStartIdx = MethodBodyRemovalResult.RestoreIdx(context.Start.StartIndex);

                int commentEndIdx;
                if (Char.IsWhiteSpace(MethodBodyRemovalResult.ShortenedSource[context.Stop.StopIndex]))
                {
                    commentEndIdx = IdxToLastNonWhiteSpace[MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex)];
                }
                else
                {
                    commentEndIdx = MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex);
                }

                return new AstNode.Comment(text, commentStartIdx, commentEndIdx);
            }

            return VisitChildren(context);
        }

        public override AstNode VisitExpr_stmt(Python3Parser.Expr_stmtContext context)
        {
            string text = context.GetText();

            if (text.StartsWith("\"\"\""))
            {
                int commentStartIdx = MethodBodyRemovalResult.RestoreIdx(context.Start.StartIndex);
                int commentEndIdx = IdxToLastNonWhiteSpace[MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex)];
                return new AstNode.Comment(text, commentStartIdx, commentEndIdx);
            }

            int maxStopLocation = FlattenChildren(context.Parent.Parent as ParserRuleContext)
                .Select(x => x.Stop.StopIndex)
                .MaxOrDefault();

            int startIdx = MethodBodyRemovalResult.RestoreIdx(context.Start.StartIndex);
            int endIdx;
            if (Char.IsWhiteSpace(MethodBodyRemovalResult.ShortenedSource[maxStopLocation]))
            {
                endIdx = IdxToLastNonWhiteSpace[MethodBodyRemovalResult.RestoreIdx(maxStopLocation)];
            }
            else
            {
                endIdx = MethodBodyRemovalResult.RestoreIdx(maxStopLocation);
            }

            CodeRange codeRange = IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx);

            return new AstNode.FieldNode(
                name: context.testlist_star_expr()[0].GetText(),
                accFlag: AccessFlags.None,
                sourceCode: codeRange.Of(MethodBodyRemovalResult.OriginalSource),
                startIdx: startIdx,
                endIdx: endIdx,
                codeRange: codeRange
            );
        }

        public override AstNode VisitFuncdef(Python3Parser.FuncdefContext context)
        {
            Python3Parser.SuiteContext suit = context.children.OfType<Python3Parser.SuiteContext>().FirstOrDefault();

            int endIdx = FlattenChildren(suit)
                .Select(x => x.Stop.StopIndex)
                .MaxOrDefault();

            int startIdx = MethodBodyRemovalResult.RestoreIdx(context.Start.StartIndex);
            int stopIdx;
            if (Char.IsWhiteSpace(MethodBodyRemovalResult.ShortenedSource[endIdx]))
            {
                stopIdx = IdxToLastNonWhiteSpace[MethodBodyRemovalResult.RestoreIdx(endIdx)];
            }
            else
            {
                stopIdx = MethodBodyRemovalResult.RestoreIdx(endIdx + 1) - 1;
            }

            CodeRange codeRange = IndexToLocationConverter.IdxToCodeRange(startIdx, stopIdx);

            return new AstNode.MethodNode(
                name: context.NAME().GetText(),
                accFlag: AccessFlags.None,
                sourceCode: codeRange.Of(MethodBodyRemovalResult.OriginalSource),
                startIdx: startIdx,
                endIdx: stopIdx,
                codeRange: codeRange,
                arguments: new List<AstNode.ArgumentNode>()
            );
        }

        protected override AstNode AggregateResult(AstNode aggregate, AstNode nextResult)
        {
            return AstNode.NodeList.Combine(aggregate, nextResult);
        }
    }
}