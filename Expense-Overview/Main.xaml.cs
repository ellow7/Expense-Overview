using ClosedXML.Excel;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing.Charts;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
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
            DGExpenses.AutoGenerateColumns = false;
        }
        private void insertDemoData()
        {
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
            for (int i = 0; i < 10; i++)
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
            insertDemoData();

            var today = DateTime.Today;
            var firstDay = new DateTime(today.Year, today.Month, 1);
            var firstDayNextMonth = firstDay.AddMonths(1);
            var lastDay = firstDayNextMonth.AddDays(-1);
            DPStartDate.SelectedDate = firstDay;
            DPEndDate.SelectedDate = lastDay;

            LoadExpenses();
        }

        #region Expenses
        private void LoadExpenses()
        {
            LoadExpenses(DPStartDate.SelectedDate ?? DateTime.MinValue, DPEndDate.SelectedDate ?? DateTime.MaxValue);
        }
        private void LoadExpenses(DateTime start, DateTime end)
        {
            DB.Expense.Load();
            bool searchWithTextFilter = TBSearch.Text != "";
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

            DGExpenses.Columns.FirstOrDefault().SortDirection = System.ComponentModel.ListSortDirection.Descending;
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
                        R.ExpenseTypeName
                    }).ToList();
                var header = data.FirstOrDefault().GetType().GetProperties().Select(R => R.Name);

                //Set values
                var headerCells = ws.Cell(1, 1).InsertData(header, true);
                var dataCells = ws.Cell(2,1).InsertData(data);

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
            LoadExpenses();
        }
        #endregion

        #region Import
        #endregion

    }
}
