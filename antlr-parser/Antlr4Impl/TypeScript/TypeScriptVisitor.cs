using System.Collections.Generic;
using System.Linq;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
using CodeRange = PrimitiveCodebaseElements.Primitive.dto.CodeRange;

namespace antlr_parser.Antlr4Impl.TypeScript
{
    public class TypeScriptVisitor : TypeScriptParserBaseVisitor<AstNode>
    {
        readonly string Path;
        readonly MethodBodyRemovalResult MethodBodyRemovalResult;
        readonly IndexToLocationConverter IndexToLocationConverter;

        public TypeScriptVisitor(string path, MethodBodyRemovalResult methodBodyRemovalResult)
        {
            Path = path;
            MethodBodyRemovalResult = methodBodyRemovalResult;
            IndexToLocationConverter = new IndexToLocationConverter(methodBodyRemovalResult.OriginalSource);
        }

        public override AstNode VisitProgram(TypeScriptParser.ProgramContext context)
        {
            if (context.Stop == null)
            {
                string[] lines = MethodBodyRemovalResult.OriginalSource.Split('\n');

                return new AstNode.FileNode(
                    path: Path,
                    packageNode: new AstNode.PackageNode(null),
                    classes: new List<AstNode.ClassNode>(),
                    fields: new List<AstNode.FieldNode>(),
                    methods: new List<AstNode.MethodNode>(),
                    header: "",
                    language: SourceCodeLanguage.TypeScript,
                    isTest: false,
                    namespaces: new List<AstNode.Namespace>(),
                    codeRange: new CodeRange(
                        new CodeLocation(1, 1),
                        new CodeLocation(lines.Length, lines.Last().Length)
                    )
                );
            }

            List<AstNode> children = context.sourceElements()
                                         ?.sourceElement()
                                         ?.Select(it => it.Accept(this))
                                         .ToList()
                                     ?? new List<AstNode>();

            List<AstNode.ClassNode> classes = children.OfType<AstNode.ClassNode>().ToList();
            List<AstNode.FieldNode> fields = children.OfType<AstNode.FieldNode>().ToList();
            List<AstNode.MethodNode> methods = children.OfType<AstNode.MethodNode>().ToList();
            List<AstNode.Namespace> namespaces = children.OfType<AstNode.Namespace>().ToList();

            int headerEndIdxRestored = classes.Select(it => it.StartIdx - 1)
                .Concat(methods.Select(it => it.StartIdx - 1))
                .Concat(fields.Select(it => it.StartIdx - 1))
                .DefaultIfEmpty(MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex))
                .Min();

            CodeLocation headerEndLocationRestored = IndexToLocationConverter.IdxToLocation(headerEndIdxRestored);

            return new AstNode.FileNode(
                path: Path,
                packageNode: new AstNode.PackageNode(null),
                classes: classes,
                fields: fields,
                methods: methods,
                header: "",
                language: SourceCodeLanguage.TypeScript,
                isTest: false,
                namespaces: namespaces,
                codeRange: new CodeRange(new CodeLocation(1, 1), headerEndLocationRestored)
            );
        }

        public override AstNode VisitNamespaceDeclaration(TypeScriptParser.NamespaceDeclarationContext context)
        {
            List<AstNode> children = context.children
                .Select(it => it.Accept(this))
                .ToList();

            return new AstNode.Namespace(
                name: context.namespaceName().GetText(),
                classes: children.OfType<AstNode.ClassNode>().ToList(),
                fields: children.OfType<AstNode.FieldNode>().ToList(),
                methods: children.OfType<AstNode.MethodNode>().ToList(),
                namespaces: children.OfType<AstNode.Namespace>().ToList(),
                startIdx: context.Start.StartIndex,
                endIdx: context.Stop.StopIndex
            );
        }

        public override AstNode VisitClassDeclaration(TypeScriptParser.ClassDeclarationContext context)
        {
            List<AstNode> children = context.children
                .SelectMany(it => it.Accept(this)?.AsList() ?? new List<AstNode>())
                .ToList();

            List<AstNode.FieldNode> fields = children.OfType<AstNode.FieldNode>().ToList();
            List<AstNode.MethodNode> methods = children.OfType<AstNode.MethodNode>().ToList();
            List<AstNode.ClassNode> innerClasses = children.OfType<AstNode.ClassNode>().ToList();

            int headerEndIdx = innerClasses.Select(it => it.StartIdx - 1)
                .Concat(methods.Select(it => it.StartIdx - 1))
                .Concat(fields.Select(it => it.StartIdx - 1))
                .DefaultIfEmpty(context.Stop.StopIndex)
                .Min();

            int headerStart = context.Start.StartIndex;

            int startIdx = MethodBodyRemovalResult.RestoreIdx(headerStart);
            int endIdx = MethodBodyRemovalResult.RestoreIdx(headerEndIdx);

            CodeRange codeRange = IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx);

            return new AstNode.ClassNode(
                name: context.Identifier().ToString(),
                methods: methods,
                fields: fields,
                innerClasses: innerClasses,
                modifier: AccessFlags.None,
                startIdx: startIdx,
                endIdx: endIdx,
                header: "",
                codeRange: codeRange
            );
        }

        public override AstNode VisitMethodDeclarationExpression(
            TypeScriptParser.MethodDeclarationExpressionContext context)
        {
            int startIdx = MethodBodyRemovalResult.RestoreIdx(context.Start.StartIndex);
            int endIdx = MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex);

            AccessFlags accessFlags = AccessFlagsFrom(context.propertyMemberBase().accessibilityModifier()?.GetText());

            CodeRange codeRange = IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx);

            return new AstNode.MethodNode(
                name: context.propertyName().identifierName().Identifier().ToString(),
                sourceCode: "",
                accFlag: accessFlags,
                startIdx: startIdx,
                endIdx: endIdx,
                codeRange: codeRange,
                arguments: new List<AstNode.ArgumentNode>()
            );
        }

        public override AstNode VisitFunctionDeclaration(TypeScriptParser.FunctionDeclarationContext context)
        {
            int startIdx = MethodBodyRemovalResult.RestoreIdx(context.Start.StartIndex);
            int endIdx = MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex);

            CodeRange codeRange = IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx);

            return new AstNode.MethodNode(
                name: context.Identifier().GetText(),
                sourceCode: "",
                accFlag: AccessFlags.None,
                startIdx: startIdx,
                endIdx: endIdx,
                codeRange: codeRange,
                arguments: new List<AstNode.ArgumentNode>()
            );
        }


        public override AstNode VisitPropertyDeclarationExpression(
            TypeScriptParser.PropertyDeclarationExpressionContext context)
        {
            int startIdx = MethodBodyRemovalResult.RestoreIdx(context.Start.StartIndex);
            int endIdx = MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex);

            AccessFlags accessFlags = AccessFlagsFrom(context.propertyMemberBase().accessibilityModifier()?.GetText());

            CodeRange codeRange = IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx);

            return new AstNode.FieldNode(
                name: context.propertyName().identifierName().Identifier().ToString(),
                accessFlags,
                sourceCode: "",
                startIdx: startIdx,
                endIdx: endIdx,
                codeRange: codeRange
            );
        }

        public override AstNode VisitVariableDeclaration(TypeScriptParser.VariableDeclarationContext context)
        {
            int startIdx = MethodBodyRemovalResult.RestoreIdx(context.Start.StartIndex);
            int endIdx = MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex);

            CodeRange codeRange = IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx);

            return new AstNode.FieldNode(
                name: context.identifierOrKeyWord()?.Identifier().ToString(),
                accFlag: AccessFlags.None,
                sourceCode: "",
                startIdx: startIdx,
                endIdx: endIdx,
                codeRange: codeRange
            );
        }

        protected override AstNode AggregateResult(AstNode aggregate, AstNode nextResult)
        {
            return AstNode.NodeList.Combine(aggregate, nextResult);
        }

        public override AstNode VisitConstructorDeclaration(TypeScriptParser.ConstructorDeclarationContext context)
        {
            int startIdx = MethodBodyRemovalResult.RestoreIdx(context.Start.StartIndex);
            int endIdx = MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex);

            AccessFlags accessFlags = AccessFlagsFrom(context.accessibilityModifier()?.GetText());

            CodeRange codeRange = IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx);

            return new AstNode.MethodNode(
                name: "constructor",
                accFlag: accessFlags,
                sourceCode: "",
                startIdx: startIdx,
                endIdx: endIdx,
                codeRange: codeRange,
                arguments: new List<AstNode.ArgumentNode>()
            );
        }

        AccessFlags AccessFlagsFrom(string text)
        {
            return text switch
            {
                "public" => AccessFlags.AccPublic,
                "private" => AccessFlags.AccPrivate,
                _ => AccessFlags.None
            };
        }
    }
}