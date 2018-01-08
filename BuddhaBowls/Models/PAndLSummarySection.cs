using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Models
{
    public class PAndLSummarySection : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string SummaryType { get; set; }
        public int WeekNumber { get; set; }
        public bool CanEdit { get; set; }
        protected List<string> _totalRows = new List<string>();

        private ObservableCollection<ExpenseItem> _summaries;
        public ObservableCollection<ExpenseItem> Summaries
        {
            get
            {
                return _summaries;
            }
            set
            {
                _summaries = value;
                NotifyPropertyChanged("Summaries");
            }
        }
        public ExpenseItem TotalSalesItem { get; set; }

        public PAndLSummarySection(string sumType, int weekNum, IEnumerable<ExpenseItem> items, ExpenseItem totalSalesItem, bool canEdit = false)
        {
            SummaryType = sumType;
            WeekNumber = weekNum;
            Summaries = new ObservableCollection<ExpenseItem>(items);
            foreach (ExpenseItem item in Summaries)
            {
                item.ExpenseType = sumType;
            }

            TotalSalesItem = totalSalesItem;
            CanEdit = canEdit;
        }

        public virtual void CommitChange()
        {
            Summaries = new ObservableCollection<ExpenseItem>(Summaries);
        }

        public virtual void UpdateItem(ExpenseItem item)
        {
            UpdateTotal();
            if (!_totalRows.Contains(item.Name))
                RefreshPercentages();
        }

        public void RefreshPercentages()
        {
            foreach (ExpenseItem item in Summaries)
            {
                if (TotalSalesItem.WeekSales != 0)
                    item.WeekPSales = item.WeekSales / TotalSalesItem.WeekSales;
                else
                    item.WeekPSales = 0;

                if (TotalSalesItem.WeekBudget != 0)
                    item.WeekPBudget = item.WeekBudget / TotalSalesItem.WeekBudget;
                else
                    item.WeekPBudget = 0;

                if (TotalSalesItem.PeriodSales != 0)
                    item.PeriodPSales = item.PeriodSales / TotalSalesItem.PeriodSales;
                else
                    item.PeriodPSales = 0;

                if (TotalSalesItem.PeriodBudget != 0)
                    item.PeriodPBudget = item.PeriodBudget / TotalSalesItem.PeriodBudget;
                else
                    item.PeriodPBudget = 0;
            }
        }

        public void Insert()
        {
            foreach (ExpenseItem item in Summaries)
            {
                if(!_totalRows.Contains(item.Name))
                    item.Insert();
            }
        }

        /// <summary>
        /// Updates or inserts the expense items in the section
        /// </summary>
        public void Update()
        {
            foreach (ExpenseItem item in Summaries)
            {
                if (!_totalRows.Contains(item.Name))
                {
                    if (item.Id == -1)
                        item.Insert();
                    else
                        item.Update();
                }
            }
        }

        public void Update(List<ExpenseItem> existingItems)
        {
            foreach (ExpenseItem item in Summaries)
            {
                int idx = existingItems.FindIndex(x => x.Name == item.Name && x.ExpenseType == item.ExpenseType && x.Date.Date == item.Date.Date);
                if (idx != -1)
                {
                    item.Id = existingItems[idx].Id;
                    existingItems[idx] = item;
                    item.Update();
                }
            }
        }

        public virtual void UpdateTotal()
        {
            ExpenseItem totalItem = Summaries.FirstOrDefault(x => _totalRows.Contains(x.Name));
            totalItem.WeekSales = Summaries.Where(x => x != totalItem).Sum(x => x.WeekSales);
        }

        public void SetTotalRows(List<string> rows)
        {
            _totalRows = rows;
        }

        public List<ExpenseItem> GetItemsNotTotals()
        {
            return Summaries.Where(x => !_totalRows.Contains(x.Name)).ToList();
        }
    }

    public class SalesPAndL : PAndLSummarySection
    {
        public ExpenseItem FoodTotal { get; set; }

        public SalesPAndL(int weekNum, IEnumerable<ExpenseItem> items) : base("Sales", weekNum, items, items.FirstOrDefault(x => x.Name == "Total"))
        {
            if (TotalSalesItem == null)
            {
                TotalSalesItem = new ExpenseItem("Sales", "Total", DateTime.Now);
                UpdateTotal();
            }
            FoodTotal = items.FirstOrDefault(x => x.Name == "Food");
            _totalRows = new List<string>() { "Total" };
        }

        //public override void UpdateItem(ExpenseItem item)
        //{
        //    UpdateTotal();
        //    if (item.Name != "Total")
        //    {
        //        base.UpdateItem(item);
        //    }
        //}

        public override void UpdateTotal()
        {
            TotalSalesItem.WeekSales = Summaries.Take(Summaries.Count - 1).Sum(x => x.WeekSales);
            TotalSalesItem.WeekBudget = Summaries.Take(Summaries.Count - 1).Sum(x => x.WeekBudget);
        }
    }

    public class CogsPAndL : PAndLSummarySection
    {

        public CogsPAndL(int weekNum, IEnumerable<ExpenseItem> items, ExpenseItem totalSales) : base("Cost of Sales", weekNum, items, totalSales)
        {
            _totalRows = new List<string>() { "Total", "Gross Profit" };
        }

        //public override void UpdateItem(ExpenseItem item)
        //{
        //    UpdateTotal();
        //    if(item.Name != "Total" && item.Name != "Gross Profit")
        //        base.UpdateItem(item);
        //}

        public override void UpdateTotal()
        {
            ExpenseItem totalItem = Summaries.First(x => x.Name == "Total");
            ExpenseItem profitItem = Summaries.First(x => x.Name == "Gross Profit");
            List<ExpenseItem> otherItems = Summaries.Where(x => !_totalRows.Contains(x.Name)).ToList();
            totalItem.WeekSales = otherItems.Sum(x => x.WeekSales);
            totalItem.WeekBudget = otherItems.Sum(x => x.WeekBudget);
            profitItem.WeekSales = TotalSalesItem.WeekSales - totalItem.WeekSales;
            profitItem.WeekBudget = TotalSalesItem.WeekBudget - totalItem.WeekBudget;
        }
    }

    public class PayrollPAndL : PAndLSummarySection
    {
        private ExpenseItem _totalCogsItem;

        public PayrollPAndL(int weekNum, IEnumerable<ExpenseItem> items, ExpenseItem totalSales, ExpenseItem totalCogs)
            : base("Payroll", weekNum, items, totalSales, true)
        {
            _totalCogsItem = totalCogs;
            _totalRows = new List<string>() { "Total Payroll", "Prime Cost", "Profit after Prime Cost" };
        }

        //public override void UpdateItem(ExpenseItem item)
        //{
        //    UpdateTotal();
        //    if(!new string[] { "Total Payroll", "Prime Cost", "Profit after Prime Cost" }.Contains(item.Name))
        //        base.UpdateItem(item);
        //}

        public override void UpdateTotal()
        {
            ExpenseItem totalItem = Summaries.First(x => x.Name == "Total Payroll");
            ExpenseItem primeCostItem = Summaries.First(x => x.Name == "Prime Cost");
            ExpenseItem profitItem = Summaries.First(x => x.Name == "Profit after Prime Cost");
            List<ExpenseItem> otherItems = Summaries.Where(x => !_totalRows.Contains(x.Name)).ToList();
            totalItem.WeekSales = otherItems.Sum(x => x.WeekSales);
            totalItem.WeekBudget = otherItems.Sum(x => x.WeekBudget);
            primeCostItem.WeekSales = totalItem.WeekSales + _totalCogsItem.WeekSales;
            primeCostItem.WeekBudget = totalItem.WeekBudget + _totalCogsItem.WeekBudget;
            profitItem.WeekSales = TotalSalesItem.WeekSales - primeCostItem.WeekSales;
            profitItem.WeekBudget = TotalSalesItem.WeekBudget - primeCostItem.WeekBudget;
        }
    }

    public class OverheadPAndL : PAndLSummarySection
    {

        public OverheadPAndL(int weekNum, IEnumerable<ExpenseItem> items, ExpenseItem totalSales)
            : base("Overhead Expense", weekNum, items, totalSales, true)
        {
            _totalRows = new List<string>() { "Total Overhead Expense" };
        }

        //public override void UpdateItem(ExpenseItem item)
        //{
        //    UpdateTotal();
        //    if(!item.Name.Contains("Total"))
        //        base.UpdateItem(item);
        //}

        public override void UpdateTotal()
        {
            ExpenseItem totalItem = Summaries.FirstOrDefault(x => _totalRows.Contains(x.Name));
            if (totalItem == null)
            {
                totalItem = new ExpenseItem(SummaryType, _totalRows[0], Summaries[0].Date);
            }
            totalItem.WeekSales = Summaries.Take(Summaries.Count - 1).Sum(x => x.WeekSales);
            totalItem.WeekBudget = Summaries.Take(Summaries.Count - 1).Sum(x => x.WeekBudget);
        }
    }

    public class TakeawayPAndL : PAndLSummarySection
    {
        private ExpenseItem _payrollPrime;
        private ExpenseItem _totalOverhead;

        public TakeawayPAndL(int weekNum, IEnumerable<ExpenseItem> items, ExpenseItem totalSales,
                                          ExpenseItem payrollPrime, ExpenseItem totalOverhead) : base("Takeaway", weekNum, items, totalSales, true)
        {
            TotalSalesItem = totalSales;
            _payrollPrime = payrollPrime;
            _totalOverhead = totalOverhead;
            _totalRows = new List<string>() { "Net Operating Income" };
        }

        //public override void UpdateItem(ExpenseItem item)
        //{
        //    UpdateTotal();
        //    if(item.Name != "Net Operating Income")
        //        base.UpdateItem(item);
        //}

        public override void UpdateTotal()
        {
            ExpenseItem totalItem = Summaries.FirstOrDefault(x => _totalRows.Contains(x.Name));
            if(totalItem == null)
            {
                totalItem = new ExpenseItem(SummaryType, _totalRows[0], Summaries[0].Date);
            }
            List<ExpenseItem> sumItems = Summaries.Where(x => x != totalItem).Concat(new List<ExpenseItem>()
            {
                _payrollPrime, _totalOverhead
            }).ToList();
            totalItem.WeekSales = TotalSalesItem.WeekSales - sumItems.Sum(x => x.WeekSales);
            totalItem.WeekBudget = TotalSalesItem.WeekBudget - sumItems.Sum(x => x.WeekBudget);
        }
    }
}
