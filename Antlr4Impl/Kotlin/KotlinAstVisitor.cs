using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Antlr4.Runtime;

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

            ImmutableList<AstNode.ClassNode> classes = parsed.OfType<AstNode.ClassNode>().ToImmutableList();
            ImmutableList<AstNode.MethodNode> methods = parsed.OfType<AstNode.MethodNode>().ToImmutableList();
            ImmutableList<AstNode.FieldNode> fields = parsed.OfType<AstNode.FieldNode>().ToImmutableList();

            return new AstNode.FileNode(fileName, pkg, classes, fields, methods);
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
            return new AstNode.MethodNode(context.identifier().GetFullText(), modifier, sourceCode);
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
            ImmutableList<AstNode> parsedMembers = context.classBody()?.classMemberDeclaration()
                                                       ?.Select(decl => decl.Accept(this))
                                                       .Where(it => it != null)
                                                       .ToImmutableList()
                                                   ?? ImmutableList<AstNode>.Empty;

            return new AstNode.ClassNode(
                context.simpleIdentifier().GetFullText(),
                parsedMembers.OfType<AstNode.MethodNode>().ToImmutableList(),
                parsedMembers.OfType<AstNode.FieldNode>().ToImmutableList(),
                parsedMembers.OfType<AstNode.ClassNode>().ToImmutableList(),
                modifier
            );
        }

        public override AstNode VisitPropertyDeclaration(KotlinParser.PropertyDeclarationContext context)
        {
            string modifier = ExtractVisibilityModifier(context.modifierList());
            string sourceCode = context.GetFullText();
            return new AstNode.FieldNode(
                context.variableDeclaration().simpleIdentifier().GetFullText(),
                modifier, sourceCode);
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