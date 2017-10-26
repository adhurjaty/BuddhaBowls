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
        //private RefreshDel RefreshOrders;

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

        public ViewOrderVM(PurchaseOrder po)
        {
            _order = po;
            _tabControl = new ViewOrderTabControl(this);
            //RefreshOrders = refresh;

            SaveCommand = new RelayCommand(SaveEdits, x => SaveButtonCanExecute);
            CancelCommand = new RelayCommand(CancelView);
            InitBreakdown();
        }

        #region ICommand Helpers

        private void CancelView(object obj)
        {
            Close();
        }

        private void SaveEdits(object obj)
        {
            //RefreshOrders();
            Close();
        }
        #endregion

        #region Initializers
        private void InitBreakdown()
        {
            List<InventoryItem> items = _order.GetPOItems();

            // check where duplicates are being formed. Remove this if I find the source
            items = _order.RemoveViewingDuplicates(items);

            string header = _order.Received ? "Received Ordered Items" : "Open Ordered Items";

            float oTotal = 0;
            BreakdownContext = new OrderBreakdownVM()
            {
                BreakdownList = GetOrderBreakdown(items, out oTotal),
                //OrderTotal = oTotal,
                OrderVendor = _models.VContainer.Items.First(x => x.Name == _order.VendorName),
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
                        BreakdownCategoryItem bdItem = new BreakdownCategoryItem(items, OrderEdited, false);
                        bdItem.Background = _models.GetCategoryColorHex(category);
                        bdItem.OrderVendor = _models.VContainer.Items.FirstOrDefault(x => x.Name == _order.VendorName);
                        breakdown.Add(bdItem);
                        total += bdItem.TotalAmount;
                    }
                }
            }

            return breakdown;
        }

        private void OrderEdited(InventoryItem item)
        {
            _order.UpdateItem(item);
            BreakdownContext.UpdateTotal();

            // check whether the item being edited is the latest order from this vendor. If so, then change the properties of the current
            // inventory item (last order price, last order qty...)
            PurchaseOrder latestOrderFromVendor = _models.POContainer.Items.Where(x => x.VendorName == _order.VendorName)
                                                                        .OrderByDescending(x => x.OrderDate).First();
            if (latestOrderFromVendor.Id == _order.Id)
            {
                Vendor vend = _models.VContainer.Items.First(x => x.Name == _order.VendorName);
                //_models.AddUpdateInventoryItem(ref item);
                
                vend.Update(item);
                VendorInventoryItem vItem = _models.VIContainer.Items.FirstOrDefault(x => x.Id == item.Id);
                if (vItem != null)
                {
                    vItem.SetVendorItem(vend, item);
                }
            }
        }
    }
}
