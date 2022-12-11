using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Expense_Overview.Account_Statements
{
    public class DibaStatement : AccountStatement
    {
        private List<Expense> expenses;
        public List<Expense> Expenses { get { return expenses; } }
        public string ImportExtension { get { return ".xlsx"; } }
        public bool ReadImport()
        {
            expenses = null;
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                ofd.RestoreDirectory = true;
                ofd.Filter = $"DIBA Import (*{ImportExtension})|*{ImportExtension}";

                if (ofd.ShowDialog() ?? false && new FileInfo(ofd.FileName).Exists)
                {
                    expenses = new List<Expense>();

                    var exp = new Expense();
                    exp.Created = DateTime.MinValue;
                    exp.Booked = DateTime.MinValue;
                    exp.Imported = DateTime.Now;
                    exp.ClientName = "";
                    exp.BookingText = "";
                    exp.UsageText = "";
                    exp.Value = 0;
                    exp.Currency = "";
                    exp.Comment = "";
                    exp.ImportText = "";

                    expenses.Add(exp);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing.\r\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return false;
        }
        
    }
}
