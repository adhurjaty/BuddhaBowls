using BuddhaBowls.Models;
using BuddhaBowls.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BuddhaBowls
{
    public class ExpenseDetailVM : TempTabVM
    {
        protected PeriodMarker _period;
        protected WeekMarker _week;
        protected bool _newExpenses = true;
        protected ExpenseItem _item;
        protected ExpenseItemsContainer _itemsContainer;
        protected PAndLSummarySection _section;

        #region Content Binders

        private string _dateStr;
        public string DateStr
        {
            get
            {
                return _dateStr;
            }
            set
            {
                _dateStr = value;
                NotifyPropertyChanged("DateStr");
            }
        }

        private ObservableCollection<PAndLSummarySection> _expenseSections;
        public ObservableCollection<PAndLSummarySection> ExpenseSections
        {
            get
            {
                return _expenseSections;
            }
            set
            {
                _expenseSections = value;
                NotifyPropertyChanged("ExpenseSections");
            }
        }

        private float _totalWeek;
        public float TotalWeek
        {
            get
            {
                return _totalWeek;
            }
            set
            {
                _totalWeek = value;
                NotifyPropertyChanged("TotalWeek");
                NotifyPropertyChanged("TotalPeriod");
            }
        }

        protected float _totalPrevPeriod;
        public float TotalPeriod
        {
            get
            {
                return _totalPrevPeriod + _totalWeek;
            }
        }
        #endregion

        #region ICommand and CanExecute

        public ICommand SaveCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        #endregion

        public ExpenseDetailVM(PAndLSummarySection section, ExpenseItem item, PeriodMarker period, WeekMarker week) : base()
        {
            _period = period;
            _week = week;
            _section = section;
            _item = item;
            _itemsContainer = _models.EIContainer.Copy();
            DateStr = string.Format("Date: {0} - {1}", week.StartDate.ToString("MM/dd"), week.EndDate.ToString("MM/dd"));

            SaveCommand = new RelayCommand(SaveExpenses);
            CancelCommand = new RelayCommand(CancelExpenses);

            InitSections();
            CalculateTotals();
        }

        #region ICommand Helpers

        protected virtual void SaveExpenses(object obj)
        {
            _item.WeekSales = TotalWeek;
            _section.RefreshPercentages();
            _section.CommitChange();
            _item.Update();
            _models.EIContainer.SetItems(_itemsContainer.Items);

            foreach (PAndLSummarySection section in ExpenseSections)
            {
                if (_newExpenses)
                {
                    section.Insert();
                    _models.EIContainer.AddItems(section.Summaries.ToList());
                }
                else
                {
                    section.Update();
                    _models.EIContainer.UpdateMultiple(section.Summaries.Where(x => x.Id != -1));
                }
            }
            Close();
        }

        protected virtual void CancelExpenses(object obj)
        {
            Close();
        }

        #endregion

        protected PAndLSummarySection GetSumSection(string expenseType, string[] labels)
        {
            List<ExpenseItem> expenses = _itemsContainer.Items.Where(x => x.Date == _week.StartDate && x.ExpenseType == expenseType).ToList();
            _newExpenses = expenses.Count == 0;
            if (_newExpenses)
            {
                List<ExpenseItem> prevExpenses = _itemsContainer.Items.Where(x => x.Date >= _period.StartDate && x.Date < _week.StartDate &&
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

        protected virtual void InitSections() { }

        public virtual void EditedItem(PAndLSummarySection section, ExpenseItem item)
        {
            section.UpdateItem(item);
            section.CommitChange();
            CalculateTotals();
        }

        protected virtual void CalculateTotals()
        {
            _totalPrevPeriod = ExpenseSections.SelectMany(x => x.GetItemsNotTotals()).Sum(x => x.PrevPeriodSales);
            TotalWeek = ExpenseSections.SelectMany(x => x.GetItemsNotTotals()).Sum(x => x.WeekSales);
        }
    }
}
