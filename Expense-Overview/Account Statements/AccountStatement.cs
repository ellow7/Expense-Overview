using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Expense_Overview.Account_Statements
{
    public interface AccountStatement
    {
        List<Expense> Expenses { get; }
        string ImportExtension { get; }
        bool ReadImport();
        List<Tuple<Expense, bool>> CheckForDuplicates(List<Expense> CurrentData);
    }
}
