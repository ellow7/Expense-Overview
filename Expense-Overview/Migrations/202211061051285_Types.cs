namespace Expense_Overview.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Types : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ExpenseTypes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(maxLength: 450),
                        Comment = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Name, unique: true);
            
            AddColumn("dbo.Expenses", "Imported", c => c.DateTime());
            AddColumn("dbo.Expenses", "ExpenseTypes_Id", c => c.Int());
            AlterColumn("dbo.Expenses", "Booked", c => c.DateTime(nullable: false));
            CreateIndex("dbo.Expenses", "ExpenseTypes_Id");
            AddForeignKey("dbo.Expenses", "ExpenseTypes_Id", "dbo.ExpenseTypes", "Id");
            DropColumn("dbo.Expenses", "TypeID");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Expenses", "TypeID", c => c.Int());
            DropForeignKey("dbo.Expenses", "ExpenseTypes_Id", "dbo.ExpenseTypes");
            DropIndex("dbo.ExpenseTypes", new[] { "Name" });
            DropIndex("dbo.Expenses", new[] { "ExpenseTypes_Id" });
            AlterColumn("dbo.Expenses", "Booked", c => c.DateTime());
            DropColumn("dbo.Expenses", "ExpenseTypes_Id");
            DropColumn("dbo.Expenses", "Imported");
            DropTable("dbo.ExpenseTypes");
        }
    }
}
