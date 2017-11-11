using BuddhaBowls.Models;
using BuddhaBowls.Services;
using BuddhaBowls.UserControls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls
{
    public class AdminExpenseVM : ExpenseDetailVM
    {

        public AdminExpenseVM(PAndLSummarySection section, ExpenseItem item, PeriodMarker period, WeekMarker week):
            base(section, item, period, week)
        {
            _tabControl = new ExpenseDetailControl(this);
            Header = "Administrative Expenses";
        }

        protected override void InitSections()
        {
            ExpenseSections = new ObservableCollection<PAndLSummarySection>();
            ExpenseSections.Add(GetMiscSection());
            ExpenseSections.Add(GetOfficeSection());
            ExpenseSections.Add(GetComputerSection());
            ExpenseSections.Add(GetTaxSection());
            ExpenseSections.Add(GetDuesSection());
        }

        private PAndLSummarySection GetMiscSection()
        {
            return GetSumSection("Misc. Admin", new string[] { "Bank Charges", "Bookkeeping", "Cash Short (Over)", "Legal",
                                                               "Accounting Year End Service", "Leisure & Entertainment", "Gaucho Buck Fees",
                                                               "Utility Bill Preparation" });
        }

        private PAndLSummarySection GetOfficeSection()
        {
            return GetSumSection("Office Supplies", new string[] { "Postage", "POS Paper", "Printer Ink", "Lamination", "Miscallaneous",
                                                                   "Office Equipment" });
        }

        private PAndLSummarySection GetComputerSection()
        {
            return GetSumSection("Computer Software/Maintenance", new string[] { "IT Support", "Software", "Anil's Fees" });
        }

        private PAndLSummarySection GetTaxSection()
        {
            return GetSumSection("Taxes/Permits", new string[] { "Unsecured Property Tax", "Corporate Taxes", "Environmental Health Services Fee",
                                                                 "Outdoor Seating Permit" });
        }

        private PAndLSummarySection GetDuesSection()
        {
            return GetSumSection("Dues & Subscriptions", new string[] { "SquareSpace", "MailChimp", "Costco Membership", "Costco Cash Back" });
        }

    }
}
