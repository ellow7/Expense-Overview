using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Reflection.Emit;

namespace Expense_Overview
{
    /*
    enable-migrations
    add-migration
    update-database -verbose
    update-database -TargetMigration:Init -verbose
    */

    public class ExpenseDBModel : DbContext
    {
        // Your context has been configured to use a 'ExpenseDBModel' connection string from your application's 
        // configuration file (App.config or Web.config). By default, this connection string targets the 
        // 'Expense_Overview.ExpenseDBModel' database on your LocalDb instance. 
        // 
        // If you wish to target a different database and/or database provider, modify the 'ExpenseDBModel' 
        // connection string in the application configuration file.
        public ExpenseDBModel()
            : base("name=ExpenseDBModel")
        {
            Database.SetInitializer<ExpenseDBModel>(new CreateDatabaseIfNotExists<ExpenseDBModel>());
        }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Expenses>().Property(m => m.Value).HasPrecision(18, 2);
        }

        // Add a DbSet for each entity type that you want to include in your model. For more information 
        // on configuring and using a Code First model, see http://go.microsoft.com/fwlink/?LinkId=390109.

        // public virtual DbSet<MyEntity> MyEntities { get; set; }
        public DbSet<Expenses> Expenses { get; set; }
        public DbSet<ExpenseTypes> ExpenseTypes { get; set; }
    }
    /// <summary>
    /// What expenses were booked when and why
    /// </summary>
    public class Expenses
    {
        public Expenses() { }
        public Expenses(DateTime? created, DateTime booked, DateTime? imported, string clientName, string bookingText, string usageText, decimal value, string currency, string comment, string importText, ExpenseTypes expenseTypes)
        {
            Created = created;
            Booked = booked;
            Imported = imported;
            ClientName = clientName;
            BookingText = bookingText;
            UsageText = usageText;
            Value = value;
            Currency = currency;
            Comment = comment;
            ImportText = importText;
            ExpenseTypes = expenseTypes;
        }

        [Key]
        public int Id { get; set; }
        /// <summary>
        /// When the booking got created
        /// </summary>
        public DateTime? Created { get; set; }
        /// <summary>
        /// When the value actually got booked on the account ("Valuta")
        /// </summary>
        public DateTime Booked { get; set; }
        /// <summary>
        /// When this got imported or created
        /// </summary>
        public DateTime? Imported { get; set; }
        /// <summary>
        /// From / to whom does this expense go
        /// </summary>
        public string ClientName { get; set; }
        /// <summary>
        /// Reason of this expense (e.g. "debit", "credit", "bankwire") 
        /// </summary>
        public string BookingText { get; set; }
        /// <summary>
        /// Basically why this got booked (e.g. "payment of the bill")
        /// </summary>
        public string UsageText { get; set; }
        /// <summary>
        /// The actual value. The important part :)
        /// </summary>
        public decimal Value { get; set; }
        /// <summary>
        /// Currency, e.g. €, $
        /// </summary>
        public string Currency { get; set; }
        /// <summary>
        /// We can set some kind of comment
        /// </summary>
        public string Comment { get; set; }
        /// <summary>
        /// What exactly got imported as a string
        /// (mostly for debugging and documentation)
        /// </summary>
        public string ImportText { get; set; }
        [NotMapped]
        public string ExpenseTypeName { get { return ExpenseTypes?.Name; } }

        //FKs
        public virtual ExpenseTypes ExpenseTypes { get; set; }

    }
    /// <summary>
    /// What type of expense. E.g. groceries, car, toys
    /// </summary>
    public class ExpenseTypes
    {
        public ExpenseTypes() { }
        public ExpenseTypes(string name, string comment)
        {
            Name = name;
            Comment = comment;
        }

        [Key]
        public int Id { get; set; }
        /// <summary>
        /// Well... The name of the type
        /// </summary>
        [StringLength(450)]
        [Index(IsUnique = true)]
        public string Name { get; set; }
        /// <summary>
        /// Commens are always good
        /// </summary>
        public string Comment { get; set; }

        //FKs
        public virtual ICollection<Expenses> Expenses { get; set; }
    }
}
