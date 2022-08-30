using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using PrimitiveCodebaseElements.Primitive;
using CodeRange = PrimitiveCodebaseElements.Primitive.dto.CodeRange;

namespace antlr_parser.Antlr4Impl.Kotlin
{
    public class KotlinVisitor : KotlinParserBaseVisitor<AstNode>
    {
        readonly string FilePath;
        readonly MethodBodyRemovalResult MethodBodyRemovalResult;
        readonly IndexToLocationConverter IndexToLocationConverter;
        readonly CodeRangeCalculator CodeRangeCalculator;

        public KotlinVisitor(
            string filePath,
            MethodBodyRemovalResult methodBodyRemovalResult,
            CodeRangeCalculator codeRangeCalculator
        )
        {
            FilePath = filePath;
            MethodBodyRemovalResult = methodBodyRemovalResult;
            IndexToLocationConverter = new IndexToLocationConverter(methodBodyRemovalResult.OriginalSource);
            CodeRangeCalculator = codeRangeCalculator;
        }

        public override AstNode VisitKotlinFile(KotlinParser.KotlinFileContext context)
        {
            AstNode.PackageNode pkg = context.preamble().packageHeader().Accept(this) as AstNode.PackageNode;
            List<AstNode> parsed = context.topLevelObject()
                .Select(obj => obj.Accept(this))
                .Where(it => it != null)
                .ToList();

            List<AstNode.ClassNode> classes = parsed.OfType<AstNode.ClassNode>().ToList();
            List<AstNode.MethodNode> methods = parsed.OfType<AstNode.MethodNode>().ToList();
            List<AstNode.FieldNode> fields = parsed.OfType<AstNode.FieldNode>().ToList();

            int headerEnd = classes.Select(it => it.StartIdx - 1)
                .Concat(methods.Select(it => it.StartIdx - 1))
                .Concat(fields.Select(it => it.StartIdx - 1))
                .DefaultIfEmpty(MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex))
                .Min();

            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(0, headerEnd)
            );

            return new AstNode.FileNode(
                path: FilePath,
                packageNode: pkg,
                classes: classes,
                fields: fields,
                methods: methods,
                header: "",
                namespaces: new List<AstNode.Namespace>(),
                language: SourceCodeLanguage.Kotlin,
                isTest: false,
                codeRange: codeRange
            );
        }

        public override AstNode VisitTopLevelObject(KotlinParser.TopLevelObjectContext context)
        {
            ParserRuleContext declaration = new List<ParserRuleContext>()
                {
                    context.classDeclaration(),
                    context.functionDeclaration(),
                    context.objectDeclaration(),
                    context.propertyDeclaration()
                }
                .FirstOrDefault(it => it != null);
            return declaration?.Accept(this);
        }

        public override AstNode VisitFunctionDeclaration(KotlinParser.FunctionDeclarationContext context)
        {
            AccessFlags modifier = ExtractVisibilityModifier(context.modifierList());
            int startIdx = MethodBodyRemovalResult.RestoreIdx(context.Start.StartIndex);
            int endIdx = MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex + 1);

            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx)
            );

            return new AstNode.MethodNode(
                context.identifier().GetFullText(),
                modifier,
                "",
                startIdx: startIdx,
                endIdx: endIdx,
                codeRange: codeRange,
                arguments: new List<AstNode.ArgumentNode>()
            );
        }

        static AccessFlags ExtractVisibilityModifier(KotlinParser.ModifierListContext ctx)
        {
            string accFlag = ctx?.modifier()
                ?.Select(it => it.visibilityModifier())
                .FirstOrDefault()
                ?.GetFullText();

            switch (accFlag)
            {
                case null:
                case "public":
                    return AccessFlags.AccPublic;
                case "private":
                    return AccessFlags.AccPrivate;
                case "internal":
                    return AccessFlags.AccProtected;
                default:
                    return AccessFlags.AccPublic;
            }
        }

        public override AstNode VisitPackageHeader(KotlinParser.PackageHeaderContext context)
        {
            return new AstNode.PackageNode(context.identifier()?.GetText() ?? "");
        }

        public override AstNode VisitClassDeclaration(KotlinParser.ClassDeclarationContext context)
        {
            AccessFlags modifier = ExtractVisibilityModifier(context.modifierList());
            List<AstNode> parsedMembers = context.classBody()?.classMemberDeclaration()
                ?.Select(decl => decl.Accept(this))
                .Where(it => it != null)
                .ToList() ?? new List<AstNode>();


            List<AstNode.ClassNode> innerClasses = parsedMembers.OfType<AstNode.ClassNode>().ToList();
            List<AstNode.FieldNode> fieldNodes = parsedMembers.OfType<AstNode.FieldNode>().ToList();
            List<AstNode.MethodNode> methodNodes = parsedMembers.OfType<AstNode.MethodNode>().ToList();

            int headerEndIdx = innerClasses.Select(it => it.StartIdx - 1)
                .Concat(methodNodes.Select(it => it.StartIdx - 1))
                .Concat(fieldNodes.Select(it => it.StartIdx - 1))
                .DefaultIfEmpty(MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex))
                .Min();

            int headerStart = new[]
            {
                PreviousPeerEndPosition(context.Parent, context),
                OutboundClassBodyStartPosition(context.Parent),
                0
            }.Max();

            int startIdx = MethodBodyRemovalResult.RestoreIdx(context.Start.StartIndex);
            int endIdx = MethodBodyRemovalResult.RestoreIdx(context.Stop.StartIndex);

            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(headerStart, headerEndIdx)
            );

            return new AstNode.ClassNode(
                context.simpleIdentifier().GetFullText(),
                methodNodes,
                fieldNodes,
                innerClasses,
                modifier,
                startIdx: startIdx,
                endIdx: endIdx,
                header: "",
                codeRange: codeRange
            );
        }

        int OutboundClassBodyStartPosition(RuleContext parent)
        {
            if (parent == null)
            {
                return -1;
            }

            if (parent is KotlinParser.ClassBodyContext)
            {
                return MethodBodyRemovalResult.RestoreIdx(
                    (parent as KotlinParser.ClassBodyContext).Start.StartIndex + 1); // +1 to exclude '{'
            }

            return OutboundClassBodyStartPosition(parent.Parent);
        }

        int PreviousPeerEndPosition(RuleContext parent, IParseTree self)
        {
            return parent switch
            {
                null => -1,
                KotlinParser.TopLevelObjectContext _ => PreviousPeerEndPosition(parent.Parent, parent),
                _ => (parent as ParserRuleContext).children
                    .Where(it =>
                        it is KotlinParser.ClassDeclarationContext || it is KotlinParser.FunctionDeclarationContext ||
                        it is KotlinParser.PropertyDeclarationContext || it is KotlinParser.TopLevelObjectContext)
                    .TakeWhile(it => it != self)
                    .OfType<ParserRuleContext>()
                    .Select(it => MethodBodyRemovalResult.RestoreIdx(it.Stop.StopIndex + 1))
                    .DefaultIfEmpty(-1)
                    .Max()
            };
        }

        public override AstNode VisitPropertyDeclaration(KotlinParser.PropertyDeclarationContext context)
        {
            AccessFlags modifier = ExtractVisibilityModifier(context.modifierList());
            int startIdx = MethodBodyRemovalResult.RestoreIdx(context.Start.StartIndex);
            int endIdx = MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex);

            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx)
            );

            return new AstNode.FieldNode(
                context.variableDeclaration().simpleIdentifier().GetFullText(),
                modifier,
                "",
                startIdx: startIdx,
                endIdx: endIdx,
                codeRange: codeRange
            );
        }

        public override AstNode VisitClassMemberDeclaration(KotlinParser.ClassMemberDeclarationContext context)
        {
            ParserRuleContext declaration = new List<ParserRuleContext>
            {
                context.functionDeclaration(),
                context.classDeclaration(),
                context.propertyDeclaration(),
                context.objectDeclaration()
            }.FirstOrDefault(it => it != null);

            return declaration?.Accept(this);
        }
    }
}