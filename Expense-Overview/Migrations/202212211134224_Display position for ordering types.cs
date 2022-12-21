namespace Expense_Overview.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Displaypositionfororderingtypes : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ExpenseTypes", "DisplayPosition", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ExpenseTypes", "DisplayPosition");
        }
    }
}
