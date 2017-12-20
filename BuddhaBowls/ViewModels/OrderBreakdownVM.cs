using BuddhaBowls.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System;
using BuddhaBowls.Helpers;
using System.Threading.Tasks;

namespace BuddhaBowls
{
    /// <summary>
    /// VM for a part of the View Order tab. Handles all of the things that an individual order breakdown needs
    /// </summary>
    public class OrderBreakdownVM : TabVM
    {
        private List<InventoryItem> _items;
        private bool _readOnly;

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
                NotifyPropertyChanged("BreakdownList");
                NotifyPropertyChanged("OrderTotal");
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
                if(BreakdownList != null)
                    return BreakdownList.Sum(x => x.TotalAmount) + VendorShippingCost;
                return 0;
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
                NotifyPropertyChanged("OrderVendor");
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

        public OrderBreakdownVM(List<InventoryItem> orderItems, string header, bool isReadOnly)
        {
            _items = orderItems;
            Header = header;
            _readOnly = isReadOnly;
            UpdateDisplay();
        }

        public List<InventoryItem> GetInventoryItems()
        {
            return _items;
        }

        /// <summary>
        /// Method used to ensure that only 1 item is selected for the whole breakdown display
        /// </summary>
        public void ClearSelectedItems()
        {
            foreach(BreakdownCategoryItem bci in BreakdownList)
            {
                bci.SelectedItem = null;
            }
        }

        public async void UpdateDisplay()
        {
            await Task.Run(() =>
            {
                BreakdownList = new ObservableCollection<BreakdownCategoryItem>(_items.Where(x => x.LastOrderAmount > 0).GroupBy(x => x.Category)
                                                       .Select(x => new BreakdownCategoryItem(x, UpdateDisplay, _readOnly)));
            });
        }

        //public void UpdateItem(InventoryItem item)
        //{
        //    BreakdownCategoryItem bdItem = BreakdownList.FirstOrDefault(x => x.Category == item.Category);
        //    if (bdItem == null)
        //    {
        //        BreakdownList.Add(new BreakdownCategoryItem(new List<InventoryItem>() { item })
        //        {
        //            Background = _models.GetCategoryColorHex(item.Category),
        //            OrderVendor = OrderVendor,
        //            IsReadOnly = true
        //        });
        //        BreakdownList = new ObservableCollection<BreakdownCategoryItem>(BreakdownList.OrderBy(x => _models.SortCategory(x.Category)));
        //    }
        //    else
        //    {
        //        bdItem.UpdateOrderItem(item);
        //        if (bdItem.Items.Count == 0)
        //            BreakdownList.Remove(bdItem);
        //    }
        //    UpdateTotal();
        //    NotifyPropertyChanged("BreakdownList");
        //    //BreakdownList = new ObservableCollection<BreakdownCategoryItem>(BreakdownList);
        //}
    }

    public class BreakdownCategoryItem : TabVM
    {
        private Action OnValueChanged;


        private string _background;
        public string Background
        {
            get
            {
                return _background;
            }
            set
            {
                _background = value;
                NotifyPropertyChanged("Background");
            }
        }

        private string _category;
        public string Category
        {
            get
            {
                return _category;
            }
            set
            {
                _category = value;
                NotifyPropertyChanged("Category");
            }
        }

        private ObservableCollection<InventoryItem> _items;
        public ObservableCollection<InventoryItem> Items
        {
            get
            {
                return _items;
            }
            set
            {
                _items = value;
                NotifyPropertyChanged("Items");
                NotifyPropertyChanged("TotalAmount");
            }
        }

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
                //if (ClearSelected != null && value != null)
                //    ClearSelected();

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

        //public Vendor OrderVendor { get; internal set; }

        //public BreakdownSelectionChanged ClearSelected;

        public BreakdownCategoryItem(IEnumerable<InventoryItem> items) : base()
        {
            Items = new ObservableCollection<InventoryItem>(items);
            Category = Items.First().Category;
            Background = _models.GetCategoryColorHex(Category);
            //OrderVendor = _models.VContainer.Items.FirstOrDefault(x => x.Name == _order.VendorName);
        }

        public BreakdownCategoryItem(IEnumerable<InventoryItem> items, Action valueChanged, bool readOnly) : this(items)
        {
            IsReadOnly = readOnly;
            if(!readOnly)
                OnValueChanged = valueChanged;
        }

        public void UpdateOrderItem(InventoryItem item)
        {
            //if (OnValueChanged != null)
            //    OnValueChanged(item);
            //else
            //{
            //    InventoryItem listItem = Items.FirstOrDefault(x => x.Id == item.Id);
            //    if (listItem != null)
            //    {
            //        if (listItem.LastOrderAmount == 0)
            //            Items.Remove(listItem);
            //        else
            //        {
            //            int idx = Items.IndexOf(listItem);
            //            Items[idx] = item;
            //        }
            //        Items = new ObservableCollection<InventoryItem>(Items);
            //    }
            //    else if(item.LastOrderAmount != 0)
            //    {
            //        Items.Add(item);
            //        Items = new ObservableCollection<InventoryItem>(MainHelper.SortItems(Items));
            //    }
            //}
            OnValueChanged();
            NotifyPropertyChanged("TotalAmount");
            NotifyPropertyChanged("Items");
        }
    }
}
