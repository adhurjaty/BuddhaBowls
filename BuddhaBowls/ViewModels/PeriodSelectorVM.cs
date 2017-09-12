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
        private DBCache _models;
        private WeekChange OnChangeWeek;

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
                        WeekList = _models.GetWeekLabels(SelectedPeriod.Period).ToList();
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
            _models = models;
            OnChangeWeek = onChangeWeek;

            PeriodList = _models.GetPeriodLabels().ToList();
            if(hasShowAll)
                PeriodList.Add(new ShowAllMarker());

            GoToCurWeek(null);

            CurWeekCommand = new RelayCommand(GoToCurWeek);
        }

        private void GoToCurWeek(object obj)
        {
            SelectedPeriod = PeriodList.FirstOrDefault(x => x.StartDate < DateTime.Now && DateTime.Now <= x.EndDate);
            SelectedWeek = WeekList.FirstOrDefault(x => x.StartDate < DateTime.Now && DateTime.Now <= x.EndDate);
        }
    }
}
