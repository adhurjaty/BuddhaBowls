using BuddhaBowls.Helpers;
using BuddhaBowls.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace BuddhaBowls
{
    public delegate void WeekChange(PeriodMarker period, WeekMarker week);

    public class PeriodSelectorVM : INotifyPropertyChanged
    {
        private WeekChange OnChangeWeek;
        private bool _hasShowAll;

        // INotifyPropertyChanged event and method
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Content Binders

        private List<PeriodMarker> _periodList;
        public List<PeriodMarker> PeriodList
        {
            get
            {
                return _periodList;
            }
            set
            {
                _periodList = value;
                NotifyPropertyChanged("PeriodList");
            }
        }

        private PeriodMarker _selectedPeriod;
        public PeriodMarker SelectedPeriod
        {
            get
            {
                return _selectedPeriod;
            }
            set
            {
                PeriodMarker oldPeriod = _selectedPeriod;
                _selectedPeriod = value;
                NotifyPropertyChanged("SelectedPeriod");

                if (_selectedPeriod != null && !(oldPeriod != null && oldPeriod.Period == _selectedPeriod.Period))
                {
                    if (_selectedPeriod.Period == -1)
                        WeekList = new List<WeekMarker>() { (WeekMarker)_selectedPeriod };
                    else
                        WeekList = MainHelper.GetWeekLabels(SelectedPeriod).ToList();
                    SelectedWeek = WeekList[0];
                }
            }
        }

        private List<WeekMarker> _weekList;
        public List<WeekMarker> WeekList
        {
            get
            {
                return _weekList;
            }
            set
            {
                _weekList = value;
                NotifyPropertyChanged("WeekList");
            }
        }

        private WeekMarker _selectedWeek;
        public WeekMarker SelectedWeek
        {
            get
            {
                return _selectedWeek;
            }
            set
            {
                if (value != null && value != _selectedWeek)
                    OnChangeWeek(SelectedPeriod, value);
                _selectedWeek = value;
                NotifyPropertyChanged("SelectedWeek");
            }
        }

        private List<int> _years;
        public List<int> Years
        {
            get
            {
                return _years;
            }
            set
            {
                _years = value;
                NotifyPropertyChanged("Years");
            }
        }

        private int _selectedYear;
        public int SelectedYear
        {
            get
            {
                return _selectedYear;
            }
            set
            {
                if (value != _selectedYear)
                {
                    ChangeYear(value);
                    OnChangeWeek(SelectedPeriod, SelectedWeek);
                }
                _selectedYear = value;
                NotifyPropertyChanged("SelectedYear");
            }
        }

        private Visibility _curWeekVisibility = Visibility.Visible;
        public Visibility CurWeekVisibility
        {
            get
            {
                return _curWeekVisibility;
            }
            set
            {
                _curWeekVisibility = value;
                NotifyPropertyChanged("CurWeekVisibility");
            }
        }
        #endregion

        public ICommand CurWeekCommand { get; set; }

        public PeriodSelectorVM(DBCache models, WeekChange onChangeWeek, bool hasShowAll = true)
        {
            OnChangeWeek = onChangeWeek;
            _hasShowAll = hasShowAll;
            Years = Enumerable.Range(2016, DateTime.Today.Year - 2016 + 1).ToList();
            GoToCurWeek(null);

            CurWeekCommand = new RelayCommand(GoToCurWeek);
        }

        private void GoToCurWeek(object obj)
        {
            // temporarily disable on change delegate so it does not get fired twice
            WeekChange tempOnChage = OnChangeWeek;
            OnChangeWeek = DummyOnChange;
            SelectedYear = DateTime.Today.Year;
            SelectedPeriod = PeriodList.FirstOrDefault(x => x.StartDate < DateTime.Now && DateTime.Now <= x.EndDate);
            SelectedWeek = WeekList.FirstOrDefault(x => x.StartDate < DateTime.Now && DateTime.Now <= x.EndDate);
            OnChangeWeek = tempOnChage;
            OnChangeWeek(SelectedPeriod, SelectedWeek);
        }

        private void ChangeYear(int year)
        {
            if (_hasShowAll)
                PeriodList = MainHelper.GetPeriodLabels(year).Concat(new List<PeriodMarker>() { new ShowAllMarker() }).ToList();
            else
                PeriodList = MainHelper.GetPeriodLabels(year).ToList();
            // temporarily disable on change delegate so it does not get fired twice
            WeekChange tempOnChage = OnChangeWeek;
            OnChangeWeek = DummyOnChange;
            SelectedPeriod = PeriodList.First();
            SelectedWeek = WeekList.First();
            OnChangeWeek = tempOnChage;
            NotifyPropertyChanged("PeriodList");
        }

        private void DummyOnChange(PeriodMarker period, WeekMarker week) { }

    }
}
