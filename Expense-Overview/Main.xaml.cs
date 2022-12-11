using DocumentFormat.OpenXml.Bibliography;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
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
            LoadExpenses(DPStartDate.SelectedDate.Value, DPEndDate.SelectedDate.Value);
        }
        private void LoadExpenses (DateTime start, DateTime end)
        {
            //DGExpenses.ItemsSource = DB.Expense.Where(R => R.Booked >= start && R.Booked <= end);//not working
            //DGExpenses.ItemsSource = null;
            DGExpenses.ItemsSource = DB.Expense.Where(R => R.Booked >= start && R.Booked <= end).OrderByDescending(R=>R.Booked).ToList();

            DGExpenses.Columns.FirstOrDefault().SortDirection = System.ComponentModel.ListSortDirection.Descending;

            DGCBCTypeSelector.ItemsSource = DB.ExpenseType.Select(R=>R.Name).ToList();
        }
        private void BTSaveExpenses_Click(object sender, RoutedEventArgs e)
        {
        }

        private void BTLoadExpenses_Click(object sender, RoutedEventArgs e)
        {
            LoadExpenses();
        }
        #endregion

        #region Import
        #endregion

    }
}
