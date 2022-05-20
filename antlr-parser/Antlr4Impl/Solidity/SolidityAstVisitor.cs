using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
using CodeRange = PrimitiveCodebaseElements.Primitive.dto.CodeRange;

namespace antlr_parser.Antlr4Impl.Solidity
{
    public class SolidityAstVisitor : SolidityBaseVisitor<AstNode>
    {
        private readonly string Path;
        private readonly MethodBodyRemovalResult MethodBodyRemovalResult;
        private readonly IndexToLocationConverter IndexToLocationConverter;


        public SolidityAstVisitor(string path, MethodBodyRemovalResult methodBodyRemovalResult)
        {
            Path = path;
            MethodBodyRemovalResult = methodBodyRemovalResult;
            IndexToLocationConverter = new IndexToLocationConverter(methodBodyRemovalResult.OriginalSource);
        }

        public override AstNode VisitSourceUnit(SolidityParser.SourceUnitContext context)
        {
            List<AstNode.ClassNode> classNodes =
                context.children.Select(it => it.Accept(this)).OfType<AstNode.ClassNode>().ToList();

            return new AstNode.FileNode(
                path: "",
                packageNode: null,
                classes: classNodes,
                fields: new List<AstNode.FieldNode>(),
                methods: new List<AstNode.MethodNode>(),
                header: "",
                namespaces: new List<AstNode.Namespace>(),
                language: SourceCodeLanguage.Solidity,
                isTest: false,
                codeRange: null
            );
        }

        public int? NearestPeerEndIndex(RuleContext context, int selfStartIdx)
        {
            switch (context.Parent)
            {
                case SolidityParser.SourceUnitContext c: return null;
                case SolidityParser.ContractDefinitionContext c:
                {
                    return c.contractPart()
                        .Select(it => it.Stop.StopIndex as int?)
                        .TakeWhile(it => it < selfStartIdx)
                        .Max();
                }
                default: return NearestPeerEndIndex(context.Parent, selfStartIdx);
            }
        }

        public int? NearestPeerClassEndIndex(RuleContext context, int selfStartIdx)
        {
            switch (context.Parent)
            {
                case SolidityParser.SourceUnitContext c:
                    return c.contractDefinition()
                        .Select(it => it.Stop.StopIndex as int?)
                        .TakeWhile(it => it < selfStartIdx)
                        .Max();

                default: return NearestPeerClassEndIndex(context.Parent, selfStartIdx);
            }
        }

        static int? EnclosingClassHeaderEnd(RuleContext context)
        {
            switch (context.Parent)
            {
                case SolidityParser.ContractDefinitionContext c:
                    return c.children
                        .OfType<TerminalNodeImpl>()
                        .First(it => it.GetText() == "{")
                        .Symbol.StartIndex;
                case SolidityParser.SourceUnitContext _: return null;
                default: return EnclosingClassHeaderEnd(context.Parent);
            }
        }

        public override AstNode VisitContractDefinition(SolidityParser.ContractDefinitionContext context)
        {
            List<AstNode> children = context.children.Select(it => it.Accept(this)).ToList();

            List<AstNode.MethodNode> methods = children.OfType<AstNode.MethodNode>().ToList();
            List<AstNode.FieldNode> fields = children.OfType<AstNode.FieldNode>().ToList();
            List<AstNode.ClassNode> classes = children.OfType<AstNode.ClassNode>().ToList();

            int startIdx = NearestPeerClassEndIndex(context, context.Start.StartIndex).GetValueOrDefault(-1) + 1;

            int endIdx = context.children
                .OfType<TerminalNodeImpl>()
                .First(it => it.GetText() == "{")
                .Symbol
                .StartIndex;

            string header = MethodBodyRemovalResult.ExtractOriginalSubstring(startIdx, endIdx)
                .Trim()
                .Unindent();

            CodeRange codeRange = IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx);

            return new AstNode.ClassNode(
                name: context.identifier().GetText(),
                methods: methods,
                fields: fields,
                innerClasses: classes,
                modifier: AccessFlags.None,
                startIdx: startIdx,
                endIdx: endIdx,
                header: header,
                codeRange: codeRange
            );
        }

        public override AstNode VisitStateVariableDeclaration(SolidityParser.StateVariableDeclarationContext context)
        {
            AccessFlags accFlag = AccessFlags.None;
            if (context.PrivateKeyword() != null) accFlag = AccessFlags.AccPrivate;
            if (context.PublicKeyword() != null) accFlag = AccessFlags.AccPublic;

            int startIdx = (NearestPeerEndIndex(context, context.Start.StartIndex)
                            ?? EnclosingClassHeaderEnd(context)
                            ?? context.Start.StartIndex - 1) + 1;

            int endIdx = context.Stop.StopIndex;

            string source = MethodBodyRemovalResult.ExtractOriginalSubstring(startIdx, endIdx)
                .Trim()
                .Unindent();

            CodeRange codeRange = IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx);

            return new AstNode.FieldNode(
                name: context.identifier().GetText(),
                accFlag: accFlag,
                sourceCode: source,
                startIdx: startIdx,
                endIdx: endIdx,
                codeRange: codeRange
            );
        }

        public override AstNode VisitFunctionDefinition(SolidityParser.FunctionDefinitionContext context)
        {
            string name = context.functionDescriptor().identifier()?.GetText() ??
                          context.functionDescriptor().ConstructorKeyword()?.GetText();

            int startIdx = (NearestPeerEndIndex(context, context.Start.StartIndex)
                            ?? EnclosingClassHeaderEnd(context)
                            ?? context.Start.StartIndex - 1) + 1;

            int endIdx = context.Stop.StopIndex;

            string source = MethodBodyRemovalResult.ExtractOriginalSubstring(startIdx, endIdx)
                .Trim()
                .Unindent();

            CodeRange codeRange = IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx);

            return new AstNode.MethodNode(
                name: name,
                accFlag: ExtractAccFlags(context.modifierList()),
                sourceCode: source,
                startIdx: startIdx,
                endIdx: endIdx,
                codeRange: codeRange,
                arguments: new List<AstNode.ArgumentNode>()
            );
        }

        AccessFlags ExtractAccFlags(SolidityParser.ModifierListContext context)
        {
            if (context.PublicKeyword() != null) return AccessFlags.AccPublic;
            if (context.PrivateKeyword() != null) return AccessFlags.AccPrivate;
            return AccessFlags.None;
        }


        protected override AstNode AggregateResult(AstNode aggregate, AstNode nextResult)
        {
            return AstNode.NodeList.Combine(aggregate, nextResult);
        }
    }
}