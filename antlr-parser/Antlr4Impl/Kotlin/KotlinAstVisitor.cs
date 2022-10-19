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

        #region VISITORS

        public override AstNode VisitKotlinFile(KotlinParser.KotlinFileContext context)
        {
            AstNode.PackageNode? pkg = context.preamble().packageHeader().Accept(this) as AstNode.PackageNode;
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

            List<AstNode.ArgumentNode> arguments = context.functionValueParameters()?.functionValueParameter()?
                .Select(parameter => parameter.Accept(this) as AstNode.ArgumentNode)
                .ToList() ?? new List<AstNode.ArgumentNode>();

            List<string> types = context.type()?
                .Select(param => param.functionType()?.GetText() ?? "void")
                .ToList() ?? new List<string> { "void" };

            string returnType = string.Join(",", types);

            return new AstNode.MethodNode(
                name: context.identifier().GetFullText(),
                modifier,
                startIdx: startIdx,
                codeRange: codeRange,
                arguments: arguments,
                returnType: returnType
            );
        }

        public override AstNode VisitFunctionValueParameter(KotlinParser.FunctionValueParameterContext context)
        {
            return new AstNode.ArgumentNode(
                name: context.parameter()?.simpleIdentifier()?.GetText() ?? "",
                type: context.parameter()?.type()?.GetText() ?? "");
        }

        public override AstNode VisitPackageHeader(KotlinParser.PackageHeaderContext context)
        {
            return new AstNode.PackageNode(context.identifier()?.GetText() ?? "");
        }

        public override AstNode VisitObjectDeclaration(KotlinParser.ObjectDeclarationContext context)
        {
            return ClassOrObjectDeclaration(
                context.modifierList(),
                context.simpleIdentifier(),
                context.classBody(),
                context
            );
        }

        public override AstNode VisitClassDeclaration(KotlinParser.ClassDeclarationContext context)
        {
            return ClassOrObjectDeclaration(
                context.modifierList(),
                context.simpleIdentifier(),
                context.classBody(),
                context
            );
        }

        AstNode ClassOrObjectDeclaration(
            KotlinParser.ModifierListContext modifierListContext,
            KotlinParser.SimpleIdentifierContext simpleIdentifierContext,
            KotlinParser.ClassBodyContext? classBodyContext,
            ParserRuleContext context
        )
        {
            AccessFlags modifier = ExtractVisibilityModifier(modifierListContext);

            List<AstNode> parsedMembers = classBodyContext?.classMemberDeclaration()
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

            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(headerStart, headerEndIdx)
            );

            return new AstNode.ClassNode(
                simpleIdentifierContext.GetFullText(),
                methodNodes,
                fieldNodes,
                innerClasses,
                modifier,
                startIdx: startIdx,
                codeRange: codeRange
            );
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
                name: context.variableDeclaration().simpleIdentifier().GetFullText(),
                accFlag: modifier,
                startIdx: startIdx,
                codeRange: codeRange
            );
        }

        #endregion

        #region UTIL

        static AccessFlags ExtractVisibilityModifier(KotlinParser.ModifierListContext? ctx)
        {
            string? accFlag = ctx?.modifier()
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

        int OutboundClassBodyStartPosition(RuleContext? parent)
        {
            switch (parent)
            {
                case null: return -1;
                case KotlinParser.ClassBodyContext x:
                    return MethodBodyRemovalResult.RestoreIdx(x.Start.StartIndex + 1); // +1 to exclude '{'
                default: return OutboundClassBodyStartPosition(parent.Parent);
            }
        }

        int PreviousPeerEndPosition(RuleContext parent, IParseTree self)
        {
            return parent switch
            {
                null => -1,
                KotlinParser.TopLevelObjectContext _ => PreviousPeerEndPosition(parent.Parent, parent),
                _ => (parent as ParserRuleContext).children
                    .Where(it =>
                        it is KotlinParser.ClassDeclarationContext
                            or KotlinParser.FunctionDeclarationContext
                            or KotlinParser.PropertyDeclarationContext
                            or KotlinParser.TopLevelObjectContext)
                    .TakeWhile(it => it != self)
                    .OfType<ParserRuleContext>()
                    .Select(it => MethodBodyRemovalResult.RestoreIdx(it.Stop.StopIndex + 1))
                    .DefaultIfEmpty(-1)
                    .Max()
            };
        }

        public override AstNode? VisitClassMemberDeclaration(KotlinParser.ClassMemberDeclarationContext context)
        {
            ParserRuleContext? declaration = new List<ParserRuleContext>
            {
                context.functionDeclaration(),
                context.classDeclaration(),
                context.propertyDeclaration(),
                context.objectDeclaration()
            }.FirstOrDefault(it => it != null);

            return declaration?.Accept(this);
        }

        #endregion
    }
}