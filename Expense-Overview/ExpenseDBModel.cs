using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection.Emit;

namespace Expense_Overview
{
    /*
    enable-migrations
    add-migration
    update-database -verbose
    update-database -TargetMigration:Init -verbose

    Remove migration:
    1) update-database -TargetMigration:PREVIOUS_MIGRATION -verbose
    2) Remove migration file from folder Migrations



    */

    public class ExpenseDBModel : DbContext
    {
        public ExpenseDBModel() : base("name=ExpenseDBModel")
        {
            //this.Database.CommandTimeout = 2;
        }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<Expense>().Property(m => m.Value).HasPrecision(18, 2);
        }

        public DbSet<Expense> Expense { get; set; }
        public DbSet<ExpenseType> ExpenseType { get; set; }

        public bool Backup(string filepath)
        {
            this.Database.ExecuteSqlCommand(
                TransactionalBehavior.DoNotEnsureTransaction,
                @"
                    BACKUP DATABASE ExpenseDB
                    TO DISK = @file
                    WITH INIT
                "
            , new SqlParameter("@file", filepath));
            return new FileInfo(filepath).Exists;
        }
        public bool Restore(string filepath)
        {
            if (!new FileInfo(filepath).Exists)
                return false;
            this.Database.ExecuteSqlCommand(TransactionalBehavior.DoNotEnsureTransaction, 
                @"USE master");
            this.Database.ExecuteSqlCommand(TransactionalBehavior.DoNotEnsureTransaction, 
                @"ALTER DATABASE ExpenseDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE");
            this.Database.ExecuteSqlCommand(
                TransactionalBehavior.DoNotEnsureTransaction,
                @"USE master
                    RESTORE DATABASE ExpenseDB
                    FROM DISK = @file
                "
            , new SqlParameter("@file", filepath));
            return true;
        }
        public bool Wipe()
        {
            //Don't hate me
            //this.Database.ExecuteSqlCommand(@"DELETE FROM Expenses");
            //this.Database.ExecuteSqlCommand(@"DELETE FROM ExpenseTypes");
            Expense.RemoveRange(Expense);
            ExpenseType.RemoveRange(ExpenseType);
            SaveChanges();

            return true;
        }
    }
    /// <summary>
    /// What expenses were booked when and why
    /// </summary>
    public class Expense
    {
        public Expense()
        {
            Created = DateTime.Now;
            Booked = DateTime.Now;
            Imported = null;
            ImportText = "Manually created";
        }
        public Expense(DateTime? created, DateTime booked, DateTime? imported, string clientName, string bookingText, string usageText, decimal value, string currency, string comment, string importText, ExpenseType expenseType)
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
            ExpenseType = expenseType;
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

        //FKs
        public virtual ExpenseType ExpenseType { get; set; }

        //Helper
        public override string ToString()
        {
            return $"Booked {Booked.ToString("yyyy-MM-dd HH:mm")} to {ClientName} with the following information: {BookingText} {UsageText}";
        }
    }
    /// <summary>
    /// What type of expense. E.g. groceries, car, toys
    /// </summary>
    public class ExpenseType
    {
        public ExpenseType() { }
        public ExpenseType(string name, string comment)
        {
            Name = name;
            Comment = comment;
        }

        [Key]
        public int Id { get; set; }
        /// <summary>
        /// Well... The name of the type
        /// </summary>
        [StringLength(450)]//unique needs max length of 450
        [Index(IsUnique = true)]
        public string Name { get; set; }
        /// <summary>
        /// Comments are always good
        /// </summary>
        public string Comment { get; set; }

        //FKs
        public virtual ICollection<Expense> Expenses { get; set; }

        //Helper
        public override string ToString()
        {
            return Name;
        }
    }
}
