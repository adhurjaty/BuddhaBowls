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
        protected WeekMarker _week;
        protected bool _newExpenses = true;

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

        public ExpenseDetailVM(WeekMarker week) : base()
        {
            Header = "Direct Operating Expenses";
            _week = week;
            DateStr = string.Format("Date: {0} - {1}", week.StartDate.ToString("MM/dd"), week.EndDate.ToString("MM/dd"));

            SaveCommand = new RelayCommand(SaveExpenses);
            CancelCommand = new RelayCommand(CancelExpenses);
        }

        #region ICommand Helpers

        protected virtual void SaveExpenses(object obj)
        {
            foreach (PAndLSummarySection section in ExpenseSections)
            {
                if (_newExpenses)
                {
                    section.Insert();
                    _models.ExpenseItems.AddRange(section.Summaries);
                }
                else
                {
                    section.Update();
                    foreach (ExpenseItem item in section.Summaries)
                    {
                        int idx = _models.ExpenseItems.FindIndex(x => x.Id == item.Id);
                        _models.ExpenseItems[idx] = item;
                    }
                }
            }
            Close();
        }

        protected virtual void CancelExpenses(object obj)
        {
            Close();
        }

        #endregion

        public virtual void EditedItem(PAndLSummarySection section, ExpenseItem item)
        {
            section.CommitChange();
            CalculateTotals();
        }

        protected virtual void CalculateTotals()
        {
            _totalPrevPeriod = ExpenseSections.SelectMany(x => x.Summaries).Sum(x => x.PrevPeriodSales);
            TotalWeek = ExpenseSections.SelectMany(x => x.Summaries).Sum(x => x.WeekSales);
        }
    }
}
