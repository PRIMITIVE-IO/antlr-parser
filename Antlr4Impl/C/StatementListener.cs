using System;

namespace antlr_parser.Antlr4Impl.C
{
    public class StatementListener : CBaseListener
    {
        public override void EnterStatement(CParser.StatementContext context)
        {
            Console.WriteLine(context.GetFullText());
        }
    }
    
    public class CompoundStatementListener : CBaseListener
    {
        public override void EnterCompoundStatement(CParser.CompoundStatementContext context)
        {
            BlockItemListListener blockItemListListener = new BlockItemListListener();
            context.blockItemList().EnterRule(blockItemListListener);
        }

        class BlockItemListListener : CBaseListener
        {
            public override void EnterBlockItemList(CParser.BlockItemListContext context)
            {
                if (context.blockItem() != null)
                {
                    BlockItemListener blockItemListener = new BlockItemListener();
                    context.blockItem().EnterRule(blockItemListener);
                }

                if (context.blockItemList() != null)
                {
                    BlockItemListListener blockItemListListener = new BlockItemListListener();
                    context.blockItemList().EnterRule(blockItemListListener);
                }
            }

            class BlockItemListener : CBaseListener
            {
                public override void EnterBlockItem(CParser.BlockItemContext context)
                {
                    if (context.declaration() != null)
                    {
                        DeclarationListener declarationListener = new DeclarationListener();
                        context.declaration().EnterRule(declarationListener);
                    }

                    if (context.statement() != null)
                    {
                        StatementListener statementListener = new StatementListener();
                        context.statement().EnterRule(statementListener);
                    }
                }
            }
        }
    }
}