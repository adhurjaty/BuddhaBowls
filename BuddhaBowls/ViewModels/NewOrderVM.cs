using BuddhaBowls.Models;
using BuddhaBowls.Services;
using BuddhaBowls.UserControls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

// TODO: Deal with all of the ParentContext weirdness

namespace BuddhaBowls
{
    /// <summary>
    /// Temp tab to create a new order
    /// </summary>
    public class NewOrderVM : TempTabVM
    {
        private RefreshDel RefreshOrder;

        #region Content Binders
        private OrderBreakdownVM _breakdownContext;
        public OrderBreakdownVM BreakdownContext
        {
            get
            {
                return _breakdownContext;
            }
            set
            {
                _breakdownContext = value;
                NotifyPropertyChanged("BreakdownContext");
            }
        }

        // Collection used for both Master List and New Order List
        ObservableCollection<InventoryItem> _filteredOrderItems;
        public ObservableCollection<InventoryItem> FilteredOrderItems
        {
            get
            {
                return _filteredOrderItems;
            }
            set
            {
                _filteredOrderItems = value;
                NotifyPropertyChanged("FilteredOrderItems");
            }
        }

        public InventoryItem SelectedOrderItem { get; set; }

        // name of the vendor in the New Order form
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
                    LoadVendorItems();
            }
        }

        // vendors in the Vendor dropdown
        public ObservableCollection<Vendor> VendorList { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;
        #endregion

        #region ICommand Bindings and Can Execute
        // Save button in New Order form
        public ICommand SaveNewOrderCommand { get; set; }
        // Reset button in New Order form
        public ICommand CancelNewOrderCommand { get; set; }
        // Clear Amts button in New Order form
        public ICommand ClearOrderCommand { get; set; }
        // Auto-Select Vendor button at top of form
        public ICommand AutoSelectVendorCommand { get; set; }

        public bool SaveOrderCanExecute
        {
            get
            {
                return OrderVendor != null && _models.InventoryItems.FirstOrDefault(x => x.LastOrderAmount > 0) != null;
            }
        }
        #endregion

        public NewOrderVM(RefreshDel refresh) : base()
        {
            RefreshOrder = refresh;
            _tabControl = new NewOrder(this);

            SaveNewOrderCommand = new RelayCommand(SaveOrder, x => SaveOrderCanExecute);
            CancelNewOrderCommand = new RelayCommand(CancelOrder);
            ClearOrderCommand = new RelayCommand(ClearOrderAmounts);
            AutoSelectVendorCommand = new RelayCommand(AutoSelectVendor);

            RefreshInventoryList();
            SetLastOrderBreakdown();
            VendorList = new ObservableCollection<Vendor>(_models.Vendors);
            OrderVendor = null;
        }

        #region ICommand Helpers
        /// <summary>
        /// Writes the inventory items to DB as they are in the New Order datagrid
        /// </summary>
        /// <param name="obj"></param>
        private void SaveOrder(object obj)
        {
            foreach (InventoryItem item in FilteredOrderItems.Where(x => x.LastOrderAmount > 0))
            {
                item.Update();
            }

            List<InventoryItem> purchasedItems = _models.InventoryItems.Where(x => x.LastOrderAmount > 0).ToList();
            PurchaseOrder po = new PurchaseOrder(OrderVendor, purchasedItems, OrderDate);

            ParentContext.GenerateAfterOrderSaved(po, OrderVendor);

            _models.PurchaseOrders.Add(po);
            RefreshOrder();

            ParentContext.DeleteTempTab();
        }

        /// <summary>
        /// Resets order amount values to last saved order amount value in New Order datagrid
        /// </summary>
        /// <param name="obj"></param>
        private void CancelOrder(object obj)
        {
            foreach (InventoryItem item in FilteredOrderItems)
            {
                item.LastOrderAmount = item.GetPrevOrderAmount();
            }

            RefreshInventoryList();
            OrderVendor = null;
            ParentContext.DeleteTempTab();
        }

        /// <summary>
        /// Sets order amounts to 0 in New Order datagrid
        /// </summary>
        /// <param name="obj"></param>
        private void ClearOrderAmounts(object obj)
        {
            foreach (InventoryItem item in FilteredOrderItems)
            {
                item.LastOrderAmount = 0;
            }

            RefreshInventoryList();
            SetLastOrderBreakdown();
        }

        private void AutoSelectVendor(object obj)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Initializers
        public ObservableCollection<BreakdownCategoryItem> GetOrderBreakdown(IEnumerable<InventoryItem> orderedItems, out float total)
        {
            ObservableCollection<BreakdownCategoryItem> breakdown = new ObservableCollection<BreakdownCategoryItem>();
            total = 0;

            foreach (string category in _models.ItemCategories)
            {
                IEnumerable<InventoryItem> items = orderedItems.Where(x => x.Category.ToUpper() == category.ToUpper());
                if (items.Count() > 0)
                {
                    BreakdownCategoryItem bdItem = new BreakdownCategoryItem(items);
                    bdItem.Background = _models.GetCategoryColorHex(category);
                    breakdown.Add(bdItem);

                    total += bdItem.TotalAmount;
                }
            }

            return breakdown;
        }

        private void SetLastOrderBreakdown()
        {
            float oTotal = 0;
            BreakdownContext = new OrderBreakdownVM()
            {
                BreakdownList = GetOrderBreakdown(_models.InventoryItems.Where(x => x.LastOrderAmount > 0), out oTotal),
                OrderTotal = oTotal,
                Header = "Price Breakdown"
            };
        }
        #endregion

        #region Update UI
        /// <summary>
        /// Called when New Order is edited
        /// </summary>
        public void InventoryOrderAmountChanged(InventoryItem item)
        {
            //_editedIds.Add(item.Id);

            NotifyPropertyChanged("FilteredOrderItems");
            SetLastOrderBreakdown();
        }

        /// <summary>
        /// Filter list of inventory items based on the string in the filter box above datagrids
        /// </summary>
        /// <param name="filterStr"></param>
        public void FilterInventoryItems(string filterStr)
        {
            FilteredOrderItems = ParentContext.FilterInventoryItems(filterStr, _models.InventoryItems);
            NotifyPropertyChanged("FilteredOrderItems");
        }

        /// <summary>
        /// When user selects a vendor, the prices update to match the last purchased price from that vendor
        /// </summary>
        private void LoadVendorItems()
        {
            List<InventoryItem> priceListItems = OrderVendor.GetFromPriceList();
            if (priceListItems != null)
            {
                foreach (InventoryItem item in FilteredOrderItems)
                {
                    InventoryItem matchingItem = priceListItems.FirstOrDefault(x => x.Id == item.Id);
                    if (matchingItem != null)
                    {
                        item.LastPurchasedPrice = matchingItem.LastPurchasedPrice;
                        //if (!_editedIds.Contains(matchingItem.Id))
                        //{
                        //    item.LastPurchasedPrice = matchingItem.LastPurchasedPrice;
                        //    item.LastOrderAmount = matchingItem.LastOrderAmount;
                        //}
                    }
                    else
                    {
                        item.LastOrderAmount = 0;
                    }
                }

                FilteredOrderItems = new ObservableCollection<InventoryItem>(SortVendorItems(priceListItems));
            }
            else
            {
                RefreshInventoryList();
            }
            SetLastOrderBreakdown();
        }

        #endregion

        private IEnumerable<InventoryItem> SortVendorItems(IEnumerable<InventoryItem> items)
        {
            return FilteredOrderItems.OrderBy(x => (items.Select(a => a.Id).Contains(x.Id) ? 0 : 1000) +
                                                    Properties.Settings.Default.InventoryOrder.IndexOf(x.Name));
        }

        private IEnumerable<InventoryItem> SortItems(IEnumerable<InventoryItem> items)
        {
            return items.OrderBy(x => Properties.Settings.Default.InventoryOrder.IndexOf(x.Name));
        }

        private void RefreshInventoryList()
        {
            if(OrderVendor == null)
            {
                FilteredOrderItems = new ObservableCollection<InventoryItem>(SortItems(_models.InventoryItems));
            }
        }
    }
}
