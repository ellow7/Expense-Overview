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
        public Main()
        {
            InitializeComponent();
            DGExpenses.AutoGenerateColumns = false;
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            var db = new ExpenseDBModel();

            var typ = db.ExpenseTypes.Where(R => R.Name == "Holiday").FirstOrDefault() ?? new ExpenseTypes("Holiday", "");

            var exp = new Expenses();
            exp.Value = 500.00M;
            exp.ClientName = "Peter Pan Holidays";
            exp.BookingText = "Neverland Holiday 2022";
            exp.Booked = DateTime.Now.AddDays(-5);
            exp.ExpenseTypes = typ;
            db.Expenses.Add(exp);
            db.SaveChanges();

            db.Expenses.Load();
            DGExpenses.ItemsSource = db.Expenses.Local.OrderByDescending(R=>R.Booked);
        }
    }
}
