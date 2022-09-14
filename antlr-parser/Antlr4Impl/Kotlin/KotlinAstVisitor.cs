using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
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
            List<AstNode> nodes = AntlrUtil.WalkUntilType(
                context.children,
                new HashSet<Type>
                {
                    typeof(KotlinParser.FunctionDeclarationContext),
                    typeof(KotlinParser.PackageHeaderContext),
                    typeof(KotlinParser.ClassDeclarationContext),
                    typeof(KotlinParser.PropertyDeclarationContext),
                    typeof(KotlinParser.ObjectDeclarationContext),
                    typeof(KotlinParser.VariableDeclarationContext)
                },
                this);

            AstNode.PackageNode? package = nodes.OfType<AstNode.PackageNode>().SingleOrDefault();
            List<AstNode.ClassNode> classes = nodes.OfType<AstNode.ClassNode>().ToList();
            List<AstNode.MethodNode> methods = nodes.OfType<AstNode.MethodNode>().ToList();
            List<AstNode.FieldNode> fields = nodes.OfType<AstNode.FieldNode>().ToList();
            List<AstNode.PackageNode> packages = nodes.OfType<AstNode.PackageNode>().ToList();
            
            CodeRange codeRange = CodeRangeCalculator.Trim(
                new CodeRange(new CodeLocation(1, 1), CodeRangeCalculator.EndPosition()));

            return new AstNode.FileNode(
                path: FilePath,
                packageNode: package,
                classes: classes,
                fields: fields,
                methods: methods,
                namespaces: new List<AstNode.Namespace>(),
                language: SourceCodeLanguage.Kotlin,
                isTest: false,
                codeRange: codeRange
            );
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
                startIdx: startIdx,
                codeRange: codeRange,
                arguments: new List<AstNode.ArgumentNode>()
            );
        }

        public override AstNode VisitPackageHeader(KotlinParser.PackageHeaderContext context)
        {
            return new AstNode.PackageNode(context.identifier()?.GetText() ?? "");
        }

        public override AstNode VisitClassDeclaration(KotlinParser.ClassDeclarationContext context)
        {
            return VisitClassOrObject(context);
        }

        public override AstNode VisitObjectDeclaration(KotlinParser.ObjectDeclarationContext context)
        {
            return VisitClassOrObject(context);
        }

        AstNode VisitClassOrObject(ParserRuleContext context)
        {
            KotlinParser.ModifierListContext modifierListContext = null;
            KotlinParser.SimpleIdentifierContext simpleIdentifierContext = null;
            if (context is KotlinParser.ClassDeclarationContext classDeclarationContext)
            {
                modifierListContext = classDeclarationContext.modifierList();
                simpleIdentifierContext = classDeclarationContext.simpleIdentifier();
            }
            else if (context is KotlinParser.ObjectDeclarationContext objectDeclarationContext)
            {
                modifierListContext = objectDeclarationContext.modifierList();
                simpleIdentifierContext = objectDeclarationContext.simpleIdentifier();
            }
            
            AccessFlags modifier = ExtractVisibilityModifier(modifierListContext);
            List<AstNode> parsedMembers = AntlrUtil.WalkUntilType(
                context.children,
                new HashSet<Type>
                {
                    typeof(KotlinParser.FunctionDeclarationContext),
                    typeof(KotlinParser.PropertyDeclarationContext),
                    typeof(KotlinParser.ClassDeclarationContext),
                    typeof(KotlinParser.ObjectDeclarationContext),
                    typeof(KotlinParser.VariableDeclarationContext)
                },
                this);

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
                context.variableDeclaration().simpleIdentifier().GetFullText(),
                modifier,
                startIdx: startIdx,
                codeRange: codeRange
            );
        }

        public override AstNode VisitVariableDeclaration(KotlinParser.VariableDeclarationContext context)
        {
            AccessFlags modifier = AccessFlags.AccPrivate;
            int startIdx = MethodBodyRemovalResult.RestoreIdx(context.Start.StartIndex);
            int endIdx = MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex);

            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx)
            );

            return new AstNode.FieldNode(
                context.simpleIdentifier().GetFullText(),
                modifier,
                startIdx: startIdx,
                codeRange: codeRange
            );
        }

        #endregion
        
        #region UTIL

        static AccessFlags ExtractVisibilityModifier(KotlinParser.ModifierListContext ctx)
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
            while (true)
            {
                switch (parent)
                {
                    case null:
                        return -1;
                    case KotlinParser.ClassBodyContext classBodyContext:
                        return MethodBodyRemovalResult.RestoreIdx(classBodyContext.Start.StartIndex +
                                                                  1); // +1 to exclude '{'
                    default:
                        parent = parent.Parent;
                        break;
                }
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
        
        #endregion
    }
}