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
    public class DirectOpExpenseVM : ExpenseDetailVM
    {

        public DirectOpExpenseVM(PAndLSummarySection section, ExpenseItem item, PeriodMarker period, WeekMarker week):
            base(section, item, period, week)
        {
            _tabControl = new ExpenseDetailControl(this);
            Header = "Direct Operating Expenses";
        }

        protected override void InitSections()
        {
            ExpenseSections = new ObservableCollection<PAndLSummarySection>();
            ExpenseSections.Add(GetUtilitiesSection());
            ExpenseSections.Add(GetJanitorialSection());
            ExpenseSections.Add(GetLaundrySection());
            ExpenseSections.Add(GetMusicSection());
            ExpenseSections.Add(GetContractSection());
            ExpenseSections.Add(GetMiscSection());
        }

        private PAndLSummarySection GetUtilitiesSection()
        {
            return GetSumSection("Utilities", new string[] { "Electric", "Water", "Gas" });
        }

        private PAndLSummarySection GetJanitorialSection()
        {
            return GetSumSection("Janitorial & Chemical", new string[] { "Monthly Cleaning", "Cleaning Tools & Supplies", "Chemicals" });
        }

        private PAndLSummarySection GetLaundrySection()
        {
            return GetSumSection("Laundry & Linen", new string[] { "Operating Linen" });
        }

        private PAndLSummarySection GetMusicSection()
        {
            return GetSumSection("Music & Entertainment", new string[] { "Pandora", "Spotify" });
        }

        private PAndLSummarySection GetContractSection()
        {
            return GetSumSection("Contract Services", new string[] { "Pest Control", "Hood Cleaning", "Knife Sharpening", "Fire Suppression Check" });
        }

        private PAndLSummarySection GetMiscSection()
        {
            return GetSumSection("Misc. Operating", new string[] { "Kitchen Supplies", "Freight & Delivery", "License & Permits",
                                                                   "Telephone & Internet", "Liability Insurance",
                                                                   "Other Direct Operating Expenses" });
        }
    }
}
