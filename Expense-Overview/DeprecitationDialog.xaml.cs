using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace Expense_Overview
{
    /// <summary>
    /// Interaction logic for DeprecitationDialog.xaml
    /// </summary>
    public partial class DeprecitationDialog : Window
    {
        public int DurationMonths = 0;
        public decimal ExpenseValue = 0;
        Expense InitialExpense;
        
        public DeprecitationDialog(Expense InitialExpense)//new deprecitation, allow editing
        {
            InitializeComponent();
            this.Title = "Create Deprecitation";
            this.InitialExpense = InitialExpense;
            this.ExpenseValue = InitialExpense.Value;//we need this, because when editing an existing deprecitation, the value would be the deprecitation value

            TBDuration.Text = "12";
            TBDuration_TextChanged(this, null);//run first calculation
        }
        public DeprecitationDialog(Deprecitation Deprecitation)//existsing derprecitation, just view and prevent editing
        {
            InitializeComponent();
            this.Title = "Show Deprecitation";
            this.InitialExpense = Deprecitation.InitialExpense;
            this.ExpenseValue = Deprecitation.Value;

            TBDuration.Text = Deprecitation.DurationMonths.ToString();
            TBDuration_TextChanged(this, null);//run calculation

            //prevent editing
            TBDuration.IsEnabled = false;
            BTOkay.IsEnabled = false;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void BTCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DurationMonths = 0;
            this.DialogResult = false;
        }

        private void BTOkay_Click(object sender, RoutedEventArgs e)
        {
            this.DurationMonths = int.Parse(TBDuration.Text);
            this.DialogResult = true;
        }

        private void TBDuration_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out int duration);//only allow numeric
        }

        private void TBDuration_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (InitialExpense == null)
                return;

            int duration;
            int.TryParse(TBDuration.Text ?? "0", out duration);

            LBExpenseName.Content = InitialExpense.UsageText;
            LBExpenseClientName.Content = InitialExpense.ClientName;
            LBExpenseValue.Content = ExpenseValue.ToString("0.00") + InitialExpense.Currency;
            LBExpenseBooked.Content = InitialExpense.Booked.ToString("yyyy-MM-dd");

            try
            {
                LBExpenseMonthlyValue.Content = Math.Round(ExpenseValue / duration).ToString("0.00") + InitialExpense.Currency;
                LBExpenseDeprecitationEndDate.Content = new DateTime(InitialExpense.Booked.Year, InitialExpense.Booked.Month, 1).AddMonths(duration).ToString("yyyy-MM-dd");
            }
            catch
            {
                LBExpenseMonthlyValue.Content = "";
                LBExpenseDeprecitationEndDate.Content = "";
            }
        }
    }
}
