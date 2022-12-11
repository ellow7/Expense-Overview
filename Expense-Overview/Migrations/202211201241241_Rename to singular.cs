namespace Expense_Overview.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Renametosingular : DbMigration
    {
        public override void Up()
        {
            RenameColumn(table: "dbo.Expenses", name: "ExpenseTypes_Id", newName: "ExpenseType_Id");
            RenameIndex(table: "dbo.Expenses", name: "IX_ExpenseTypes_Id", newName: "IX_ExpenseType_Id");
        }
        
        public override void Down()
        {
            RenameIndex(table: "dbo.Expenses", name: "IX_ExpenseType_Id", newName: "IX_ExpenseTypes_Id");
            RenameColumn(table: "dbo.Expenses", name: "ExpenseType_Id", newName: "ExpenseTypes_Id");
        }
    }
}
