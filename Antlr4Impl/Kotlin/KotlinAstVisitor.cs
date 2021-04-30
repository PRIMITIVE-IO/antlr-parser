using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;

namespace antlr_parser.Antlr4Impl.Kotlin
{
    public class KotlinVisitor : KotlinParserBaseVisitor<Ast>
    {
        readonly string fileName;
        readonly MethodBodyRemovalResult methodBodyRemovalResult;

        public KotlinVisitor(string fileName, MethodBodyRemovalResult methodBodyRemovalResult)
        {
            this.fileName = fileName;
            this.methodBodyRemovalResult = methodBodyRemovalResult;
        }

        public override Ast VisitKotlinFile(KotlinParser.KotlinFileContext context)
        {
            Ast.Package pkg = context.preamble().packageHeader().Accept(this) as Ast.Package;
            List<Ast> parsed = context.topLevelObject()
                .Select(obj => obj.Accept(this))
                .Where(it => it != null)
                .ToList();

            List<Ast.Klass> classes = parsed.OfType<Ast.Klass>().ToList();
            List<Ast.Method> methods = parsed.OfType<Ast.Method>().ToList();
            List<Ast.Field> fields = parsed.OfType<Ast.Field>().ToList();

            return new Ast.File(fileName, pkg, classes, fields, methods);
        }

        public override Ast VisitTopLevelObject(KotlinParser.TopLevelObjectContext context)
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

        public override Ast VisitFunctionDeclaration(KotlinParser.FunctionDeclarationContext context)
        {
            string modifier = ExtractVisibilityModifier(context.modifierList());
            methodBodyRemovalResult.IdxToRemovedMethodBody.TryGetValue(context.Stop.StopIndex, out string removedBody);
            if (string.IsNullOrEmpty(removedBody))
            {
                removedBody = "";
            }
            string sourceCode = context.GetFullText() + removedBody;

            sourceCode = StringUtil.TrimIndent(sourceCode);
            return new Ast.Method(context.identifier().GetFullText(), modifier, sourceCode);
        }

        static string ExtractVisibilityModifier(KotlinParser.ModifierListContext ctx)
        {
            return ctx?.modifier()
                ?.Select(it => it.visibilityModifier())
                .FirstOrDefault()
                ?.GetFullText();
        }

        public override Ast VisitPackageHeader(KotlinParser.PackageHeaderContext context)
        {
            return new Ast.Package(context.identifier()?.GetText() ?? "");
        }

        public override Ast VisitClassDeclaration(KotlinParser.ClassDeclarationContext context)
        {
            string modifier = ExtractVisibilityModifier(context.modifierList());
            List<Ast> parsedMembers = context.classBody()?.classMemberDeclaration()
                                                   ?.Select(decl => decl.Accept(this))
                                                   .Where(it => it != null)
                                                   .ToList()
                                               ?? new List<Ast>();

            return new Ast.Klass(
                context.simpleIdentifier().GetFullText(),
                parsedMembers.OfType<Ast.Method>().ToList(),
                parsedMembers.OfType<Ast.Field>().ToList(),
                parsedMembers.OfType<Ast.Klass>().ToList(),
                modifier
            );
        }

        public override Ast VisitPropertyDeclaration(KotlinParser.PropertyDeclarationContext context)
        {
            string modifier = ExtractVisibilityModifier(context.modifierList());
            string sourceCode = context.GetFullText();
            return new Ast.Field(
                context.variableDeclaration().simpleIdentifier().GetFullText(),
                modifier, sourceCode);
        }

        public override Ast VisitClassMemberDeclaration(KotlinParser.ClassMemberDeclarationContext context)
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