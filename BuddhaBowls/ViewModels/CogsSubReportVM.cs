using BuddhaBowls.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BuddhaBowls
{
    public delegate void DoubleClickEvent(IInvEvent invEvent);
    public delegate void ExpandCollapseEvent(CogsSubReportVM context);

    public class CogsSubReportVM : INotifyPropertyChanged
    {
        private DoubleClickEvent OnDoubleClick;
        private ExpandCollapseEvent OnExpandCollapse;
        

        // INotifyPropertyChanged event and method
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _detailHeader;
        public string DetailHeader
        {
            get
            {
                return _detailHeader;
            }
            set
            {
                _detailHeader = value;
                NotifyPropertyChanged("DetailHeader");
            }
        }

        private ObservableCollection<IInvEvent> _eventList;
        public ObservableCollection<IInvEvent> EventList
        {
            get
            {
                return _eventList;
            }
            set
            {
                _eventList = value;
                NotifyPropertyChanged("EventList");
            }
        }

        private IInvEvent _selectedEvent;
        public IInvEvent SelectedEvent
        {
            get
            {
                return _selectedEvent;
            }
            set
            {
                _selectedEvent = value;
                NotifyPropertyChanged("SelectedEvent");
            }
        }

        private string _expandChevron = "ChevronRight";
        public string ExpandChevron
        {
            get
            {
                return _expandChevron;
            }
            set
            {
                _expandChevron = value;
                NotifyPropertyChanged("ExpandChevron");
            }
        }

        public ICommand ExpandCollapseCommand { get; set; }

        public CogsSubReportVM(string header, DoubleClickEvent openEvent, ExpandCollapseEvent collapseEvent)
        {
            DetailHeader = header;
            OnDoubleClick = openEvent;
            OnExpandCollapse = collapseEvent;

            ExpandCollapseCommand = new RelayCommand(ExpandCollapsedClick);
        }

        public void ExpandCollapsedClick(object obj)
        {
            OnExpandCollapse(this);
        }

        public void ItemDoubleClicked(IInvEvent item)
        {
            OnDoubleClick(item);
        }

        public void SetInvEnvents<T>(List<T> invEvents) where T : IInvEvent
        {
            EventList = new ObservableCollection<IInvEvent>(invEvents.Select(x => (IInvEvent)x).ToList());
        }

        public void Expanded()
        {
            ExpandChevron = "ChevronDown";
        }

        public void Collapsed()
        {
            ExpandChevron = "ChevronRight";
        }
    }
}
