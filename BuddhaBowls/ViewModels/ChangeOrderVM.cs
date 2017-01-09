using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BuddhaBowls
{
    public class ChangeOrderVM : INotifyPropertyChanged
    {
        // INotifyPropertyChanged event and method
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public InventoryTabVM ParentContext { get; set; }

        #region Data Bindings

        private ObservableCollection<string> _originalOrder;
        public ObservableCollection<string> OriginalOrder
        {
            get
            {
                return _originalOrder;
            }
            set
            {
                _originalOrder = value;
                NotifyPropertyChanged("OriginalOrder");
            }
        }

        public string SelectedOriginal { get; set; }

        private ObservableCollection<string> _newOrder;
        public ObservableCollection<string> NewOrder
        {
            get
            {
                return _newOrder;
            }
            set
            {
                _newOrder = value;
                NotifyPropertyChanged("NewOrder");
            }
        }

        public string SelectedNew { get; set; }

        #endregion

        #region ICommand Bindings and Can Execute

        public ICommand MoveToNewCommand { get; set; }
        public ICommand MoveToOriginalCommand { get; set; }
        public ICommand SaveCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        public bool MoveToNewCanExecute
        {
            get
            {
                return OriginalOrder.Count > 0 && !string.IsNullOrEmpty(SelectedOriginal);
            }
        }

        public bool MoveToOriginalCanExecute
        {
            get
            {
                return NewOrder.Count > 0 && !string.IsNullOrEmpty(SelectedNew);
            }
        }

        public bool SaveCanExecute
        {
            get
            {
                return NewOrder.Count > 0;
            }
        }
        #endregion

        public ChangeOrderVM(InventoryTabVM parent)
        {
            ParentContext = parent;

            MoveToNewCommand = new RelayCommand(MoveToNew, x => MoveToNewCanExecute);
            MoveToOriginalCommand = new RelayCommand(MoveToOriginal, x => MoveToOriginalCanExecute);
            SaveCommand = new RelayCommand(SaveHelper, x => SaveCanExecute);
            CancelCommand = new RelayCommand(CancelHelper);

            OriginalOrder = new ObservableCollection<string>(ParentContext.FilteredInventoryItems.Select(x => x.Name));
            NewOrder = new ObservableCollection<string>();
        }

        #region ICommand Helpers

        private void MoveToNew(object obj)
        {
            NewOrder.Add(SelectedOriginal);
            OriginalOrder.Remove(SelectedOriginal);
            SelectedOriginal = null;
        }

        private void MoveToOriginal(object obj)
        {
            OriginalOrder.Add(SelectedNew);
            NewOrder.Remove(SelectedNew);
            SelectedNew = null;
        }

        private void SaveHelper(object obj)
        {
            if(OriginalOrder.Count > 0)
            {
                foreach(string name in OriginalOrder)
                {
                    NewOrder.Add(name);
                }

                OriginalOrder.Clear();
            }
            else
            {
                Properties.Settings.Default.InventoryOrder = NewOrder.ToList();
                Properties.Settings.Default.Save();
                ParentContext.ParentContext.DeleteTempTab();
            }
        }

        private void CancelHelper(object obj)
        {
            ParentContext.ParentContext.DeleteTempTab();
        }

        #endregion

        #region Initializers

        #endregion

        #region Update UI Methods

        #endregion
    }
}
