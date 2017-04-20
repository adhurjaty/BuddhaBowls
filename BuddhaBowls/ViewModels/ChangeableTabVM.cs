using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace BuddhaBowls
{
    public class ChangeableTabVM : TabVM
    {
        //protected enum PageState { Primary, Secondary, Error };
        //protected PageState _pageState;
        protected int _pageIndex;

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

        private ObservableCollection<SwitchButton> _switchButtonList;
        public ObservableCollection<SwitchButton> SwitchButtonList
        {
            get
            {
                return _switchButtonList;
            }
            set
            {
                _switchButtonList = value;
                NotifyPropertyChanged("SwitchButtonList");
            }
        }

        //private string _primaryPageName;
        //public string PrimaryPageName
        //{
        //    get
        //    {
        //        return _primaryPageName;
        //    }
        //    set
        //    {
        //        _primaryPageName = value;
        //        NotifyPropertyChanged("PrimaryPageName");
        //    }
        //}

        //private string _secondaryPageName;
        //public string SecondaryPageName
        //{
        //    get
        //    {
        //        return _secondaryPageName;
        //    }
        //    set
        //    {
        //        _secondaryPageName = value;
        //        NotifyPropertyChanged("SecondaryPageName");
        //    }
        //}

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

        //public ICommand PrimaryCommand{ get; set; }
        //public ICommand SecondaryCommand{ get; set; }

        #endregion

        public ChangeableTabVM() : base()
        {
            //_pageState = DBConnection ? PageState.Primary : PageState.Error;

            //PrimaryCommand = new RelayCommand(SwtichToPrimary, x => _pageState == PageState.Secondary);
            //SecondaryCommand = new RelayCommand(SwtichToSecondary, x => _pageState == PageState.Primary);
            _pageIndex = 0;

            ChangePageState(_pageIndex);
        }

        #region ICommand Helpers

        protected void SwitchPage(object idx)
        {
            ChangePageState((int)idx);
        }

        //private void SwtichToSecondary(object obj)
        //{
        //    ChangePageState(PageState.Secondary);
        //}

        #endregion

        protected void InitSwitchButtons(string[] tabs)
        {
            SwitchButtonList = new ObservableCollection<SwitchButton>();
            for (int i = 0; i < tabs.Length; i++)
            {
                SwitchButtonList.Add(new SwitchButton(tabs[i], SwitchPage, i));
            }
        }

        protected virtual void ChangePageState(int pageIdx)
        {
            if(SwitchButtonList != null && SwitchButtonList.Count > _pageIndex)
                SwitchButtonList[_pageIndex].CanExecute = true;
            _pageIndex = pageIdx;
            if(SwitchButtonList != null && SwitchButtonList.Count > pageIdx)
                SwitchButtonList[pageIdx].CanExecute = false;
        }
    }

    public class SwitchButton
    {
        public string PageName { get; set; }
        public int PageIdx { get; set; }

        public ICommand SwitchCommand { get; set; }
        public bool CanExecute { get; set; }

        public SwitchButton(string name, Action<object> cmd, int idx)
        {
            PageName = name;
            CanExecute = idx != 0;
            SwitchCommand = new RelayCommand(cmd, x => CanExecute);
            PageIdx = idx;
        }
    }
}
