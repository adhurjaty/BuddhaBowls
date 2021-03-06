﻿using BuddhaBowls.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BuddhaBowls
{
    public class ChangeOrderVM : TempTabVM, INotifyPropertyChanged
    {
        private Vendor _vendor;

        // INotifyPropertyChanged event and method
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public NewInventoryVM ParentContext { get; set; }

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

        public string _selectedOriginal;
        public string SelectedOriginal
        {
            get
            {
                return _selectedOriginal;
            }
            set
            {
                _selectedOriginal = value;
                NotifyPropertyChanged("SelectedOriginal");
            }
        }

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

        private string _selectedNew;
        public string SelectedNew
        {
            get
            {
                return _selectedNew;
            }
            set
            {
                _selectedNew = value;
                NotifyPropertyChanged("SelectedNew");
            }
        }

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

        public ChangeOrderVM()
        {
            MoveToNewCommand = new RelayCommand(MoveToNew, x => MoveToNewCanExecute);
            MoveToOriginalCommand = new RelayCommand(MoveToOriginal, x => MoveToOriginalCanExecute);
            CancelCommand = new RelayCommand(CancelHelper);

            NewOrder = new ObservableCollection<string>();
        }

        public ChangeOrderVM(NewInventoryVM parent) : this()
        {
            ParentContext = parent;
            
            SaveCommand = new RelayCommand(SaveInventoryHelper, x => SaveCanExecute);
            OriginalOrder = new ObservableCollection<string>(((NewInventoryVM)ParentContext).FilteredInventoryItems.Select(x => x.Name));
            Header = "Change Inventory Order";
        }

        public ChangeOrderVM(VendorTabVM parent, Vendor vendor) : this()
        {
            ParentContext = parent;
            _vendor = vendor;

            SaveCommand = new RelayCommand(SaveVendorHelper, x => SaveCanExecute);
            OriginalOrder = new ObservableCollection<string>(vendor.GetRecListOrder());
            Header = "Change Order - " + vendor.Name;
        }

        #region ICommand Helpers

        private void MoveToNew(object obj)
        {
            NewOrder.Add(SelectedOriginal);
            OriginalOrder.Remove(SelectedOriginal);
            SelectedOriginal = OriginalOrder.FirstOrDefault();
        }

        private void MoveToOriginal(object obj)
        {
            OriginalOrder.Add(SelectedNew);
            NewOrder.Remove(SelectedNew);
            SelectedNew = NewOrder.FirstOrDefault();
        }

        private void SaveInventoryHelper(object obj)
        {
            if(OriginalOrder.Count > 0)
            {
                MoveToNew();
            }
            else
            {
                Properties.Settings.Default.InventoryOrder = NewOrder.ToList();
                Properties.Settings.Default.Save();
                ((InventoryTabVM)ParentContext).LoadDisplayItems();

                string dir = Path.Combine(Properties.Settings.Default.DBLocation, "Settings");
                Directory.CreateDirectory(dir);
                File.WriteAllLines(Path.Combine(dir, GlobalVar.INV_ORDER_FILE), NewOrder);

                ParentContext.ParentContext.DeleteTempTab();
            }
        }

        private void SaveVendorHelper(object obj)
        {
            if (OriginalOrder.Count > 0)
            {
                MoveToNew();
            }
            else
            {
                _vendor.SaveItemOrder(NewOrder.ToList());

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

        public void ReorderNewList(string droppedData, string target)
        {
            int removeIdx = NewOrder.IndexOf(droppedData);
            int targetIdx = NewOrder.IndexOf(target);

            if (removeIdx < targetIdx)
            {
                NewOrder.Insert(targetIdx + 1, droppedData);
                NewOrder.RemoveAt(removeIdx);
            }
            else
            {
                int remIdx = removeIdx + 1;
                if(remIdx < NewOrder.Count)
                {
                    NewOrder.Insert(targetIdx, droppedData);
                    NewOrder.RemoveAt(remIdx);
                }
            }
        }

        private void MoveToNew()
        {
            foreach (string name in OriginalOrder)
            {
                NewOrder.Add(name);
            }

            OriginalOrder.Clear();
        }
        #endregion
    }
}
