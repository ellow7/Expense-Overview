using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using System.Globalization;

namespace Expense_Overview.Account_Statements
{
    public class DibaStatement : AccountStatement
    {
        private List<Expense> expenses;
        public List<Expense> Expenses { get { return expenses; } }
        public string ImportExtension { get { return ".csv"; } }
        public bool ReadImport()
        {
            expenses = new List<Expense>();
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                ofd.RestoreDirectory = true;
                ofd.Filter = $"DIBA Import (*{ImportExtension})|*{ImportExtension}";

                if (ofd.ShowDialog() ?? false && new FileInfo(ofd.FileName).Exists)
                {
                    var now = DateTime.Now;
                    #region If we used some kind of excel format...
                    //var wb = new XLWorkbook(ofd.FileName);
                    //var ws = wb.Worksheets.First();

                    //expenses = new List<Expense>();

                    //int startRow = 14;
                    //int startCol = 1;
                    //int colCounter = 1;
                    //int rowCounter = 0;

                    //#region Check format
                    //List<string> columnHeaders = new List<string> { "Buchung", "Valuta", "Auftraggeber/Empfänger", "Buchungstext", "Verwendungszweck", "Saldo", "Währung", "Betrag", "Währung" };
                    //foreach (var header in columnHeaders)
                    //{
                    //    var cell = ws.Cell(startRow, startCol + colCounter++);
                    //    if (cell.Value.ToString() != header)
                    //        throw new Exception($"Column '{header}' not found. Found instead: '{cell.Value}'");
                    //}
                    //#endregion

                    //while (ws.Cell(startRow + rowCounter++, 1).Value.ToString() != "")//while we have bookings
                    //{
                    //    var exp = new Expense();
                    //    exp.Created = ws.Cell(startRow + rowCounter, 1).GetDateTime();
                    //    exp.Booked = ws.Cell(startRow + rowCounter, 2).GetDateTime();
                    //    exp.Imported = now;
                    //    exp.ClientName = ws.Cell(startRow + rowCounter, 3).ToString();
                    //    exp.BookingText = ws.Cell(startRow + rowCounter, 4).ToString();
                    //    exp.UsageText = ws.Cell(startRow + rowCounter, 5).ToString();
                    //    exp.Value = (decimal)ws.Cell(startRow + rowCounter, 7).GetDouble();
                    //    exp.Currency = ws.Cell(startRow + rowCounter, 6).ToString();
                    //    exp.Comment = "";
                    //    exp.ImportText = ws.Row(rowCounter).ToString();

                    //    expenses.Add(exp);
                    //}
                    #endregion
                    List<DIBAStatementFormat> records;
                    using (var reader = new StreamReader(ofd.FileName, encoding: Encoding.Default))
                    {
                        for (int i = 0; i < 13; i++)
                            reader.ReadLine();//skip 13 lines
                        using (var csv = new CsvReader(reader, CultureInfo.GetCultureInfo("de-DE")))
                        {
                            records = csv.GetRecords<DIBAStatementFormat>().ToList();
                            //var test = records.FirstOrDefault();
                            //var test2 = records.Count();
                        }
                    }

                    foreach (var rec in records)
                    {
                        var exp = new Expense();
                        exp.Created = rec.Buchung;
                        exp.Booked = rec.Valuta;
                        exp.Imported = now;
                        exp.ClientName = rec.Auftraggeber;
                        exp.BookingText = rec.Buchungstext;
                        exp.UsageText = rec.Verwendungszweck;
                        exp.Value = rec.Betrag;
                        exp.Currency = rec.WährungBetrag;
                        exp.Comment = "";
                        exp.ImportText = rec.ToString();
                        expenses.Add(exp);
                    }

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
    public class DIBAStatementFormat
    {
        public DateTime Buchung { get; set; }
        public DateTime Valuta { get; set; }
        [Name("Auftraggeber/Empfänger")]
        public string Auftraggeber { get; set; }
        public string Buchungstext { get; set; }
        public string Verwendungszweck { get; set; }
        public decimal Saldo { get; set; }
        [Name("Währung")]
        public string WährungSaldo { get; set; }
        public decimal Betrag { get; set; }
        [Name("Währung")]
        public string WährungBetrag { get; set; }

        public override string ToString()
        {
            string importString = "";
            var properties = this.GetType().GetProperties();
            foreach (var prop in properties)
                importString += prop.GetValue(this).ToString() + ";";
            return importString;
        }
    }
}
