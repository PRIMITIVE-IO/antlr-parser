using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Antlr4.Runtime;

namespace antlr_parser.Antlr4Impl.Kotlin
{
    public class KotlinVisitor : KotlinParserBaseVisitor<Ast>
    {
        private readonly string _fileName;

        public KotlinVisitor(string fileName)
        {
            _fileName = fileName;
        }

        public override Ast VisitKotlinFile(KotlinParser.KotlinFileContext context)
        {
            var pkg = context.preamble().packageHeader().Accept(this) as Ast.Package;
            var parsed = context.topLevelObject()
                .Select(obj => obj.Accept(this) )
                .Where(it => it != null)
                .ToList();

            var classes = parsed.OfType<Ast.Klass>().ToImmutableList();
            var methods = parsed.OfType<Ast.Method>().ToImmutableList();
            var fields = parsed.OfType<Ast.Field>().ToImmutableList();
            
            return new Ast.File(_fileName, pkg, classes, fields, methods);
        }

        public override Ast VisitTopLevelObject(KotlinParser.TopLevelObjectContext context)
        {
            var declaration = new List<ParserRuleContext>()
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
            var modifier = extractVisibilityModifier(context.modifierList());
            var soruceCode = context.GetFullText();

            return new Ast.Method(context.identifier().GetFullText(), modifier, soruceCode);
        }

        private string extractVisibilityModifier(KotlinParser.ModifierListContext ctx)
        {
            return ctx?.modifier()
                ?.Select(it => it.visibilityModifier())
                ?.FirstOrDefault()
                ?.GetFullText();
        }

        public override Ast VisitPackageHeader(KotlinParser.PackageHeaderContext context)
        {
            return new Ast.Package(context.identifier()?.GetText() ?? "");
        }

        public override Ast VisitClassDeclaration(KotlinParser.ClassDeclarationContext context)
        {
            var modifier = extractVisibilityModifier(context.modifierList());
            var parsedMembers = context.classBody()?.classMemberDeclaration()
                ?.Select(decl => decl.Accept(this))
                ?.Where(it => it != null)
                ?.ToImmutableList()
                ??ImmutableList<Ast>.Empty;

            return new Ast.Klass(
                context.simpleIdentifier().GetFullText(),
                parsedMembers.OfType<Ast.Method>().ToImmutableList(),
                parsedMembers.OfType<Ast.Field>().ToImmutableList(),
                parsedMembers.OfType<Ast.Klass>().ToImmutableList(),
                modifier
            );
        }

        public override Ast VisitPropertyDeclaration(KotlinParser.PropertyDeclarationContext context)
        {
            var modifier = extractVisibilityModifier(context.modifierList());
            var sourceCode = context.GetFullText();
            return new Ast.Field(context.variableDeclaration().simpleIdentifier().GetFullText(), modifier, sourceCode);
        }

        public override Ast VisitClassMemberDeclaration(KotlinParser.ClassMemberDeclarationContext context)
        {
            var declaration = new List<ParserRuleContext>
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