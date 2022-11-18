using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Tree;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
using CodeRange = PrimitiveCodebaseElements.Primitive.dto.CodeRange;

namespace antlr_parser.Antlr4Impl.Go
{
    public class GoAstVisitor : GoParserBaseVisitor<AstNode>
    {
        readonly string Path;
        readonly MethodBodyRemovalResult MethodBodyRemovalResult;
        readonly IndexToLocationConverter IndexToLocationConverter;
        readonly CodeRangeCalculator CodeRangeCalculator;

        public GoAstVisitor(
            string path,
            MethodBodyRemovalResult methodBodyRemovalResult,
            CodeRangeCalculator codeRangeCalculator
        )
        {
            Path = path;
            MethodBodyRemovalResult = methodBodyRemovalResult;
            IndexToLocationConverter = new IndexToLocationConverter(methodBodyRemovalResult.OriginalSource);
            CodeRangeCalculator = codeRangeCalculator;
        }

        public override AstNode VisitSourceFile(GoParser.SourceFileContext context)
        {
            string? packageName = context.packageClause()?.packageName?.Text;

            List<AstNode> nodes = context.children.SelectNotNull(child => child.Accept(this))
                .SelectMany(node => node.AsList())
                .ToList();

            List<AstNode.ClassNode> classes = nodes.OfType<AstNode.ClassNode>().ToList();
            List<AstNode.FieldNode> fields = nodes.OfType<AstNode.FieldNode>().ToList();
            List<AstNode.MethodNode> methods = nodes.OfType<AstNode.MethodNode>().ToList();

            int stopIndex = classes.Select(cls => cls.StartIdx - 1)
                .Concat(methods.Select(method => method.StartIdx - 1))
                .Concat(fields.Select(field => field.StartIdx - 1))
                .Concat(new[] { context.Stop.StopIndex })
                .MinOrDefault();

            return new AstNode.FileNode(
                path: Path,
                packageNode: packageName?.Let(it => new AstNode.PackageNode(it)),
                classes: classes,
                fields: fields,
                methods: methods,
                namespaces: new List<AstNode.Namespace>(),
                language: SourceCodeLanguage.Go,
                isTest: false,
                codeRange: CodeRangeCalculator.Trim(
                    new CodeRange(
                        new CodeLocation(1, 1),
                        IndexToLocationConverter.IdxToLocation(MethodBodyRemovalResult.RestoreIdx(stopIndex))
                    )
                )
            );
        }

        public override AstNode VisitTypeDecl(GoParser.TypeDeclContext context)
        {
            List<AstNode> children = context.children.SelectNotNull(x => x.Accept(this))
                .SelectMany(x => x.AsList())
                .ToList();

            List<AstNode.MethodNode> methods = children.OfType<AstNode.MethodNode>().ToList();
            List<AstNode.FieldNode> fields = children.OfType<AstNode.FieldNode>().ToList();
            List<AstNode.ClassNode> classes = children.OfType<AstNode.ClassNode>().ToList();

            int startIdx = context.Start.StartIndex;
            
            var parent = AntlrUtil.FindParent<GoParser.SourceFileContext>(context);
            
            int nearestPeerStopIdx = parent.declaration()
                .Select(x => x.Stop.StopIndex)
                .Concat(parent.functionDecl().Select(x => x.Stop.StopIndex))
                .Concat(parent.methodDecl().Select(x => x.Stop.StopIndex))
                .Where(x => x < startIdx)
                .Select(x => x + 1)
                .MinOrDefault();

            return new AstNode.ClassNode(
                name: context.typeSpec().FirstOrDefault()?.IDENTIFIER()?.GetText() ?? "anon",
                methods: methods,
                fields: fields,
                innerClasses: classes,
                modifier: AccessFlags.None,
                startIdx: startIdx,
                codeRange: CodeRangeCalculator.Trim(
                    IndexToLocationConverter.IdxToCodeRange(
                        Math.Min(startIdx, nearestPeerStopIdx),
                        context.typeSpec()?.FirstOrDefault()?.type_()?.typeLit()?.structType()?.L_CURLY()?
                            .Symbol?
                            .StartIndex ?? context.Stop.StopIndex
                    )
                )
            );
        }

        public override AstNode VisitFieldDecl(GoParser.FieldDeclContext context)
        {
            GoParser.SourceFileContext sourceFileCtx = AntlrUtil.FindParent<GoParser.SourceFileContext>(context)!;
            
            int nearestPeerStopIdx = sourceFileCtx.declaration().Select(x => x.Stop.StopIndex)
                .Concat(sourceFileCtx.functionDecl().Select(x => x.Stop.StopIndex))
                .Concat(sourceFileCtx.methodDecl().Select(x => x.Stop.StopIndex))
                .Where(x => x < context.Start.StartIndex)
                .Select(x => x + 1)
                .MinOrDefault();
            
            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(
                    MethodBodyRemovalResult.RestoreIdx(Math.Min(context.Start.StartIndex,nearestPeerStopIdx)),
                    MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex)
                )
            );

            return context.identifierList()?.IDENTIFIER()?.Select(name =>
                       new AstNode.FieldNode(
                           name: name.GetText(),
                           accFlag: AccessFlags.None,
                           codeRange: codeRange,
                           startIdx: context.Start.StartIndex
                       ) as AstNode
                   ).Aggregate(AstNode.NodeList.Combine) ??
                   new AstNode.FieldNode(
                       "anon",
                       AccessFlags.None,
                       context.Start.StartIndex,
                       codeRange);
        }

        public override AstNode VisitFunctionDecl(GoParser.FunctionDeclContext context)
        {
            string name = context.IDENTIFIER().ToString() ?? "anon";

            GoParser.SourceFileContext sourceFileCtx = AntlrUtil.FindParent<GoParser.SourceFileContext>(context)!;
            
            int nearestPeerStopIdx = sourceFileCtx.declaration().Select(x => x.Stop.StopIndex)
                .Concat(sourceFileCtx.functionDecl().Select(x => x.Stop.StopIndex))
                .Concat(sourceFileCtx.methodDecl().Select(x => x.Stop.StopIndex))
                .Where(x => x < context.Start.StartIndex)
                .Select(x => x + 1)
                .MinOrDefault();
            
            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(
                    Math.Min(context.Start.StartIndex, nearestPeerStopIdx),
                    context.Stop.StopIndex
                )
            );

            List<AstNode.ArgumentNode> arguments = context.signature().parameters().parameterDecl()
                .Select(param => param.Accept(this))
                .Cast<AstNode.ArgumentNode>()
                .ToList();

            string returnType = context.signature()?.result()?.type_()?.typeName()?.IDENTIFIER()?.GetText() ?? "void";

            return new AstNode.MethodNode(
                name: name,
                accFlag: AccessFlags.None,
                startIdx: context.Start.StartIndex,
                codeRange: codeRange,
                arguments: arguments,
                returnType: returnType
            );
        }

        public override AstNode VisitParameterDecl(GoParser.ParameterDeclContext context)
        {
            string type = context.type_()?.typeName()?.IDENTIFIER()?.GetText() ??
                          context.type_()?.typeLit()?.pointerType()?.type_()?.GetText().SubstringBefore("{") ??
                          (context.ELLIPSIS() != null ? "[]" + context.type_().GetText().SubstringBefore("{") : null) ??
                          "anon";
            return new AstNode.ArgumentNode(
                name: context.identifierList()?.GetText() ?? "anon",
                type: type
            );
        }

        public override AstNode VisitMethodDecl(GoParser.MethodDeclContext context)
        {
            GoParser.SourceFileContext sourceFileCtx = AntlrUtil.FindParent<GoParser.SourceFileContext>(context)!;
            
            int nearestPeerStopIdx = sourceFileCtx.declaration().Select(x => x.Stop.StopIndex)
                .Concat(sourceFileCtx.functionDecl().Select(x => x.Stop.StopIndex))
                .Concat(sourceFileCtx.methodDecl().Select(x => x.Stop.StopIndex))
                .Where(x => x < context.Start.StartIndex)
                .Select(x => x + 1)
                .MinOrDefault();
            
            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(
                    Math.Min(context.Start.StartIndex, nearestPeerStopIdx),
                    context.Stop.StopIndex
                )
            );

            List<AstNode.ArgumentNode> receiverParameters = context.receiver()
                ?.parameters()
                .parameterDecl()
                .Select(param => param.Accept(this))
                .Cast<AstNode.ArgumentNode>()
                .ToList() ?? new List<AstNode.ArgumentNode>();

            List<AstNode.ArgumentNode> arguments = context.signature().parameters().parameterDecl()
                .Select(param => param.Accept(this))
                .Cast<AstNode.ArgumentNode>()
                .ToList();

            string returnType = context.signature()?.result()?.type_()?.typeName()?.IDENTIFIER()?.Symbol?.Text ??
                                "void";

            return new AstNode.MethodNode(
                name: context.IDENTIFIER().GetText(),
                accFlag: AccessFlags.None,
                startIdx: context.Start.StartIndex,
                codeRange: codeRange,
                arguments: arguments,
                returnType: returnType,
                receiver: receiverParameters.FirstOrDefault()
            );
        }

        protected override AstNode AggregateResult(AstNode aggregate, AstNode nextResult)
        {
            return AstNode.NodeList.Combine(aggregate, nextResult);
        }
    }
}