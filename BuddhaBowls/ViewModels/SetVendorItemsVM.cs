using BuddhaBowls.Models;
using BuddhaBowls.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using BuddhaBowls.UserControls;
using System.Windows;
using BuddhaBowls.Helpers;

namespace BuddhaBowls
{
    /// <summary>
    /// Temp tab where user can populate which items the selected vendor sells and at what price and qty
    /// </summary>
    public class SetVendorItemsVM : TempTabVM
    {
        private Vendor _vendor;
        private List<InventoryItem> _availableItems;
        private List<InventoryItem> _removedItems;

        #region Content Binders

        private ObservableCollection<InventoryItem> _vendorItems;
        public ObservableCollection<InventoryItem> VendorItems
        {
            get
            {
                return _vendorItems;
            }
            set
            {
                _vendorItems = value;
                NotifyPropertyChanged("VendorItems");
            }
        }

        private string _filterText;
        public string FilterText
        {
            get
            {
                return _filterText;
            }
            set
            {
                _filterText = value;
                NotifyPropertyChanged("FilterText");
            }
        }

        private InventoryItem _selectedItem;
        public InventoryItem SelectedItem
        {
            get
            {
                return _selectedItem;
            }
            set
            {
                _selectedItem = value;
                NotifyPropertyChanged("SelectedItem");
            }
        }

        private ObservableCollection<InventoryItem> _remainingItems;
        public ObservableCollection<InventoryItem> RemainingItems
        {
            get
            {
                return _remainingItems;
            }
            set
            {
                _remainingItems = value;
                NotifyPropertyChanged("RemainingItems");
            }
        }

        public InventoryItem ItemToAdd { get; set; }

        private Visibility _modalVisibility = Visibility.Hidden;
        public Visibility ModalVisibility
        {
            get
            {
                return _modalVisibility;
            }
            set
            {
                _modalVisibility = value;
                NotifyPropertyChanged("ModalVisibility");
            }
        }

        public string ModalTitle { get; set; } = "Add Vendor Item";

        #endregion

        #region ICommand Properties and Can Execute

        public ICommand AddItemCommand { get; set; }
        public ICommand RemoveItemCommand { get; set; }
        public ICommand ResetCommand { get; set; }
        public ICommand SaveCommand { get; set; }
        public ICommand CancelCommand { get; set; }
        public ICommand ModalOkCommand { get; set; }
        public ICommand ModalCancelCommand { get; set; }

        public bool RemoveCanExecute
        {
            get
            {
                return SelectedItem != null;
            }
        }

        public bool ModalOkCanExecute
        {
            get
            {
                return ItemToAdd != null;
            }
        }

        #endregion

        public SetVendorItemsVM(Vendor vendor)
        {
            _vendor = vendor;
            _tabControl = new SetVendorItems(this);
            _removedItems = new List<InventoryItem>();

            Header = vendor.Name + ": Edit Purchase List";

            AddItemCommand = new RelayCommand(AddItem);
            RemoveItemCommand = new RelayCommand(RemoveItem, x => RemoveCanExecute);
            ResetCommand = new RelayCommand(ResetList);
            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
            ModalOkCommand = new RelayCommand(ModalOk, x => ModalOkCanExecute);
            ModalCancelCommand = new RelayCommand(ModalCancel);

            _availableItems = vendor.GetInventoryItems() ?? new List<InventoryItem>();
            Refresh();
        }

        #region ICommand Helpers

        private void AddItem(object obj)
        {
            ParentContext.ModalContext = this;
            ModalVisibility = Visibility.Visible;
        }

        private void RemoveItem(object obj)
        {
            _removedItems.Add(SelectedItem);
            _availableItems.Remove(SelectedItem);
            Refresh();
        }

        private void ResetList(object obj)
        {
            VendorItems = new ObservableCollection<InventoryItem>(_vendor.GetInventoryItems());
        }

        private void Save(object obj)
        {
            foreach(InventoryItem item in _removedItems)
            {
                _vendor.RemoveInvItem(item);
            }
            _vendor.Update(_availableItems);
            ParentContext.GenerateVendorOrderList(_vendor, open: false);
            Close();
        }

        private void Cancel(object obj)
        {
            Close();
        }

        private void ModalOk(object obj)
        {
            _availableItems.Add(ItemToAdd);
            Refresh();
            ModalVisibility = Visibility.Hidden;
        }

        private void ModalCancel(object obj)
        {
            ItemToAdd = null;
            ModalVisibility = Visibility.Hidden;
        }

        #endregion

        #region Initializers

        #endregion

        #region Update UI

        public void FilterVendorItems(string text)
        {
            VendorItems = ParentContext.FilterInventoryItems(text, _availableItems);
        }

        private void Refresh()
        {
            if (_availableItems == null)
            {
                VendorItems = new ObservableCollection<InventoryItem>();
                RemainingItems = new ObservableCollection<InventoryItem>(_models.InventoryItems.OrderBy(x => x.Name));
            }
            else
            {
                VendorItems = new ObservableCollection<InventoryItem>(MainHelper.SortItems(_availableItems));
                RemainingItems = new ObservableCollection<InventoryItem>(_models.InventoryItems
                                                                                .Where(x => !_availableItems.Select(y => y.Id).Contains(x.Id))
                                                                                .OrderBy(x => x.Name));
            }
        }

        #endregion
    }
}
