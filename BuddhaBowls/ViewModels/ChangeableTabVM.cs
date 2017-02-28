using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace BuddhaBowls
{
    public class ChangeableTabVM : TabVM
    {
        protected enum PageState { Primary, Secondary, Error };
        protected PageState _pageState;

        #region Content Binders

        private string _header;
        public string Header
        {
            get
            {
                return _header;
            }
            set
            {
                _header = value;
                NotifyPropertyChanged("Header");
            }
        }

        private string _primaryPageName;
        public string PrimaryPageName
        {
            get
            {
                return _primaryPageName;
            }
            set
            {
                _primaryPageName = value;
                NotifyPropertyChanged("PrimaryPageName");
            }
        }

        private string _secondaryPageName;
        public string SecondaryPageName
        {
            get
            {
                return _secondaryPageName;
            }
            set
            {
                _secondaryPageName = value;
                NotifyPropertyChanged("SecondaryPageName");
            }
        }

        private UserControl _tabControl;
        public UserControl TabControl
        {
            get
            {
                return _tabControl;
            }
            set
            {
                _tabControl = value;
                NotifyPropertyChanged("TabControl");
            }
        }
        #endregion

        #region ICommand and CanExecute

        public ICommand PrimaryCommand{ get; set; }
        public ICommand SecondaryCommand{ get; set; }

        #endregion

        public ChangeableTabVM() : base()
        {
            _pageState = DBConnection ? PageState.Primary : PageState.Error;

            PrimaryCommand = new RelayCommand(SwtichToPrimary, x => _pageState == PageState.Secondary);
            SecondaryCommand = new RelayCommand(SwtichToSecondary, x => _pageState == PageState.Primary);

            ChangePageState(_pageState);
        }

        #region ICommand Helpers

        private void SwtichToPrimary(object obj)
        {
            ChangePageState(PageState.Primary);
        }

        private void SwtichToSecondary(object obj)
        {
            ChangePageState(PageState.Secondary);
        }

        #endregion

        protected virtual void ChangePageState(PageState state)
        {
            _pageState = state;
        }
    }
}
