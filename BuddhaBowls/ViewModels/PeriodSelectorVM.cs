using BuddhaBowls.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls
{
    public delegate void WeekChange(DateTime start, DateTime end);

    public class PeriodSelectorVM : INotifyPropertyChanged
    {
        private ModelContainer _models;
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

                if (_selectedPeriod != null)
                {
                    WeekList = _models.GetWeekLabels(SelectedPeriod.Period).ToList();
                    if (!(oldPeriod != null && oldPeriod.Period == _selectedPeriod.Period))
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
                _selectedWeek = value;
                NotifyPropertyChanged("SelectedWeek");
                if (_selectedWeek != null)
                    OnChangeWeek(_selectedWeek.StartDate, _selectedWeek.EndDate);
            }
        }

        #endregion

        public PeriodSelectorVM(ModelContainer models, WeekChange onChangeWeek)
        {
            _models = models;
            OnChangeWeek = onChangeWeek;

            PeriodList = _models.GetPeriodLabels().ToList();
            SelectedPeriod = PeriodList.FirstOrDefault(x => x.StartDate < DateTime.Now && DateTime.Now <= x.EndDate);
            SelectedWeek = WeekList.FirstOrDefault(x => x.StartDate < DateTime.Now && DateTime.Now <= x.EndDate);
        }


    }
}
