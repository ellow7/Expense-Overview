using ClosedXML;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing.Charts;
using Expense_Overview.Account_Statements;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Expense_Overview
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Main : Window
    {
        public ExpenseDBModel DB;
        private Random rnd = new Random();
        public Main()
        {
            InitializeComponent();
        }
        private void insertDemoData()
        {
            var res = MessageBox.Show("Are you sure you want to insert demo data?", "Insert Demo Data?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes)
                return;

            var tpGroceries = DB.ExpenseType.Where(R => R.Name == "Groceries").FirstOrDefault();
            if (tpGroceries == null)
            {
                tpGroceries = new ExpenseType("Groceries", "");
                DB.ExpenseType.Add(tpGroceries);
            }
            var tpHoliday = DB.ExpenseType.Where(R => R.Name == "Holiday").FirstOrDefault();
            if (tpHoliday == null)
            {
                tpHoliday = new ExpenseType("Holiday", "");
                DB.ExpenseType.Add(tpHoliday);
            }
            for (int i = 0; i < 100; i++)
            {
                var exp = new Expense();
                exp.Value = rnd.Next(10, 1000) + rnd.Next(0, 100) / 100;
                exp.ClientName = "Peter Pan Holidays " + i;
                exp.BookingText = "Neverland Holiday 2022";
                exp.Booked = DateTime.Now.AddDays(-rnd.Next(0, 365));
                exp.ExpenseType = tpHoliday;
                DB.Expense.Add(exp);
            }
            DB.SaveChanges();
        }
        private void Window_Initialized(object sender, EventArgs e)
        {
            DB = new ExpenseDBModel();
            //insertDemoData();

            var today = DateTime.Today;
            var firstDay = new DateTime(today.Year, today.Month, 1);
            var firstDayNextMonth = firstDay.AddMonths(1);
            var lastDay = firstDayNextMonth.AddDays(-1);
            DPStartDate.SelectedDate = firstDay;
            DPEndDate.SelectedDate = lastDay;
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //TODO: Check for pending changes
            this.Focus();
        }
        #region Expenses
        private void LoadData()
        {
            LoadData(DPStartDate.SelectedDate ?? DateTime.MinValue, DPEndDate.SelectedDate ?? DateTime.MaxValue);
        }
        private void LoadData(DateTime start, DateTime end)
        {
            try
            {
                DB = new ExpenseDBModel();
                #region Expenses
                DB.Expense.Load();
                bool searchWithTextFilter = TBSearch.Text != "";
                DGExpenses.ItemsSource = null;
                DGExpenses.ItemsSource = DB.Expense.Local.Where
                    (
                        R => R.Booked >= start && R.Booked <= end && //date filtering
                        !(
                            searchWithTextFilter && //search box filtering
                            !(
                                R.Booked.ToString("yyyy-MM-dd HH:mm").Contains(TBSearch.Text.ToUpper()) ||
                                (R.ClientName?.ToUpper() ?? "").Contains(TBSearch.Text.ToUpper()) ||
                                (R.BookingText?.ToUpper() ?? "").Contains(TBSearch.Text.ToUpper()) ||
                                (R.UsageText?.ToUpper() ?? "").Contains(TBSearch.Text.ToUpper()) ||
                                R.Value.ToString().Contains(TBSearch.Text.ToUpper()) ||
                                (R.Comment?.ToUpper() ?? "").Contains(TBSearch.Text.ToUpper()) ||
                                (R.ExpenseType?.Name?.ToUpper() ?? "").Contains(TBSearch.Text.ToUpper())
                            )
                        )
                    ).ToList();
                //DGExpenses.Columns.FirstOrDefault().SortDirection = System.ComponentModel.ListSortDirection.Descending;
                #endregion

                #region ExpenseTypes
                DB.ExpenseType.Load();
                DGCBCExpenseTypes.ItemsSource = null;
                DGCBCExpenseTypes.ItemsSource = DB.ExpenseType.Local.OrderBy(R => R.Id);//Combobox in Expenses
                DGCBCExpenseTypesImport.ItemsSource = null;
                DGCBCExpenseTypesImport.ItemsSource = DB.ExpenseType.Local.OrderBy(R => R.Id);//Combobox in Import
                DGExpenseTypes.ItemsSource = null;
                DGExpenseTypes.ItemsSource = DB.ExpenseType.Local.OrderBy(R => R.Id).ToList();//Datagrid in ExpenseTypes
                //DGExpenseTypes.Columns.FirstOrDefault().SortDirection = System.ComponentModel.ListSortDirection.Descending;
                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading.\r\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BTExportExpenses_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            sfd.RestoreDirectory = true;
            sfd.Filter = "Excel file (*.xlsx)|*.xlsx";
            string start = (DPStartDate.SelectedDate ?? DateTime.MinValue).ToString("yyyy-MM-dd");
            string end = (DPEndDate.SelectedDate ?? DateTime.MaxValue).ToString("yyyy-MM-dd");
            string filter = TBSearch.Text;
            if (filter != "")
                filter = " Filter " + filter;
            sfd.FileName = $"Expense Export {start} - {end}{filter}.xlsx";

            if (sfd.ShowDialog() ?? false)
            {
                var wb = new XLWorkbook();
                var ws = wb.AddWorksheet("Expense export");

                //Get data
                var data = (DGExpenses.ItemsSource as List<Expense>)
                    .Select(R => new
                    {
                        R.Booked,
                        R.ClientName,
                        R.BookingText,
                        R.Value,
                        R.ExpenseType.Name
                    }).ToList();
                var header = data.FirstOrDefault().GetType().GetProperties().Select(R => R.Name);

                //Set values
                var headerCells = ws.Cell(1, 1).InsertData(header, true);
                var dataCells = ws.Cell(2, 1).InsertData(data);

                //Format stuff
                headerCells.Style.Font.Bold = true;
                headerCells.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerCells.Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);
                headerCells.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                dataCells.Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);
                dataCells.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                ws.Columns().AdjustToContents();

                //Save - duh
                wb.SaveAs(sfd.FileName);
            }
        }

        private void SearchBoxes_TextChanged(object sender, RoutedEventArgs e)
        {
            LoadData();
        }
        private void BTSaveData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Focus();
                DB.SaveChanges();
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving.\r\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadData();
            }
        }
        private void BTRemoveExpense_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var toRemove = (Expense)DGExpenses.SelectedItem;
                var res = MessageBox.Show("Are you sure you want to remove this expense?", "Sure?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res != MessageBoxResult.Yes)
                    return;
                DB.Expense.Remove(toRemove);
                DB.SaveChanges();
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error removing item.\r\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadData();
            }
        }

        private void BTRemoveExpenseType_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var toRemove = (ExpenseType)DGExpenseTypes.SelectedItem;
                int amountExpenses = toRemove.Expenses?.Count ?? 0;
                var res = MessageBox.Show($"Are you sure you want to remove this expense type?\r\nThere are {amountExpenses} using this type.", "Sure?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res != MessageBoxResult.Yes)
                    return;
                DB.ExpenseType.Remove(toRemove);
                DB.SaveChanges();
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error removing item.\r\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadData();
            }
        }
        #endregion

        #region Import
        AccountStatement import;
        AccountStatementHandler importHandler;
        private void BTImport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DGImport.ItemsSource = null;
                import = new DibaStatement();//change this if you implemented new types
                import.ReadImport();
                importHandler = new AccountStatementHandler(import, DB);
                importHandler.RemoveDuplicates();
                importHandler.GuessExpenseTypes();

                DGImport.ItemsSource = importHandler.Statement.Expenses;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing.\r\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                DGImport.ItemsSource = null;//reset import
                import = null;
                importHandler = null;
            }
        }

        private void BTSaveImport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DB.Expense.AddRange(import.Expenses);
                DB.SaveChanges();
                DGImport.ItemsSource = null;//reset import
                import = null;
                importHandler = null;
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving import.\r\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        private void BTAddType_Click(object sender, RoutedEventArgs e)
        {
            try { 
            DB.ExpenseType.Add(new ExpenseType());
            DB.SaveChanges();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding type.\r\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            LoadData();
        }

        #region Settings
        private void BTBackupDB_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                sfd.RestoreDirectory = true;
                sfd.Filter = "Backup (*.bak)|*.bak";
                string timestamp = DateTime.Now.ToString("yyyy MM dd");
                sfd.FileName = $"{timestamp} Backup Expense DB.bak";

                if (sfd.ShowDialog() ?? false)
                {
                    var success = DB.Backup(sfd.FileName);
                    if (!success)
                        MessageBox.Show($"Error writing backup.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    else
                        MessageBox.Show($"Backup successful.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error writing backup.\r\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BTRestoreDB_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var res = MessageBox.Show("Are you sure you want to restore?\r\nThis will wipe the current database!", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res != MessageBoxResult.Yes)
                    return;

                OpenFileDialog ofd = new OpenFileDialog();
                ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                ofd.RestoreDirectory = true;
                ofd.Filter = "Backup (*.bak)|*.bak";

                if (ofd.ShowDialog() ?? false)
                {
                    var success = DB.Restore(ofd.FileName);
                    if (!success)
                        MessageBox.Show($"Error restoring backup.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    else
                        MessageBox.Show($"Restore successful.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error writing backup.\r\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BTWipeDB_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var res = MessageBox.Show("Are you sure you want to wipe the database?\r\nThis will wipe the current database!", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res != MessageBoxResult.Yes)
                    return;
                var success = DB.Wipe();
                if (!success)
                    MessageBox.Show($"Error wiping.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show($"Wipe successful.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error wiping.\r\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion
    }
}
