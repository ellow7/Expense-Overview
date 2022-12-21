﻿using ClosedXML;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing.Charts;
using Expense_Overview.Account_Statements;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.IO;
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
        public ExpenseDBModel DB = new ExpenseDBModel();
        private Random rnd = new Random();
        public Main()
        {
            InitializeComponent();

            //apply settings
            TBAutoBackupPath.Text = Properties.Settings.Default.BackupPath;

            DoAutoBackup();//monthly backup
        }
        private void Window_Initialized(object sender, EventArgs e)
        {
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
                    ).OrderByDescending(R => R.Booked).ToList();
                //DGExpenses.Columns.FirstOrDefault().SortDirection = System.ComponentModel.ListSortDirection.Descending;
                #endregion

                #region ExpenseTypes
                DB.ExpenseType.Load();
                DGCBCExpenseTypes.ItemsSource = null;
                DGCBCExpenseTypes.ItemsSource = DB.ExpenseType.Local.OrderBy(R => R.DisplayPosition);//Combobox in Expenses
                DGCBCExpenseTypesImport.ItemsSource = null;
                DGCBCExpenseTypesImport.ItemsSource = DB.ExpenseType.Local.OrderBy(R => R.DisplayPosition);//Combobox in Import
                DGExpenseTypes.ItemsSource = null;
                DGExpenseTypes.ItemsSource = DB.ExpenseType.Local.OrderBy(R => R.DisplayPosition).ToList();//Datagrid in ExpenseTypes
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
                        R.UsageText,
                        R.Value,
                        R.Comment,
                        Type = R.ExpenseType?.Name
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
            try
            {
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
        private void TBAutoBackupPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            Properties.Settings.Default.BackupPath = TBAutoBackupPath.Text;
            Properties.Settings.Default.Save();
        }
        private void BTBrowseBackupPath_Click(object sender, RoutedEventArgs e)
        {
            var cofd = new CommonOpenFileDialog();
            cofd.IsFolderPicker = true;
            cofd.Multiselect = false;
            cofd.EnsurePathExists = true;
            cofd.RestoreDirectory = false;
            if (TBAutoBackupPath.Text != "")
                cofd.DefaultDirectory = TBAutoBackupPath.Text;
            else
                cofd.DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (cofd.ShowDialog() == CommonFileDialogResult.Ok)
                TBAutoBackupPath.Text = cofd.FileName;
        }
        /// <summary>
        /// Automatically backup each month
        /// </summary>
        private void DoAutoBackup()
        {
            if (DB.Expense.Count() == 0)
                return;//no data no backup
            string timestamp = DateTime.Now.ToString("yyyy MM");
            string filename = $"{timestamp} Backup Expense DB.bak";
            string directory = TBAutoBackupPath.Text;
            if (directory == "")
                return;//no backup wanted
            if (Directory.Exists(directory))
            {
                var file = new FileInfo(System.IO.Path.Combine(directory, filename));
                if (file.Exists)
                    return;//already done backup
                var success = DB.Backup(file.FullName);
                if (!success)
                    MessageBox.Show($"Error writing backup.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show($"Backup successful.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
                MessageBox.Show("Backup directory not accessible:\r\n" + directory, "Backup Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        private void BTBackupDB_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog();
                if (TBAutoBackupPath.Text == "" || !new DirectoryInfo(TBAutoBackupPath.Text).Exists)
                    sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                else
                    sfd.InitialDirectory = TBAutoBackupPath.Text;
                sfd.RestoreDirectory = true;
                sfd.Filter = "Backup (*.bak)|*.bak";
                string timestamp = DateTime.Now.ToString("yyyy MM");
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
        private void BTInsertDemoData_Click(object sender, RoutedEventArgs e)
        {
            InsertDemoData();
        }
        private void InsertDemoData()
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
            var tpFun = DB.ExpenseType.Where(R => R.Name == "Fun Activities").FirstOrDefault();
            if (tpFun == null)
            {
                tpFun = new ExpenseType("Fun Activities", "");
                DB.ExpenseType.Add(tpFun);
            }
            var tpInternet = DB.ExpenseType.Where(R => R.Name == "Internet").FirstOrDefault();
            if (tpInternet == null)
            {
                tpInternet = new ExpenseType("Internet", "");
                DB.ExpenseType.Add(tpInternet);
            }
            var tpIncome = DB.ExpenseType.Where(R => R.Name == "Income").FirstOrDefault();
            if (tpIncome == null)
            {
                tpIncome = new ExpenseType("Income", "");
                DB.ExpenseType.Add(tpIncome);
            }
            for (int i = 0; i < 100; i++)
            {
                var exp = new Expense();
                exp.Value = -(decimal)rnd.Next(10, 1000) + (decimal)rnd.Next(0, 100) / 100;
                exp.ClientName = "Peter Pan Holidays";
                exp.UsageText = "Neverland Holiday " + i;
                exp.Comment = "Demo Data";
                exp.Booked = DateTime.Now.AddDays(-rnd.Next(0, 365)).AddMinutes(rnd.Next(0, 60 * 24));
                if (rnd.Next(10) > 5)
                    exp.ExpenseType = tpHoliday;
                else
                    exp.ExpenseType = tpFun;
                DB.Expense.Add(exp);
            }
            for (int i = 0; i < 100; i++)
            {
                var exp = new Expense();
                exp.Value = -(decimal)rnd.Next(10, 150) + (decimal)rnd.Next(0, 100) / 100;
                exp.ClientName = "Grocery Store";
                exp.UsageText = "Grocery Shopping " + i;
                exp.Comment = "Demo Data";
                exp.Booked = DateTime.Now.AddDays(-rnd.Next(0, 365)).AddMinutes(rnd.Next(0, 60 * 24));
                exp.ExpenseType = tpGroceries;
                DB.Expense.Add(exp);
            }
            for (int i = 0; i < 14; i++)
            {
                var exp = new Expense();
                exp.Value = -69.69M;
                exp.ClientName = "Internet Provider";
                exp.UsageText = "Your Internet for this month";
                exp.Comment = "Demo Data";
                exp.Booked = DateTime.Now.AddMonths(-i).AddMinutes(rnd.Next(0, 60 * 24));
                exp.ExpenseType = tpInternet;
                DB.Expense.Add(exp);
            }
            for (int i = 0; i < 14; i++)
            {
                var exp = new Expense();
                exp.Value = 1234.56M;
                exp.ClientName = "Your Employer";
                exp.UsageText = "Salary";
                exp.Comment = "Demo Data";
                exp.Booked = DateTime.Now.AddMonths(-i).AddMinutes(rnd.Next(0, 60 * 24));
                exp.ExpenseType = tpIncome;
                DB.Expense.Add(exp);
            }
            for (int i = 0; i < 14; i++)
            {
                var exp = new Expense();
                exp.Value = -69.69M;
                exp.ClientName = "Internet Provider";
                exp.UsageText = "Your Internet for this month";
                exp.Comment = "Demo Data";
                exp.Booked = DateTime.Now.AddMonths(-i);
                exp.ExpenseType = tpInternet;
                DB.Expense.Add(exp);
            }
            for (int i = 0; i < 10; i++)
            {
                var exp = new Expense();
                exp.Value = -(decimal)rnd.Next(1, 5) + (decimal)rnd.Next(0, 100) / 100;
                exp.ClientName = "Credit Card Company";
                exp.UsageText = "Non-dubious booking " + i;
                exp.Comment = "Demo Data";
                exp.Booked = DateTime.Now.AddDays(-rnd.Next(0, 365)).AddMinutes(rnd.Next(0, 60 * 24));
                DB.Expense.Add(exp);
            }
            DB.SaveChanges();
            LoadData();
        }
        private void BTRemoveDemoData_Click(object sender, RoutedEventArgs e)
        {
            RemoveDemoData();
        }
        private void RemoveDemoData()
        {
            var res = MessageBox.Show("Are you sure you want to remove demo data?", "Remove Demo Data?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes)
                return;

            DB.Expense.Load();
            var remove = DB.Expense.Where(R => R.Comment == "Demo Data").ToList();
            DB.Expense.RemoveRange(remove);
            DB.SaveChanges();
            LoadData();
        }
        #endregion
    }
}
