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
    public class AdvertisingExpenseVM : ExpenseDetailVM
    {

        public AdvertisingExpenseVM(PAndLSummarySection section, ExpenseItem item, PeriodMarker period, WeekMarker week):
            base(section, item, period, week)
        {
            _tabControl = new ExpenseDetailControl(this);
            Header = "Advertising & Promotion";
        }

        protected override void InitSections()
        {
            ExpenseSections = new ObservableCollection<PAndLSummarySection>();
            ExpenseSections.Add(GetAdsSection());
            ExpenseSections.Add(GetStickersSection());
            ExpenseSections.Add(GetDonationsSection());
            ExpenseSections.Add(GetPromotionSection());
        }

        private PAndLSummarySection GetAdsSection()
        {
            return GetSumSection("Advertising", new string[] { "Yelp", "Cooking Experiments", "Graphic Design", "Screen Printing",
                                                               "Punch Cards", "Doorhangers/Fliers", "Stamps/Ink", "Gift Cards",
                                                               "Santa Barbara Axxess" });
        }

        private PAndLSummarySection GetStickersSection()
        {
            return GetSumSection("Stickers", new string[] { "Pot Leaf", "Logo", "Pack a Bowl" });
        }

        private PAndLSummarySection GetDonationsSection()
        {
            return GetSumSection("Donations", new string[] { "Donations" });
        }

        private PAndLSummarySection GetPromotionSection()
        {
            return GetSumSection("Promotion", new string[] { "1$ Off Student Discount", "T-Shirt Give Away's", "Restaurant.com",
                                                             "Punch Card Buddha Bowl", "420 Bowls", "Gift Card Trade" });
        }
    }
}
