//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Expense_Overview
{
    using System;
    using System.Collections.Generic;
    
    public partial class Expenses
    {
        public int Id { get; set; }
        public Nullable<System.DateTime> Created { get; set; }
        public Nullable<System.DateTime> Booked { get; set; }
        public string ClientName { get; set; }
        public string BookingText { get; set; }
        public string UsageText { get; set; }
        public Nullable<decimal> Value { get; set; }
        public string Currency { get; set; }
        public string Comment { get; set; }
        public Nullable<int> TypeID { get; set; }
        public string ImportText { get; set; }
    
        public virtual ExpenseTypes ExpenseTypes { get; set; }
    }
}