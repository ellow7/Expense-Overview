using System;
using System.Collections;
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
    enable-migrations <- run once
    add-migration
    update-database

    Remove migration:
    1) update-database -TargetMigration:'PREVIOUS_MIGRATION'
    2) Remove migration file from folder Migrations
    Example:
    update-database -TargetMigration:'Display position for ordering types'







    */

    public class ExpenseDBModel : DbContext
    {
        public ExpenseDBModel() : base("name=ExpenseDBModel") { }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            //One to zero or one relationship Expense -> Deprecitation
            modelBuilder.Entity<Expense>()
                .HasOptional(R => R.InitialDeprecitation)
                .WithRequired(R => R.InitialExpense);

            //One to many relationship (gets too complicated for EF6)
            modelBuilder.Entity<Expense>()
                .HasOptional<Deprecitation>(R => R.Deprecitation)
                .WithMany(R => R.DeprecitationExpenses);
            //.HasForeignKey(R => R.Id);
        }

        #region Tables
        public DbSet<Expense> Expense { get; set; }
        public DbSet<ExpenseType> ExpenseType { get; set; }
        public DbSet<Deprecitation> Deprecitation { get; set; }
        #endregion

        #region Helper
        #region Backup and Restore
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
            Expense.RemoveRange(Expense);
            ExpenseType.RemoveRange(ExpenseType);
            Deprecitation.RemoveRange(Deprecitation);
            SaveChanges();

            return true;
        }
        #endregion

        /// <summary>
        /// Create a deprecitation from an initial expense.
        /// The value of the inital expense will be set to zero.
        /// </summary>
        public Deprecitation CreateDeprecitation(Expense InitialExpense, int DurationMonths)
        {
            Deprecitation depr;

            //Check for deprecitation with this as inititalExpense
            depr = this.Deprecitation.Where(R => R.InitialExpense.Id == InitialExpense.Id).FirstOrDefault();
            if (depr != null)//already present
                return null;
            //Check for deprecitation with this as expense
            depr = this.Deprecitation.Where(R => R.DeprecitationExpenses.Select(S => S.Id).ToList().Contains(InitialExpense.Id)).FirstOrDefault();
            if (depr != null)//already present
                return null;

            //create new
            depr = new Deprecitation();
            depr.Value = InitialExpense.Value;
            depr.InitialExpense = InitialExpense;

            this.Deprecitation.Add(depr);

            depr.DeprecitationExpenses = new List<Expense>();
            depr.DurationMonths = DurationMonths;
            depr.Comment = InitialExpense.Comment;
            decimal singleExpense = Math.Floor(depr.Value / depr.DurationMonths);//e.g. -1.000€
            decimal remainingExpense = depr.Value;//e.g. -1.800€
            DateTime bookingCounter = new DateTime(InitialExpense.Booked.Year, InitialExpense.Booked.Month, 1).AddMonths(1).Date;

            while (remainingExpense < 0)
            {
                var exp = new Expense();

                if (remainingExpense <= singleExpense)
                    exp.Value = singleExpense;
                else
                    exp.Value = remainingExpense;

                exp.ClientName = InitialExpense.ClientName;
                exp.UsageText = InitialExpense.UsageText;
                exp.Comment = "Deprecitation " + InitialExpense.Comment;
                exp.ExpenseType = InitialExpense.ExpenseType;
                exp.Booked = bookingCounter;

                this.Expense.Add(exp);
                depr.DeprecitationExpenses.Add(exp);
                bookingCounter = bookingCounter.AddMonths(1);
                remainingExpense -= singleExpense;
            }
            InitialExpense.Value = 0;
            return depr;
        }

        /// <summary>
        /// Remove deprecitation, all expenses from it and restore the inital expense.
        /// </summary>
        public bool RemoveDeprecitation(Deprecitation Deprecitation)
        {
            Deprecitation.InitialExpense.Value = Deprecitation.Value;
            this.Expense.RemoveRange(Deprecitation.DeprecitationExpenses);
            this.Deprecitation.Remove(Deprecitation);
            return true;
        }
        #endregion
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
        public virtual Deprecitation InitialDeprecitation { get; set; }
        public virtual Deprecitation Deprecitation { get; set; }

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

        /// <summary>
        /// Display position for ordering types
        /// </summary>
        public int DisplayPosition { get; set; }

        //FKs
        public virtual ICollection<Expense> Expenses { get; set; }

        //Helper
        public override string ToString()
        {
            return DisplayPosition + " " + Name;
        }
    }

    /// <summary>
    /// Linear deprecitations of expenses.
    /// E.g. Split the purchase of a car to expenses over 10 years.
    /// </summary>
    public class Deprecitation
    {
        public Deprecitation() { }
        [Key]
        public int Id { get; set; }
        /// <summary>
        /// Initial value of the expense
        /// (We need this because we set the initial expense costs to 0)
        /// </summary>
        public decimal Value { get; set; }
        /// <summary>
        /// How long we want to depreciate
        /// </summary>
        public int DurationMonths { get; set; }

        public string Comment { get; set; }

        //FKs
        /// <summary>
        /// What we want to split into deprecitations
        /// </summary>
        [Required]
        public virtual Expense InitialExpense { get; set; }
        /// <summary>
        /// All the generated deprecitation costs
        /// </summary>
        public virtual ICollection<Expense> DeprecitationExpenses { get; set; }

    }
}
