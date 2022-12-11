namespace Expense_Overview.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Decimalvalue : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Expenses", "Value", c => c.Decimal(nullable: false, precision: 18, scale: 2));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Expenses", "Value", c => c.Single(nullable: false));
        }
    }
}
