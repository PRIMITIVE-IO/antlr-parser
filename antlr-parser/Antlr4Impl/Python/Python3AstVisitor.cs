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
        readonly string FilePath;
        readonly MethodBodyRemovalResult MethodBodyRemovalResult;
        readonly IndexToLocationConverter IndexToLocationConverter;
        readonly CodeRangeCalculator CodeRangeCalculator;

        public Python3AstVisitor(string filePath,
            MethodBodyRemovalResult methodBodyRemovalResult,
            IndexToLocationConverter indexToLocationConverter,
            CodeRangeCalculator codeRangeCalculator
        )
        {
            FilePath = filePath;
            MethodBodyRemovalResult = methodBodyRemovalResult;
            IndexToLocationConverter = indexToLocationConverter;
            CodeRangeCalculator = codeRangeCalculator;
        }

        public override AstNode VisitFile_input(Python3Parser.File_inputContext context)
        {
            List<AstNode> children = context.children
                .SelectNotNull(x => x.Accept(this))
                .ToList();


            List<AstNode.ClassNode> classNodes = children.OfType<AstNode.ClassNode>().ToList();
            List<AstNode.FieldNode> fieldNodes = children.OfType<AstNode.FieldNode>().ToList();
            List<AstNode.MethodNode> methodNodes = children.OfType<AstNode.MethodNode>().ToList();

            CodeLocation? headerEnd = classNodes.Select(c => c.CodeRange.End)
                .Concat(fieldNodes.Select(f => f.CodeRange.End))
                .Concat(methodNodes.Select(m => m.CodeRange.End))
                .MinOrDefault();

            CodeRange codeRange = CodeRangeCalculator.Trim(
                new CodeRange(new CodeLocation(1, 1), headerEnd)
            );

            return new AstNode.FileNode(
                path: FilePath,
                packageNode: null,
                classes: classNodes,
                fields: fieldNodes,
                methods: methodNodes,
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
                AstNode.FieldNode field => field.StartIdx - 1,
                AstNode.MethodNode method => method.StartIdx - 1,
                null => MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex),
                _ => throw new Exception($"Unexpected child node: {firstChild.GetType()} in class {context.GetText()}")
            };

            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx)
            );

            return new AstNode.ClassNode(
                name: context.NAME().GetText(),
                methods: children.OfType<AstNode.MethodNode>().ToList(),
                fields: children.OfType<AstNode.FieldNode>().ToList(),
                innerClasses: children.OfType<AstNode.ClassNode>().ToList(),
                modifier: AccessFlags.None,
                startIdx: startIdx,
                codeRange: codeRange
            );
        }

        public override AstNode VisitSimple_stmt(Python3Parser.Simple_stmtContext context)
        {
            string text = context.GetText();

            if (text.StartsWith("\"\"\""))
            {
                int commentEndIdx = MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex);

                return new AstNode.Comment(commentEndIdx);
            }

            return VisitChildren(context);
        }

        public override AstNode VisitExpr_stmt(Python3Parser.Expr_stmtContext context)
        {
            string text = context.GetText();

            if (text.StartsWith("\"\"\""))
            {
                int commentStartIdx = MethodBodyRemovalResult.RestoreIdx(context.Start.StartIndex);
                return new AstNode.Comment(commentStartIdx);
            }

            int maxStopLocation = FlattenChildren(context.Parent.Parent as ParserRuleContext)
                .Select(x => x.Stop.StopIndex)
                .MaxOrDefault();

            int startIdx = MethodBodyRemovalResult.RestoreIdx(context.Start.StartIndex);
            int endIdx = MethodBodyRemovalResult.RestoreIdx(maxStopLocation);

            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx)
            );

            return new AstNode.FieldNode(
                name: context.testlist_star_expr()[0].GetText(),
                accFlag: AccessFlags.None,
                startIdx: startIdx,
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
            int stopIdx = MethodBodyRemovalResult.RestoreIdx(endIdx);

            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(startIdx, stopIdx)
            );

            List<AstNode.ArgumentNode> arguments = context.parameters()?.typedargslist()?.tfpdef()
                .Select(param => new AstNode.ArgumentNode(param.GetText(), ""))
                .ToList() ?? new List<AstNode.ArgumentNode>();

            return new AstNode.MethodNode(
                name: context.NAME().GetText(),
                accFlag: AccessFlags.None,
                startIdx: startIdx,
                codeRange: codeRange,
                arguments: arguments,
                returnType: "void"
            );
        }

        protected override AstNode AggregateResult(AstNode aggregate, AstNode nextResult)
        {
            return AstNode.NodeList.Combine(aggregate, nextResult);
        }
    }
}