using System;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.C
{
    public class TypeQualifierListener : CBaseListener
    {
        public string TypeQualifier = "";
        
        public override void EnterTypeQualifier(CParser.TypeQualifierContext context)
        {
            if (context.Atomic() != null)
            {
                TypeQualifier = "atomic";
            }

            if (context.Const() != null)
            {
                TypeQualifier = "const";
            }

            if (context.Restrict() != null)
            {
                TypeQualifier = "restrict";
            }

            if (context.Volatile() != null)
            {
                TypeQualifier = "volatile";
            }
        }
    }

    public class TypeSpecifierListener : CBaseListener
    {
        public TypeName Type = TypeName.For("void");

        public override void EnterTypeSpecifier(CParser.TypeSpecifierContext context)
        {
            if (context.Bool() != null)
            {
                Type = TypeName.For("bool");
            }

            if (context.Char() != null)
            {
                Type = TypeName.For("char");
            }

            if (context.Complex() != null)
            {
                Type = TypeName.For("complex");
            }

            if (context.Double() != null)
            {
                Type = TypeName.For("double");
            }

            if (context.Float() != null)
            {
                Type = TypeName.For("float");
            }

            if (context.Int() != null)
            {
                Type = TypeName.For("int");
            }

            if (context.Long() != null)
            {
                Type = TypeName.For("long");
            }

            if (context.Short() != null)
            {
                Type = TypeName.For("short");
            }

            if (context.Signed() != null)
            {
                Type = TypeName.For("signed");
            }

            if (context.Unsigned() != null)
            {
                Type = TypeName.For("unsigned");
            }

            if (context.Void() != null)
            {
                Type = TypeName.For("void");
            }

            if (context.typeSpecifier() != null)
            {
                TypeSpecifierListener typeSpecifierListener = new TypeSpecifierListener();
                context.typeSpecifier().EnterRule(typeSpecifierListener);
                Type = typeSpecifierListener.Type;
            }

            if (context.typedefName() != null)
            {
                Console.WriteLine(context.GetFullText());
            }

            if (context.constantExpression() != null)
            {
                Console.WriteLine(context.GetFullText());
            }

            if (context.enumSpecifier() != null)
            {
                Console.WriteLine(context.GetFullText());
            }

            if (context.atomicTypeSpecifier() != null)
            {
                Console.WriteLine(context.GetFullText());
            }

            if (context.structOrUnionSpecifier() != null)
            {
                Console.WriteLine(context.GetFullText());
            }
        }
    }
}