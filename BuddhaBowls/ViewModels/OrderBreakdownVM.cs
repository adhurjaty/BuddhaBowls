using BuddhaBowls.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System;
using BuddhaBowls.Helpers;

namespace BuddhaBowls
{
    public delegate void BreakdownSelectionChanged();
    public delegate void BreakdownUpdate(InventoryItem item);

    /// <summary>
    /// VM for a part of the View Order tab. Handles all of the things that an individual order breakdown needs
    /// </summary>
    public class OrderBreakdownVM : TabVM
    {
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

        public int HasShipping
        {
            get
            {
                return VendorShippingCost > 0 ? 20 : 0;
            }
        }

        private float _vendorShippingCost;
        public float VendorShippingCost
        {
            get
            {
                return _vendorShippingCost;
            }
            set
            {
                _vendorShippingCost = value;
                NotifyPropertyChanged("VendorShippingCost");
                NotifyPropertyChanged("HasShipping");
            }
        }

        public float OrderTotal
        {
            get
            {
                return BreakdownList.Sum(x => x.TotalAmount) + VendorShippingCost;
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

        private Vendor _orderVendor;
        public Vendor OrderVendor
        {
            get
            {
                return _orderVendor;
            }
            set
            {
                _orderVendor = value;
                if(_orderVendor != null)
                    VendorShippingCost = _orderVendor.ShippingCost;
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

        public void UpdateTotal()
        {
            NotifyPropertyChanged("OrderTotal");
        }

        public void UpdateItem(VendorInventoryItem item)
        {
            BreakdownCategoryItem bdItem = BreakdownList.FirstOrDefault(x => x.Category == item.Category);
            if (bdItem == null)
            {
                BreakdownList.Add(new BreakdownCategoryItem(new List<InventoryItem>() { item.ToInventoryItem() })
                {
                    Background = _models.GetCategoryColorHex(item.Category),
                    OrderVendor = OrderVendor,
                    IsReadOnly = true
                });
                BreakdownList = new ObservableCollection<BreakdownCategoryItem>(BreakdownList.OrderBy(x => _models.SortCategory(x.Category)));
            }
            else
            {
                bdItem.UpdateOrderItem(item.ToInventoryItem());
                if (bdItem.Items.Count == 0)
                    BreakdownList.Remove(bdItem);
            }
            UpdateTotal();
            NotifyPropertyChanged("BreakdownList");
            //BreakdownList = new ObservableCollection<BreakdownCategoryItem>(BreakdownList);
        }
    }

    public class BreakdownCategoryItem : TabVM
    {
        private BreakdownUpdate OnValueChanged;

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

        public Vendor OrderVendor { get; internal set; }

        public BreakdownSelectionChanged ClearSelected;

        public BreakdownCategoryItem(IEnumerable<InventoryItem> items) : base()
        {
            Items = new ObservableCollection<InventoryItem>(items);
            Category = Items.First().Category;
        }

        public BreakdownCategoryItem(IEnumerable<InventoryItem> items, BreakdownUpdate valueChanged, bool readOnly) : this(items)
        {
            IsReadOnly = readOnly;
            if(!readOnly)
                OnValueChanged = valueChanged;
        }

        public void UpdateOrderItem(InventoryItem item)
        {
            if (OnValueChanged != null)
                OnValueChanged(item);
            else
            {
                InventoryItem listItem = Items.FirstOrDefault(x => x.Id == item.Id);
                if (listItem != null)
                {
                    if (listItem.LastOrderAmount == 0)
                        Items.Remove(listItem);
                    else
                    {
                        int idx = Items.IndexOf(listItem);
                        Items[idx] = item;
                    }
                    Items = new ObservableCollection<InventoryItem>(Items);
                }
                else if(item.LastOrderAmount != 0)
                {
                    Items.Add(item);
                    Items = new ObservableCollection<InventoryItem>(MainHelper.SortItems(Items));
                }
            }

            NotifyPropertyChanged("TotalAmount");
            NotifyPropertyChanged("Items");
        }
    }
}
