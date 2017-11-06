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
        private VendorInvItemsContainer _itemsContainer;
        private List<VendorInventoryItem> _displayItems;
        //private RefreshDel RefreshOrder;
        // stores vendor inventory offering
        //private List<VendorInventoryItem> _sortedInvtems;
        //private List<VendorInventoryItem> _shownInvItems;

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
        private ObservableCollection<Vendor> _vendorList;
        public ObservableCollection<Vendor> VendorList
        {
            get
            {
                return _vendorList;
            }
            set
            {
                _vendorList = value;
                NotifyPropertyChanged("VendorList");
            }
        }

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

        private bool _isReceipt = false;
        public bool IsReceipt
        {
            get
            {
                return _isReceipt;
            }
            set
            {
                _isReceipt = value;
                NotifyPropertyChanged("IsReceipt");
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
        // Adds item to vendor's sold items
        public ICommand AddNewItemCommand { get; set; }
        // Deletes item from vendor's sold items
        public ICommand DeleteItemCommand { get; set; }

        public bool SaveOrderCanExecute
        {
            get
            {
                return OrderVendor != null && FilteredOrderItems.FirstOrDefault(x => x.LastOrderAmount > 0) != null;
            }
        }
        #endregion

        public NewOrderVM() : base()
        {
            //RefreshOrder = refresh;
            _tabControl = new NewOrder(this);
            //_sortedInvtems = _models.VendorInvItems.Select(x => x.Copy()).ToList();

            SaveNewOrderCommand = new RelayCommand(SaveOrder, x => SaveOrderCanExecute);
            CancelNewOrderCommand = new RelayCommand(CancelOrder);
            ClearOrderCommand = new RelayCommand(ClearOrderAmounts);
            AutoSelectVendorCommand = new RelayCommand(AutoSelectVendor);
            AddNewItemCommand = new RelayCommand(AddVendorItem, x => OrderVendor != null);
            DeleteItemCommand = new RelayCommand(DeleteVendorItem, x => OrderVendor != null);

            //RefreshInventoryList();
            //SetLastOrderBreakdown();
            ShowSelectVendor();
            RefreshVendors();
            PurchasedUnitList = _models.GetPurchasedUnits();
            _itemsContainer = _models.VIContainer.Copy();
            _models.VContainer.AddUpdateBinding(RefreshVendors);
            _models.VContainer.AddUpdateBinding(LoadVendorItems);
        }

        #region ICommand Helpers
        /// <summary>
        /// Writes the inventory items to DB as they are in the New Order datagrid
        /// </summary>
        /// <param name="obj"></param>
        private void SaveOrder(object obj)
        {
            FilterText = "";
            // close at top so the copy gets removed in dbcache and there are fewer updates to run
            Close();

            List<InventoryItem> purchasedItems = _displayItems.Select(x => x.ToInventoryItem()).ToList();
            PurchaseOrder po = new PurchaseOrder(OrderVendor, purchasedItems.Where(x => x.LastOrderAmount > 0).ToList(), OrderDate);

            if (IsReceipt)
                po.Receive();

            GenerateAfterOrderSaved(po, OrderVendor, !IsReceipt);

            _models.POContainer.AddItem(po);

            //_models.VIContainer.Update(purchasedVItems);
            foreach (VendorInventoryItem item in _displayItems)
            {
                //item.Update();
                VendorInventoryItem masterItem = _models.VIContainer.Items.First(x => x.Id == item.Id);
                masterItem.SetVendorItem(OrderVendor, purchasedItems.First(x => x.Id == item.Id));
                //masterItem.Update();
            }
            OrderVendor.Update(purchasedItems);
        }

        /// <summary>
        /// Resets order amount values to last saved order amount value in New Order datagrid
        /// </summary>
        /// <param name="obj"></param>
        private void CancelOrder(object obj)
        {
            foreach (InventoryItem item in _displayItems)
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
            FilterText = "";

            foreach (VendorInventoryItem item in _displayItems)
            {
                item.LastOrderAmount = 0;
            }

            SetLastOrderBreakdown();
            //LoadVendorItems();
        }

        private void AutoSelectVendor(object obj)
        {
            throw new NotImplementedException();
        }

        private void AddVendorItem(object obj)
        {
            List<InventoryItem> remainingItems = _models.VIContainer.Items.Where(x => !_displayItems.Select(y => y.Id).Contains(x.Id))
                                                                          .Select(x => x.ToInventoryItem()).ToList();
            ModalVM<InventoryItem> modal = new ModalVM<InventoryItem>("Add Inv Item", remainingItems, AddInvItemToVendor);
            ParentContext.ModalContext = modal;
        }

        private void DeleteVendorItem(object obj)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to remove " + SelectedOrderItem.Name + " from " + OrderVendor.Name + "?",
                                                      "Remove " + SelectedOrderItem.Name + "?", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                _models.VIContainer.RemoveFromVendor(SelectedOrderItem, OrderVendor);
                _itemsContainer.RemoveItem(SelectedOrderItem);
                OrderVendor.Update();
                LoadVendorItems();
            }
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
                BreakdownList = GetOrderBreakdown(_displayItems, out oTotal),
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
            NotifyPropertyChanged("FilteredOrderItems");
            BreakdownContext.UpdateItem(item);
            NotifyPropertyChanged("BreakdownContext");
            //SetLastOrderBreakdown();
        }

        /// <summary>
        /// Filter list of inventory items based on the string in the filter box above datagrids
        /// </summary>
        public void FilterInventoryItems()
        {
            if (_displayItems != null)
            {
                FilteredOrderItems = MainHelper.FilterInventoryItems(FilterText, _displayItems);
                NotifyPropertyChanged("FilteredOrderItems");
            }
        }

        private void RefreshVendors()
        {
            VendorList = new ObservableCollection<Vendor>(_models.VContainer.Items);
        }

        /// <summary>
        /// When user selects a vendor, the prices update to match the last purchased price from that vendor
        /// </summary>
        private void LoadVendorItems()
        {
            FilterText = "";

            if (OrderVendor != null && _itemsContainer != null && _itemsContainer.Items.Count > 0)
            {
                if(_displayItems != null)
                    _itemsContainer.Update(_displayItems);
                // want to preserve counts while loading different vendors - more challenging than anticipated. May come back
                //if(_displayItems != null)
                //    _itemsContainer.Update(_displayItems);

                _displayItems = _itemsContainer.Items.Where(x => x.Vendors.Select(y => y.Id).Contains(OrderVendor.Id)).ToList();
                foreach (VendorInventoryItem item in _displayItems)
                {
                    item.SelectedVendor = OrderVendor;
                }
                FilteredOrderItems = new ObservableCollection<VendorInventoryItem>(_displayItems);
                UnitVisibility = Visibility.Visible;
            }
            else
            {
                ShowMissingVendorItems();
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
            int idx = _itemsContainer.Items.FindIndex(x => x.Id == item.Id);
            _itemsContainer.Items[idx] = item;
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

        protected override void Close()
        {
            _models.VIContainer.RemoveCopy(_itemsContainer);
            base.Close();
        }
        #endregion

        private void RefreshInventoryList()
        {
            LoadVendorItems();
        }

        private void AddInvItemToVendor(InventoryItem item)
        {
            _models.VIContainer.UpdateItem(item, OrderVendor);
            OrderVendor.Update();
            LoadVendorItems();
        }

        private void GenerateAfterOrderSaved(PurchaseOrder po, Vendor vendor, bool openExcel)
        {
            new Thread(delegate ()
            {
                ReportGenerator generator = new ReportGenerator(_models);
                string xlsPath = generator.GenerateOrder(po, vendor);
                generator.GenerateReceivingList(po, vendor);
                generator.Close();
                if(openExcel)
                    System.Diagnostics.Process.Start(xlsPath);
            }).Start();
        }
    }
}
