using BuddhaBowls.Models;
using BuddhaBowls.Services;
using BuddhaBowls.UserControls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BuddhaBowls
{
    /// <summary>
    /// Temp tab to view or edit an open or received order - opened from the Orders tab
    /// </summary>
    public class ViewOrderVM : TempTabVM
    {
        private PurchaseOrder _order;
        private RefreshDel RefreshOrders;

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

        #endregion

        #region ICommand and CanExecute Properties 
        // > Button
        public ICommand MoveToReceivedCommand { get; set; }
        // < Button
        public ICommand MoveToOpenCommand { get; set; }
        // Save Button
        public ICommand SaveCommand { get; set; }
        // Cancel Button
        public ICommand CancelCommand { get; set; }

        public bool MoveToReceivedCanExecute
        {
            get
            {
                return BreakdownContext.SelectedItem != null;
            }
        }

        public bool MoveToOpenCanExecute
        {
            get
            {
                return true; //ReceivedBreakdownContext.SelectedItem != null;
            }
        }

        public bool SaveButtonCanExecute
        {
            get
            {
                return true;
            }
        }
        #endregion

        public ViewOrderVM(PurchaseOrder po, RefreshDel refresh)
        {
            _order = po;
            _tabControl = new ViewOrderTabControl(this);
            RefreshOrders = refresh;

            SaveCommand = new RelayCommand(SaveEdits, x => SaveButtonCanExecute);
            CancelCommand = new RelayCommand(CancelView);
            InitBreakdown(po);
        }

        #region ICommand Helpers

        private void CancelView(object obj)
        {
            Close();
        }

        private void SaveEdits(object obj)
        {
            RefreshOrders();
            Close();
        }
        #endregion

        #region Initializers
        private void InitBreakdown(PurchaseOrder po)
        {
            List<InventoryItem>[] poItems = po.GetPOItems();
            List<InventoryItem> openItems = poItems[0];
            List<InventoryItem> receivedItems = poItems[1];
            List<InventoryItem> items = openItems != null ? openItems : receivedItems;

            // check where duplicates are being formed. Remove this if I find the source
            items = po.RemoveViewingDuplicates(items);

            string header = openItems != null ? "Open Ordered Items" : "Received Ordered Items";

            float oTotal = 0;
            BreakdownContext = new OrderBreakdownVM()
            {
                BreakdownList = GetOrderBreakdown(items, out oTotal),
                OrderTotal = oTotal,
                OrderVendor = _models.Vendors.First(x => x.Name == _order.VendorName),
                Header = header
            };
            
        }
        #endregion

        public ObservableCollection<BreakdownCategoryItem> GetOrderBreakdown(IEnumerable<InventoryItem> orderedItems, out float total)
        {
            ObservableCollection<BreakdownCategoryItem> breakdown = new ObservableCollection<BreakdownCategoryItem>();
            total = 0;

            if (orderedItems != null)
            {
                foreach (string category in _models.GetInventoryCategories())
                {
                    IEnumerable<InventoryItem> items = orderedItems.Where(x => x.Category.ToUpper() == category.ToUpper());
                    if (items.Count() > 0)
                    {
                        BreakdownCategoryItem bdItem = new BreakdownCategoryItem(items, false);
                        bdItem.Background = _models.GetCategoryColorHex(category);
                        bdItem.OrderVendor = _models.Vendors.FirstOrDefault(x => x.Name == _order.VendorName);
                        breakdown.Add(bdItem);
                        total += bdItem.TotalAmount;
                    }
                }
            }

            return breakdown;
        }
    }
}
