using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace antlr_parser.Antlr4Impl.Kotlin
{
    public class KotlinVisitor : KotlinParserBaseVisitor<AstNode>
    {
        readonly string fileName;
        readonly MethodBodyRemovalResult methodBodyRemovalResult;

        public KotlinVisitor(string fileName, MethodBodyRemovalResult methodBodyRemovalResult)
        {
            this.fileName = fileName;
            this.methodBodyRemovalResult = methodBodyRemovalResult;
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
                .DefaultIfEmpty(context.Stop.StopIndex)
                .Min();

            string header = methodBodyRemovalResult.RestoreOriginalSubstring(0, headerEnd).Trim().TrimIndent();

            return new AstNode.FileNode(fileName, pkg, classes, fields, methods, header);
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
            string modifier = ExtractVisibilityModifier(context.modifierList());
            methodBodyRemovalResult.IdxToRemovedMethodBody.TryGetValue(context.Stop.StopIndex, out string removedBody);
            if (string.IsNullOrEmpty(removedBody))
            {
                removedBody = "";
            }

            string sourceCode = context.GetFullText() + removedBody;

            sourceCode = StringUtil.TrimIndent(sourceCode);
            return new AstNode.MethodNode(
                context.identifier().GetFullText(),
                modifier,
                sourceCode,
                context.Start.StartIndex,
                context.Stop.StopIndex
            );
        }

        static string ExtractVisibilityModifier(KotlinParser.ModifierListContext ctx)
        {
            return ctx?.modifier()
                ?.Select(it => it.visibilityModifier())
                .FirstOrDefault()
                ?.GetFullText();
        }

        public override AstNode VisitPackageHeader(KotlinParser.PackageHeaderContext context)
        {
            return new AstNode.PackageNode(context.identifier()?.GetText() ?? "");
        }

        public override AstNode VisitClassDeclaration(KotlinParser.ClassDeclarationContext context)
        {
            string modifier = ExtractVisibilityModifier(context.modifierList());
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
                .DefaultIfEmpty(context.Stop.StopIndex)
                .Min();

            int headerStart = new[]
            {
                PreviousPeerEndPosition(context.Parent, context),
                OutboundClassBodyStartPosition(context.Parent),
                0
            }.Max();

            string header = methodBodyRemovalResult.RestoreOriginalSubstring(headerStart, headerEndIdx)
                .TrimIndent()
                .Trim();

            return new AstNode.ClassNode(
                context.simpleIdentifier().GetFullText(),
                methodNodes,
                fieldNodes,
                innerClasses,
                modifier,
                context.Start.StartIndex,
                context.Stop.StartIndex,
                header
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
                return (parent as KotlinParser.ClassBodyContext).Start.StartIndex + 1; // +1 to exclude '{'
            }

            return OutboundClassBodyStartPosition(parent.Parent);
        }

        int PreviousPeerEndPosition(RuleContext parent, IParseTree self)
        {
            if (parent == null)
            {
                return -1;
            }

            if (parent is KotlinParser.TopLevelObjectContext)
            {
                return PreviousPeerEndPosition(parent.Parent, parent);
            }

            return (parent as ParserRuleContext).children
                .Where(it =>
                    it is KotlinParser.ClassDeclarationContext ||
                    it is KotlinParser.FunctionDeclarationContext ||
                    it is KotlinParser.PropertyDeclarationContext ||
                    it is KotlinParser.TopLevelObjectContext)
                .TakeWhile(it => it != self)
                .OfType<ParserRuleContext>()
                .Select(it => it.Stop.StopIndex + 1)
                .DefaultIfEmpty(-1)
                .Max();
        }

        public override AstNode VisitPropertyDeclaration(KotlinParser.PropertyDeclarationContext context)
        {
            string modifier = ExtractVisibilityModifier(context.modifierList());
            string sourceCode = context.GetFullText();
            return new AstNode.FieldNode(
                context.variableDeclaration().simpleIdentifier().GetFullText(),
                modifier,
                sourceCode,
                context.Start.StartIndex,
                context.Stop.StopIndex
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