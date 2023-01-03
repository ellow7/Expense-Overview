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
        Expense InitialExpense;
        public DeprecitationDialog(Expense InitialExpense)
        {
            InitializeComponent();
            this.InitialExpense = InitialExpense;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TBDuration.Text = "12";
            TBDuration_TextChanged(this, null);
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
            LBExpenseValue.Content = InitialExpense.Value.ToString("0.00") + InitialExpense.Currency;
            LBExpenseBooked.Content = InitialExpense.Booked.ToString("yyyy-MM-dd");

            try
            {
                LBExpenseMonthlyValue.Content = Math.Round(InitialExpense.Value / duration).ToString("0.00") + InitialExpense.Currency;
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
