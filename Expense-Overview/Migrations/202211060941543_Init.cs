namespace Expense_Overview.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Init : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Expenses",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Created = c.DateTime(),
                        Booked = c.DateTime(),
                        ClientName = c.String(),
                        BookingText = c.String(),
                        UsageText = c.String(),
                        Value = c.Single(nullable: false),
                        Currency = c.String(),
                        Comment = c.String(),
                        TypeID = c.Int(),
                        ImportText = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Expenses");
        }
    }
}
