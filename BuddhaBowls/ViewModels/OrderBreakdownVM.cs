using BuddhaBowls.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System;

namespace BuddhaBowls
{
    public delegate void BreakdownSelectionChanged();

    /// <summary>
    /// VM for a part of the View Order tab. Handles all of the things that an individual order breakdown needs
    /// </summary>
    public class OrderBreakdownVM : INotifyPropertyChanged
    {
        // INotifyPropertyChanged event and method
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private ObservableCollection<BreakdownCategoryItem> _breakdownList;
        public ObservableCollection<BreakdownCategoryItem> BreakdownList
        {
            get
            {
                return _breakdownList;
            }
            set
            {
                _breakdownList = value;
                foreach(BreakdownCategoryItem bci in _breakdownList)
                {
                    bci.ClearSelected = ClearSelectedItems;
                }
                NotifyPropertyChanged("BreakdownList");
            }
        }

        private float _orderCost;
        public float OrderTotal
        {
            get
            {
                return _orderCost;
            }
            set
            {
                _orderCost = value;
                NotifyPropertyChanged("OrderTotal");
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

        public InventoryItem SelectedItem
        {
            get
            {
                BreakdownCategoryItem item = BreakdownList.FirstOrDefault(x => x.SelectedItem != null);
                if (item == null)
                    return null;
                return item.SelectedItem;
            }
        }

        public List<InventoryItem> GetInventoryItems()
        {
            List<InventoryItem> outItems = new List<InventoryItem>();

            foreach(BreakdownCategoryItem bci in BreakdownList)
            {
                foreach(InventoryItem item in bci.Items)
                {
                    outItems.Add(item);
                }
            }

            return outItems;
        }

        public void ClearSelectedItems()
        {
            foreach(BreakdownCategoryItem bci in _breakdownList)
            {
                bci.SelectedItem = null;
            }
        }
    }

    public class BreakdownCategoryItem : INotifyPropertyChanged
    {
        // INotifyPropertyChanged event and method
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Background { get; set; }
        public string Category { get; set; }
        public ObservableCollection<InventoryItem> Items { get; set; }

        public float TotalAmount
        {
            get
            {
                return Items.Sum(x => x.PurchaseExtension);
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
                if (ClearSelected != null && value != null)
                    ClearSelected();

                _selectedItem = value;
                NotifyPropertyChanged("SelectedItem");
            }
        }

        private bool _isReadOnly = true;
        public bool IsReadOnly
        {
            get
            {
                return _isReadOnly;
            }
            set
            {
                _isReadOnly = value;
                NotifyPropertyChanged("IsReadOnly");
            }
        }

        public BreakdownSelectionChanged ClearSelected;

        public BreakdownCategoryItem(IEnumerable<InventoryItem> items)
        {
            Items = new ObservableCollection<InventoryItem>(items);
            Category = Items.First().Category;
        }

        public BreakdownCategoryItem(IEnumerable<InventoryItem> items, bool readOnly) : this(items)
        {
            IsReadOnly = readOnly;
        }

        public void Update()
        {
            NotifyPropertyChanged("TotalAmount");
            NotifyPropertyChanged("Items");
        }
    }
}
