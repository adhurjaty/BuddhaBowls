using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BuddhaBowls
{
    public class ChangeableTabVM : TabVM
    {
        //protected enum PageState { Primary, Secondary, Error };
        //protected PageState _pageState;
        protected int _pageIndex;
        // hate this pre-initialization but it makes _tabCache easier to deal with
        protected UserControl[] _tabCache = new UserControl[5];

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

        private PeriodSelectorVM _periodSelector;
        public PeriodSelectorVM PeriodSelector
        {
            get
            {
                return _periodSelector;
            }
            set
            {
                _periodSelector = value;
                NotifyPropertyChanged("PeriodSelector");
            }
        }

        #endregion

        #region ICommand and CanExecute

        #endregion

        public ChangeableTabVM() : base()
        {
            _pageIndex = DBConnection ? 0 : -1;

            ChangePageState(_pageIndex);
        }

        #region ICommand Helpers

        protected void SwitchPage(object idx)
        {
            ChangePageState((int)idx);
        }

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
            if(pageIdx != -1)
            {
                if (SwitchButtonList != null)
                {
                    if (_pageIndex > -1)
                    {
                        SwitchButtonList[_pageIndex].CanExecute = true;
                        if (_tabCache == null)
                            _tabCache = new UserControl[SwitchButtonList.Count];
                        _tabCache[_pageIndex] = TabControl;
                    }
                    SwitchButtonList[pageIdx].CanExecute = false;
                }
            }
            _pageIndex = pageIdx;
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
