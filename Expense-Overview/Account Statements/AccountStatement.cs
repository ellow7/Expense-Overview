using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Expense_Overview.Account_Statements
{
    public interface AccountStatement
    {
        List<Expense> Expenses { get; }
        string ImportExtension { get; }
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
                foreach (var exp in Statement.Expenses)
                {
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
                List<Expense> probablyDuplicates = new List<Expense>();
                foreach (var exp in Statement.Expenses)
                {

                    var exactDuplicate = CurrentData.Expense.Where(R => R.ImportText == exp.ImportText).First();
                    if (exactDuplicate != null)
                    {
                        CurrentData.Expense.Remove(exactDuplicate);
                        continue;
                    }

                    var likelyDuplicate = CurrentData.Expense.Where(R =>
                            R.Booked == exp.Booked
                            && R.ClientName == exp.ClientName
                            && R.BookingText == exp.BookingText
                            && R.UsageText == exp.UsageText
                            && R.Value == exp.Value
                        ).First();
                    if (likelyDuplicate != null)
                    {
                        CurrentData.Expense.Remove(likelyDuplicate);
                        continue;
                    }

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










                    /*
                    //check for identical import text
                    if (CurrentData.Expense.Where(R => R.ImportText == exp.ImportText).Count() > 0)
                        toRemove.Add(exp);
                    //check for identical
                    else if (CurrentData.Expense.Where(R =>
                            R.Booked == exp.Booked
                            && R.ClientName == exp.ClientName
                            && R.BookingText == exp.BookingText
                            && R.UsageText == exp.UsageText
                            && R.Value == exp.Value
                        ).Count() > 0)
                        toRemove.Add(exp);
                    //check for similar booking within ±7 days
                    else if (bookingsAround.Where(R =>
                            R.ClientName == exp.ClientName
                            && R.BookingText == exp.BookingText
                            && R.UsageText == exp.UsageText
                            && R.Value == exp.Value
                        ).Count() > 0)
                        toRemove.Add(exp);*/
                }

                if (probablyDuplicates.Count > 0)
                {
                    string dupes = String.Join("\r\n", probablyDuplicates.Select(R => R.ToString()).ToArray());
                    if (MessageBox.Show($"The following {probablyDuplicates.Count} expenses are probably duplicates.\r\nDo you want to remove them?\r\n{dupes}", "Duplicates", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        foreach (var rem in probablyDuplicates)
                            Statement.Expenses.Remove(rem);//remove item
                    }
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
