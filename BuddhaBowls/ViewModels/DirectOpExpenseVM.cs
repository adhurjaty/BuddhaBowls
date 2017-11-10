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
        private PeriodMarker _period;

        public DirectOpExpenseVM(PAndLSummarySection section, ExpenseItem item, PeriodMarker period, WeekMarker week) : base(section, item, week)
        {
            _tabControl = new ExpenseDetailControl(this);
            Header = "Direct Operating Expenses";

            _period = period;

            InitSections();
            CalculateTotals();
        }

        protected override void SaveExpenses(object obj)
        {
            _item.WeekSales = TotalWeek;
            _section.RefreshPercentages();
            _section.CommitChange();
            _section.Update();
            base.SaveExpenses(obj);
        }

        private void InitSections()
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
            return GetSumSection("Janitorial & Chemical", new string[] { "Monthly Cleaning", "Cleaning tools and supplies", "Chemicals" });
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
            return GetSumSection("Misc.", new string[] { "Kitchen Supplies", "Freight & Delivery", "License & Permits",
                                                         "Telephone & Internet", "Liability Insurance", "Other Direct Operating Expenses" });
        }

        private PAndLSummarySection GetSumSection(string expenseType, string[] labels)
        {
            List<ExpenseItem> expenses = _models.EIContainer.Items.Where(x => x.Date == _week.StartDate && x.ExpenseType == expenseType).ToList();
            _newExpenses = expenses.Count == 0;
            if (_newExpenses)
            {
                List<ExpenseItem> prevExpenses = _models.EIContainer.Items.Where(x => x.Date >= _period.StartDate && x.Date < _week.StartDate &&
                                                                             x.ExpenseType == expenseType).OrderByDescending(x => x.Date).ToList();
                foreach (string label in labels)
                {
                    ExpenseItem newItem = new ExpenseItem(expenseType, label, _week.StartDate);
                    List<ExpenseItem> existingItems = prevExpenses.Where(x => x.Name == label).ToList();
                    if (existingItems.Count > 0)
                    {
                        ExpenseItem newestExisting = existingItems.First();
                        newItem.WeekSales = newestExisting.WeekSales;

                        // if there are missing previous records (very likely) just fill them in with the latest value
                        newItem.PrevPeriodSales = existingItems.Sum(x => x.WeekSales);
                        if (existingItems.Count < _week.Period - 1)
                            newItem.PrevPeriodSales += (_week.Period - 1 - existingItems.Count) * newestExisting.WeekSales;
                    }
                    expenses.Add(newItem);
                }
            }

            // add a total row at the end
            expenses.Add(new ExpenseItem(expenseType, "Total " + expenseType, _week.StartDate)
            {
                WeekSales = expenses.Sum(x => x.WeekSales),
                PrevPeriodSales = expenses.Sum(x => x.PrevPeriodSales)
            });

            PAndLSummarySection section = new PAndLSummarySection(expenseType, _week.Period, expenses, new ExpenseItem(), true);
            section.SetTotalRows(new List<string>() { "Total " + expenseType });
            return section;
        }
    }
}
