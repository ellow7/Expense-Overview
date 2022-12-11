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
        public List<Tuple<Expense, bool>> CheckForDuplicates(List<Expense> CurrentData)
        {
            try
            {
                List<Tuple<Expense, bool>> CheckedData = new List<Tuple<Expense, bool>>();
                foreach (var exp in this.Expenses)
                {
                    bool isDupe = false;


                    CheckedData.Add(new Tuple<Expense, bool>(exp, isDupe));
                }
                return CheckedData;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking for duplicates.\r\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }
    }
}
