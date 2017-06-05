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

        //private OrderBreakdownVM _receivedBreakdownContext;
        //public OrderBreakdownVM ReceivedBreakdownContext
        //{
        //    get
        //    {
        //        return _receivedBreakdownContext;
        //    }
        //    set
        //    {
        //        _receivedBreakdownContext = value;
        //        NotifyPropertyChanged("ReceivedBreakdownContext");
        //    }
        //}
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

            MoveToReceivedCommand = new RelayCommand(MoveToReceived, x => MoveToReceivedCanExecute);
            MoveToOpenCommand = new RelayCommand(MoveToOpen, x => MoveToOpenCanExecute);
            SaveCommand = new RelayCommand(SavePartialOrder, x => SaveButtonCanExecute);
            CancelCommand = new RelayCommand(CancelView);

            InitBreakdown(po);
        }

        #region ICommand Helpers
        private void MoveToReceived(object obj)
        {
            //MoveItem(BreakdownContext, ReceivedBreakdownContext);
        }

        private void MoveToOpen(object obj)
        {
            //MoveItem(ReceivedBreakdownContext, BreakdownContext);
        }

        private void MoveItem(OrderBreakdownVM fromContext, OrderBreakdownVM toContext)
        {
            List<InventoryItem> fromList = fromContext.GetInventoryItems();
            List<InventoryItem> toList = toContext.GetInventoryItems();

            InventoryItem selected = fromContext.SelectedItem;
            fromList.Remove(selected);
            toList.Add(selected);

            float oTotal = 0;
            fromContext.BreakdownList = GetOrderBreakdown(fromList, out oTotal);
            fromContext.OrderTotal = oTotal;

            toContext.BreakdownList = GetOrderBreakdown(toList, out oTotal);
            toContext.OrderTotal = oTotal;
        }

        private void CancelView(object obj)
        {
            Close();
        }

        private void SavePartialOrder(object obj)
        {
            List<InventoryItem> openItems = BreakdownContext.GetInventoryItems();
            List<InventoryItem> receivedItems = BreakdownContext.GetInventoryItems();
            //List<InventoryItem> receivedItems = ReceivedBreakdownContext.GetInventoryItems();

            if (receivedItems.Count == 0)
            {
                _order.ReceivedDate = null;
                _order.Update();
            }
            else if (openItems.Count != 0)
            {
                _order.SplitToPartials(openItems, receivedItems);
            }

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
            string header = openItems != null ? "Open Ordered Items" : "Received Ordered Items";

            float oTotal = 0;
            BreakdownContext = new OrderBreakdownVM()
            {
                BreakdownList = GetOrderBreakdown(items, out oTotal),
                OrderTotal = oTotal,
                OrderVendor = _models.Vendors.First(x => x.Name == _order.VendorName),
                Header = header
            };

            //BreakdownContext = new OrderBreakdownVM()
            //{
            //    BreakdownList = GetOrderBreakdown(openItems, out oTotal),
            //    OrderTotal = oTotal,
            //    Header = "Open Ordered Items"
            //};

            //ReceivedBreakdownContext = new OrderBreakdownVM()
            //{
            //    BreakdownList = GetOrderBreakdown(receivedItems, out oTotal),
            //    OrderTotal = oTotal,
            //    Header = "Received Ordered Items"
            //};
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
