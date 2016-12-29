﻿using BuddhaBowls.Models;
using BuddhaBowls.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

// TODO: Deal with all of the ParentContext weirdness

namespace BuddhaBowls
{
    public class NewOrderVM : INotifyPropertyChanged
    {
        private ModelContainer _models;
        private Thread _thread;

        // INotifyPropertyChanged event and method
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public OrderTabVM ParentContext { get; set; }

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
        public ObservableCollection<InventoryItem> FilteredOrderItems
        {
            get
            {
                // really bad pattern but whatchu gonna do?
                return ParentContext.ParentContext.FilteredInventoryItems;
            }
            set
            {
                ParentContext.ParentContext.FilteredInventoryItems = value;
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
                    SetVendorPrices();
            }
        }

        // vendors in the Vendor dropdown
        public ObservableCollection<Vendor> VendorList { get; set; }
        #endregion

        #region ICommand Bindings and Can Execute
        // Save button in New Order form
        public ICommand SaveNewOrderCommand { get; set; }
        // Reset button in New Order form
        public ICommand CancelNewOrderCommand { get; set; }
        // Clear Amts button in New Order form
        public ICommand ClearOrderCommand { get; set; }

        public bool SaveOrderCanExecute
        {
            get
            {
                return OrderVendor != null && _models.InventoryItems.FirstOrDefault(x => x.LastOrderAmount > 0) != null;
            }
        }
        #endregion

        public NewOrderVM(ModelContainer models, OrderTabVM parent)
        {
            ParentContext = parent;
            _models = models;

            SaveNewOrderCommand = new RelayCommand(SaveOrder, x => SaveOrderCanExecute);
            CancelNewOrderCommand = new RelayCommand(CancelOrder);
            ClearOrderCommand = new RelayCommand(ClearOrderAmounts);

            SetLastOrderBreakdown();
            VendorList = new ObservableCollection<Vendor>(_models.Vendors);
        }

        #region ICommand Helpers
        /// <summary>
        /// Writes the inventory items to DB as they are in the New Order datagrid
        /// </summary>
        /// <param name="obj"></param>
        private void SaveOrder(object obj)
        {
            foreach (InventoryItem item in FilteredOrderItems)
            {
                item.Update();
            }

            List<InventoryItem> purchasedItems = _models.InventoryItems.Where(x => x.LastOrderAmount > 0).ToList();
            PurchaseOrder po = new PurchaseOrder(OrderVendor.Name, purchasedItems);

            _thread = new Thread(delegate()
            {
                ReportGenerator generator = new ReportGenerator(_models);
                string xlsPath = generator.GenerateOrder(po, OrderVendor);
                generator.Close();
                OrderVendor = null;
                System.Diagnostics.Process.Start(xlsPath);
            });
            _thread.Start();

            ParentContext.DeleteTempTab();

            _models.PurchaseOrders.Add(po);
            ParentContext.RefreshOrderList();

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
        public void InventoryOrderAmountChanged()
        {
            //FilteredOrderItems = new ObservableCollection<InventoryItem>(FilteredOrderItems);
            NotifyPropertyChanged("FilteredOrderItems");
            SetLastOrderBreakdown();
        }

        /// <summary>
        /// Filter list of inventory items based on the string in the filter box above datagrids
        /// </summary>
        /// <param name="filterStr"></param>
        public void FilterInventoryItems(string filterStr)
        {
            ParentContext.ParentContext.FilterInventoryItems(filterStr);
            NotifyPropertyChanged("FilteredOrderItems");
        }

        /// <summary>
        /// When user selects a vendor, the prices update to match the last purchased price from that vendor
        /// </summary>
        private void SetVendorPrices()
        {
            List<InventoryItem> priceListItems = OrderVendor.GetFromPriceList();
            if (priceListItems != null)
            {
                foreach (InventoryItem item in FilteredOrderItems)
                {
                    InventoryItem matchingItem = priceListItems.FirstOrDefault(x => x.Id == item.Id);
                    if (matchingItem != null)
                        item.LastPurchasedPrice = matchingItem.LastPurchasedPrice;
                }
            }
            RefreshInventoryList();
            SetLastOrderBreakdown();
        }
        #endregion

        private void RefreshInventoryList()
        {
            ParentContext.ParentContext.RefreshInventoryList();
            NotifyPropertyChanged("FilteredOrderItems");
        }
    }
}
