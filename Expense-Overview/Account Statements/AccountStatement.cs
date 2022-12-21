using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Expense_Overview.Account_Statements
{
    public interface AccountStatement
    {
        List<Expense> Expenses { get; }
        bool ReadImport();
    }
    public class AccountStatementHandler
    {
        public AccountStatement Statement { get; set; }
        public ExpenseDBModel CurrentData { get; set; }
        public AccountStatementHandler(AccountStatement statement, ExpenseDBModel currentData)
        {
            this.Statement = statement;
            CurrentData = currentData;
        }

        public bool GuessExpenseTypes()
        {
            try
            {
                if (Statement.Expenses == null || Statement.Expenses.Count == 0)
                    return true;
                foreach (var exp in Statement.Expenses)
                {
                    //exact matches with client name and usage text
                    var exactMatches = CurrentData.Expense.Where(R => R.ClientName == exp.ClientName && R.UsageText == exp.UsageText).ToList();
                    if (exactMatches.Any())
                    {
                        exp.ExpenseType = exactMatches.Where(R => R.ExpenseType != null).OrderByDescending(R => R.Booked).FirstOrDefault()?.ExpenseType ?? null;
                        exp.Comment = exactMatches.Where(R => R.ExpenseType != null).OrderByDescending(R => R.Booked).FirstOrDefault()?.Comment ?? "";
                        continue;
                    }

                    //likely matches with client name and trimmed usage text
                    var trimmer = new Char[] { ' ', '.', ',', '-', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
                    var likelyMatches = CurrentData.Expense.Local.Where(R => R.ClientName == exp.ClientName && R.UsageText.Trim(trimmer) == exp.UsageText.Trim(trimmer)).ToList();
                    if (likelyMatches.Any())
                    {
                        exp.ExpenseType = likelyMatches.Where(R => R.ExpenseType != null).OrderByDescending(R => R.Booked).FirstOrDefault()?.ExpenseType ?? null;
                        continue;
                    }

                    //probable matches with client name - better than nothing
                    var probableMatches = CurrentData.Expense.Where(R => R.ClientName == exp.ClientName).ToList();
                    if (probableMatches.Any())
                    {
                        exp.ExpenseType = probableMatches.Where(R => R.ExpenseType != null).OrderByDescending(R => R.Booked).FirstOrDefault()?.ExpenseType ?? null;
                        continue;
                    }

                    exp.ExpenseType = null;
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error guessing expense types.\r\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return false;
        }
        public bool RemoveDuplicates()
        {
            try
            {
                if (Statement.Expenses == null || Statement.Expenses.Count == 0)
                    return true;
                List<Expense> exactDuplicates = new List<Expense>();
                List<Expense> likelyDuplicates = new List<Expense>();
                List<Expense> probablyDuplicates = new List<Expense>();
                foreach (var exp in Statement.Expenses)
                {
                    //remove exact duplicates via import text
                    var exactDuplicate = CurrentData.Expense.Where(R => R.ImportText == exp.ImportText);
                    if (exactDuplicate.Count() > 0)
                    {
                        exactDuplicates.AddRange(exactDuplicate);
                        continue;
                    }

                    //remove likely duplicates with properties
                    var likelyDuplicate = CurrentData.Expense.Where(R =>
                            R.Booked == exp.Booked
                            && R.ClientName == exp.ClientName
                            && R.BookingText == exp.BookingText
                            && R.UsageText == exp.UsageText
                            && R.Value == exp.Value
                        ).FirstOrDefault();
                    if (likelyDuplicate != null)
                    {
                        likelyDuplicates.Add(likelyDuplicate);
                        continue;
                    }

                    //remove probable duplicates within 7 days with user dialog
                    //get expenses from around this booking
                    DateTime from = exp.Booked.AddDays(-7);
                    DateTime to = exp.Booked.AddDays(+7);
                    List<Expense> bookingsAround = CurrentData.Expense.Where(R => R.Booked > from && R.Booked < to).ToList();
                    probablyDuplicates.AddRange(bookingsAround.Where(R =>
                            R.ClientName == exp.ClientName
                            && R.BookingText == exp.BookingText
                            && R.UsageText == exp.UsageText
                            && R.Value == exp.Value
                        ));
                }

                if (exactDuplicates.Count > 0)
                {
                    string dupes = String.Join("\r\n", exactDuplicates.Select(R => R.ToString()).Take(30).ToArray());
                    if (MessageBox.Show($"The following {exactDuplicates.Count} expenses are exact (100%) duplicates.\r\nDo you want to remove them?\r\n{dupes}", "Duplicates", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        foreach (var rem in exactDuplicates)
                            CurrentData.Expense.Remove(rem);//remove item
                }
                if (likelyDuplicates.Count > 0)
                {
                    string dupes = String.Join("\r\n", likelyDuplicates.Select(R => R.ToString()).Take(20).ToArray());
                    if (MessageBox.Show($"The following {likelyDuplicates.Count} expenses are likely (50%) duplicates.\r\nDo you want to remove them?\r\n{dupes}", "Duplicates", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        foreach (var rem in likelyDuplicates)
                            CurrentData.Expense.Remove(rem);//remove item
                }
                if (probablyDuplicates.Count > 0)
                {
                    string dupes = String.Join("\r\n", probablyDuplicates.Select(R => R.ToString()).Take(20).ToArray());
                    if (MessageBox.Show($"The following {probablyDuplicates.Count} expenses are probably (20%) duplicates.\r\nDo you want to remove them?\r\n{dupes}", "Duplicates", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        foreach (var rem in probablyDuplicates)
                            CurrentData.Expense.Remove(rem);//remove item
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error removing duplicates.\r\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return false;
        }
    }
}
