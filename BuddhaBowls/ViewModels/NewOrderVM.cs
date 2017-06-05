using BuddhaBowls.Helpers;
using BuddhaBowls.Models;
using BuddhaBowls.Services;
using BuddhaBowls.UserControls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace BuddhaBowls
{
    /// <summary>
    /// Temp tab to create a new order
    /// </summary>
    public class NewOrderVM : TempTabVM
    {
        private RefreshDel RefreshOrder;
        // stores vendor inventory offering
        private List<VendorInventoryItem> _sortedInvtems;
        private List<VendorInventoryItem> _shownInvItems;

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
        ObservableCollection<VendorInventoryItem> _filteredOrderItems;
        public ObservableCollection<VendorInventoryItem> FilteredOrderItems
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

        public VendorInventoryItem SelectedOrderItem { get; set; }

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
                FilterInventoryItems();
                NotifyPropertyChanged("FilterText");
            }
        }

        // vendors in the Vendor dropdown
        public ObservableCollection<Vendor> VendorList { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Today;

        private List<string> _purchasedUnitList;
        public List<string> PurchasedUnitList
        {
            get
            {
                return _purchasedUnitList;
            }
            set
            {
                _purchasedUnitList = value;
                NotifyPropertyChanged("PurchasedUnitList");
            }
        }

        private Visibility _unitVisibility = Visibility.Hidden;
        public Visibility UnitVisibility
        {
            get
            {
                return _unitVisibility;
            }
            set
            {
                _unitVisibility = value;
                NotifyPropertyChanged("UnitVisibility");
            }
        }
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
                return OrderVendor != null && FilteredOrderItems.FirstOrDefault(x => x.LastOrderAmount > 0) != null;
            }
        }
        #endregion

        public NewOrderVM(RefreshDel refresh) : base()
        {
            RefreshOrder = refresh;
            _tabControl = new NewOrder(this);
            _sortedInvtems = _models.VendorInvItems.Select(x => x.Copy()).ToList();

            SaveNewOrderCommand = new RelayCommand(SaveOrder, x => SaveOrderCanExecute);
            CancelNewOrderCommand = new RelayCommand(CancelOrder);
            ClearOrderCommand = new RelayCommand(ClearOrderAmounts);
            AutoSelectVendorCommand = new RelayCommand(AutoSelectVendor);

            //RefreshInventoryList();
            //SetLastOrderBreakdown();
            ShowSelectVendor();
            VendorList = new ObservableCollection<Vendor>(_models.Vendors);
            OrderVendor = null;
            PurchasedUnitList = _models.GetPurchasedUnits();
        }

        #region ICommand Helpers
        /// <summary>
        /// Writes the inventory items to DB as they are in the New Order datagrid
        /// </summary>
        /// <param name="obj"></param>
        private void SaveOrder(object obj)
        {
            foreach (VendorInventoryItem item in _shownInvItems.Where(x => x.LastOrderAmount > 0))
            {
                InventoryItem invItem = item.ToInventoryItem();
                invItem.Update();
                VendorInventoryItem origVitem = _models.VendorInvItems.First(x => x.Id == item.Id);
                origVitem.SetVendorItem(item.SelectedVendor, invItem);
                origVitem.SelectedVendor = item.SelectedVendor;
            }

            List<InventoryItem> purchasedItems = _shownInvItems.Where(x => x.LastOrderAmount > 0).Select(x => x.ToInventoryItem()).ToList();
            PurchaseOrder po = new PurchaseOrder(OrderVendor, purchasedItems, OrderDate);

            GenerateAfterOrderSaved(po, OrderVendor);

            _models.AddPurchaseOrder(po);
            RefreshOrder();
            ParentContext.InventoryTab.InvListVM.UpdateInvValue();

            Close();
        }

        /// <summary>
        /// Resets order amount values to last saved order amount value in New Order datagrid
        /// </summary>
        /// <param name="obj"></param>
        private void CancelOrder(object obj)
        {
            foreach (InventoryItem item in _sortedInvtems)
            {
                item.LastOrderAmount = item.GetPrevOrderAmount();
            }

            RefreshInventoryList();
            OrderVendor = null;
            Close();
        }

        /// <summary>
        /// Sets order amounts to 0 in New Order datagrid
        /// </summary>
        /// <param name="obj"></param>
        private void ClearOrderAmounts(object obj)
        {
            foreach (InventoryItem item in _sortedInvtems)
            {
                item.LastOrderAmount = 0;
            }

            LoadVendorItems();
        }

        private void AutoSelectVendor(object obj)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Initializers
        /// <summary>
        /// Creates observable collection that is used to display the price breakdown by category of the current order
        /// </summary>
        /// <param name="orderedItems"></param>
        /// <param name="total"></param>
        /// <returns></returns>
        private ObservableCollection<BreakdownCategoryItem> GetOrderBreakdown(IEnumerable<InventoryItem> orderedItems, out float total)
        {
            ObservableCollection<BreakdownCategoryItem> breakdown = new ObservableCollection<BreakdownCategoryItem>();
            total = 0;

            if (orderedItems != null)
            {
                foreach (string category in _models.GetInventoryCategories())
                {
                    IEnumerable<InventoryItem> items = orderedItems.Where(x => x.Category.ToUpper() == category.ToUpper() && x.LastOrderAmount > 0);
                    if (items.Count() > 0)
                    {
                        BreakdownCategoryItem bdItem = new BreakdownCategoryItem(items);
                        bdItem.Background = _models.GetCategoryColorHex(category);
                        bdItem.OrderVendor = OrderVendor;
                        breakdown.Add(bdItem);

                        total += bdItem.TotalAmount;
                    }
                }
            }

            return breakdown;
        }

        private void SetLastOrderBreakdown()
        {
            float oTotal = 0;
            BreakdownContext = new OrderBreakdownVM()
            {
                BreakdownList = GetOrderBreakdown(_shownInvItems, out oTotal),
                OrderTotal = oTotal + (OrderVendor != null ? OrderVendor.ShippingCost : 0),
                OrderVendor = OrderVendor,
                Header = "Price Breakdown"
            };
        }
        #endregion

        #region Update UI
        /// <summary>
        /// Called when New Order is edited
        /// </summary>
        public void RowEdited(VendorInventoryItem item)
        {
            //_editedIds.Add(item.Id);

            NotifyPropertyChanged("FilteredOrderItems");
            SetLastOrderBreakdown();
        }

        /// <summary>
        /// Filter list of inventory items based on the string in the filter box above datagrids
        /// </summary>
        public void FilterInventoryItems()
        {
            FilteredOrderItems = MainHelper.FilterInventoryItems(FilterText, _sortedInvtems);
            NotifyPropertyChanged("FilteredOrderItems");
        }

        /// <summary>
        /// When user selects a vendor, the prices update to match the last purchased price from that vendor
        /// </summary>
        private void LoadVendorItems()
        {
            FilterText = "";

            if (OrderVendor != null)
                _shownInvItems = _sortedInvtems.Where(x => x.Vendors.Select(y => y.Id).Contains(OrderVendor.Id)).ToList();
            else
                _shownInvItems = null;

            if (_shownInvItems != null && _shownInvItems.Count > 0)
            {
                foreach (VendorInventoryItem item in _shownInvItems)
                {
                    item.SelectedVendor = OrderVendor;
                }
                FilteredOrderItems = new ObservableCollection<VendorInventoryItem>(_shownInvItems);
                UnitVisibility = Visibility.Visible;
            }
            else
            {
                ShowMissingVendorItems();
                _shownInvItems = null;
                UnitVisibility = Visibility.Hidden;
            }
            SetLastOrderBreakdown();
        }

        /// <summary>
        /// Method used to update values that have been edited from the master list or vendor list
        /// </summary>
        /// <param name="item"></param>
        public void EditedItem(VendorInventoryItem item)
        {
            int idx = _sortedInvtems.FindIndex(x => x.Id == item.Id);
            _sortedInvtems[idx] = item;
            LoadVendorItems();
        }

        private void ShowSelectVendor()
        {
            FilteredOrderItems = new ObservableCollection<VendorInventoryItem>() { new VendorInventoryItem() { Name = "Please Select Vendor" } };
        }

        private void ShowMissingVendorItems()
        {
            FilteredOrderItems = new ObservableCollection<VendorInventoryItem>() { new VendorInventoryItem() { Name = "Vendor has no items" } };
        }

        #endregion

        private void RefreshInventoryList()
        {
            LoadVendorItems();
        }

        private void GenerateAfterOrderSaved(PurchaseOrder po, Vendor vendor)
        {
            new Thread(delegate ()
            {
                ReportGenerator generator = new ReportGenerator(_models);
                string xlsPath = generator.GenerateOrder(po, vendor);
                generator.GenerateReceivingList(po, vendor);
                generator.Close();
                System.Diagnostics.Process.Start(xlsPath);
            }).Start();
        }
    }
}
