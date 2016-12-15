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
    public class OrderTabVM : INotifyPropertyChanged
    {
        private MainWindow _window;
        private ModelContainer _models;

        // INotifyPropertyChanged event and method
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainViewModel ParentContext { get; set; }

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

        private ObservableCollection<PurchaseOrder> _openOrders;
        public ObservableCollection<PurchaseOrder> OpenOrders
        {
            get
            {
                return _openOrders;
            }
            set
            {
                _openOrders = value;
                NotifyPropertyChanged("OpenOrders");
            }
        }

        // Collection used for both Master List and New Order List
        public ObservableCollection<InventoryItem> FilteredOrderItems
        {
            get
            {
                return ParentContext.FilteredInventoryItems;
            }
            set
            {
                ParentContext.FilteredInventoryItems = value;
                NotifyPropertyChanged("FilteredOrderItems");
            }
        }

        private ObservableCollection<PurchaseOrder> _receivedOrders;
        public ObservableCollection<PurchaseOrder> ReceivedOrders
        {
            get
            {
                return _receivedOrders;
            }
            set
            {
                _receivedOrders = value;
                NotifyPropertyChanged("ReceivedOrders");
            }
        }

        public PurchaseOrder SelectedOrder { get; set; }

        // name of the vendor in the New Order form
        public string OrderVendor { get; set; }

        #endregion

        #region ICommand Bindings and Can Execute
        // Save button in New Order form
        public ICommand SaveNewOrderCommand { get; set; }
        // Reset button in New Order form
        public ICommand CancelNewOrderCommand { get; set; }
        // Clear Amts button in New Order form
        public ICommand ClearOrderCommand { get; set; }
        // Received button in Order Overview form
        public ICommand ReceivedOrdersCommand { get; set; }
        // Clear button in Order Overview form
        public ICommand ClearReceivedCheckCommand { get; set; }
        // View button in Order Overview form
        public ICommand ViewOrderCommand { get; set; }
        // Plus button in Order Overview form
        public ICommand AddNewOrderCommand { get; set; }
        // Minus button in Order Overview form for open orders
        public ICommand DeleteOrderCommand { get; set; }
        // Minus button in Order Overview form for received orders
        public ICommand DeleteReceivedOrderCommand { get; set; }

        public bool SaveOrderCanExecute
        {
            get
            {
                return !string.IsNullOrWhiteSpace(OrderVendor) && _models.InventoryItems.FirstOrDefault(x => x.LastOrderAmount > 0) != null;
            }
        }

        public bool ViewOrderCanExecute
        {
            get
            {
                return SelectedOrder != null;
            }
        }

        public bool RemoveOpenOrderCanExecute
        {
            get
            {
                return SelectedOrder != null && SelectedOrder.ReceivedDate == null;
            }
        }

        public bool RemoveReceivedOrderCanExecute
        {
            get
            {
                return SelectedOrder != null && SelectedOrder.ReceivedDate != null;
            }
        }
        #endregion

        public OrderTabVM(ModelContainer models, MainViewModel parent)
        {
            ParentContext = parent;
            if (models == null)
            {
                TryDBConnect(false);
            }
            else
            {
                _models = models;

                SaveNewOrderCommand = new RelayCommand(SaveOrder, x => SaveOrderCanExecute);
                CancelNewOrderCommand = new RelayCommand(CancelOrder);
                ClearOrderCommand = new RelayCommand(ClearOrderAmounts);
                ReceivedOrdersCommand = new RelayCommand(MoveReceivedOrders);
                ClearReceivedCheckCommand = new RelayCommand(ClearReceivedChecks);
                ViewOrderCommand = new RelayCommand(ViewOrder, x => ViewOrderCanExecute);
                AddNewOrderCommand = new RelayCommand(StartNewOrder);
                DeleteOrderCommand = new RelayCommand(RemoveOpenOrder, x => RemoveOpenOrderCanExecute);
                DeleteReceivedOrderCommand = new RelayCommand(RemoveReceivedOrder, x => RemoveReceivedOrderCanExecute);

                TryDBConnect(true);
            }
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

            PurchaseOrder po = new PurchaseOrder(OrderVendor, _models.InventoryItems.Where(x => x.LastOrderAmount > 0).ToList());

            OrderVendor = "";
            _window.DeleteTempTab();
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
            OrderVendor = "";
            _window.DeleteTempTab();
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
        }

        /// <summary>
        /// Reset the check boxes in open orders data grid to unchecked
        /// </summary>
        /// <param name="obj"></param>
        private void ClearReceivedChecks(object obj)
        {
            foreach (PurchaseOrder order in OpenOrders)
            {
                order.Received = false;
            }

            NotifyPropertyChanged("OpenOrders");
        }

        /// <summary>
        /// Move the received orders
        /// </summary>
        /// <param name="obj"></param>
        private void MoveReceivedOrders(object obj)
        {
            foreach (PurchaseOrder order in OpenOrders.Where(x => x.Received))
            {
                order.ReceivedDate = DateTime.Now;
                order.Update();
            }

            LoadPreviousOrders();
        }

        private void ViewOrder(object obj)
        {
            _window.AddTempTab("PO#: " + SelectedOrder.Id, new NewOrder(this));
        }

        /// <summary>
        /// Display dialog to user to delete, unreceive, or partial receive received order and take appropriate action on user input
        /// </summary>
        /// <param name="obj"></param>
        private void RemoveReceivedOrder(object obj)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Display dialog to user to delete open order
        /// </summary>
        /// <param name="obj"></param>
        private void RemoveOpenOrder(object obj)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Open new tab to create a new order
        /// </summary>
        /// <param name="obj"></param>
        private void StartNewOrder(object obj)
        {
            SetLastOrderBreakdown();
            _window.AddTempTab("New Order", new NewOrder(this));
        }
        #endregion

        #region Initializers
        public void InitializeWindow(MainWindow window)
        {
            _window = window;
        }

        /// <summary>
        /// Populate the 2 dataGrids in the Orders overview
        /// </summary>
        /// <returns></returns>
        private bool LoadPreviousOrders()
        {
            if (_models != null && _models.PurchaseOrders != null)
            {
                OpenOrders = new ObservableCollection<PurchaseOrder>(_models.PurchaseOrders.Where(x => !x.Received).OrderBy(x => x.OrderDate));
                ReceivedOrders = new ObservableCollection<PurchaseOrder>(_models.PurchaseOrders.Where(x => x.Received).OrderBy(x => x.ReceivedDate));
                return true;
            }

            return false;
        }

        private void OrdersNotFound()
        {
            OpenOrders = new ObservableCollection<PurchaseOrder>() { new PurchaseOrder() { Company = "Orders not found" } };
            ReceivedOrders = new ObservableCollection<PurchaseOrder>() { new PurchaseOrder() { Company = "Orders not found" } };
        }

        private ObservableCollection<BreakdownCategoryItem> GetOrderBreakdown(IEnumerable<InventoryItem> orderedItems, out float total)
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
                OrderTotal = oTotal
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

        public void MoveOrderToReceived(PurchaseOrder po)
        {
            po.ReceivedDate = DateTime.Now;
            LoadPreviousOrders();
        }

        /// <summary>
        /// Filter list of inventory items based on the string in the filter box above datagrids
        /// </summary>
        /// <param name="filterStr"></param>
        public void FilterInventoryItems(string filterStr)
        {
            ParentContext.FilterInventoryItems(filterStr);
            NotifyPropertyChanged("FilteredOrderItems");
        }
        #endregion

        private bool TryDBConnect(bool connection)
        {
            RefreshInventoryList();

            if (!LoadPreviousOrders() || !connection)
            {
                OrdersNotFound();
                return false;
            }
            return true;
        }

        private void RefreshInventoryList()
        {
            ParentContext.RefreshInventoryList();
            NotifyPropertyChanged("FilteredInvetoryItems");
        }
    }
}
