namespace Expense_Overview.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Deprecitation : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Deprecitations",
                c => new
                    {
                        Id = c.Int(nullable: false),
                        Value = c.Decimal(nullable: false, precision: 18, scale: 2),
                        DurationMonths = c.Int(nullable: false),
                        Comment = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Expenses", t => t.Id)
                .Index(t => t.Id);
            
            AddColumn("dbo.Expenses", "Deprecitation_Id", c => c.Int());
            CreateIndex("dbo.Expenses", "Deprecitation_Id");
            AddForeignKey("dbo.Expenses", "Deprecitation_Id", "dbo.Deprecitations", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Deprecitations", "Id", "dbo.Expenses");
            DropForeignKey("dbo.Expenses", "Deprecitation_Id", "dbo.Deprecitations");
            DropIndex("dbo.Expenses", new[] { "Deprecitation_Id" });
            DropIndex("dbo.Deprecitations", new[] { "Id" });
            DropColumn("dbo.Expenses", "Deprecitation_Id");
            DropTable("dbo.Deprecitations");
        }
    }
}
