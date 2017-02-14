using BuddhaBowls.Models;
using BuddhaBowls.Services;
using BuddhaBowls.UserControls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace BuddhaBowls
{
    public class OrderTabVM : INotifyPropertyChanged
    {
        private ModelContainer _models;

        // INotifyPropertyChanged event and method
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainViewModel ParentContext { get; set; }

        #region Content Binders
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

        private PurchaseOrder _selectedOpenOrder;
        public PurchaseOrder SelectedOpenOrder
        {
            get
            {
                return _selectedOpenOrder;
            }
            set
            {
                _selectedOpenOrder = value;

                if (value != null && value.IsPartial)
                    _selectedReceivedOrder = value;
                else
                    _selectedReceivedOrder = null;

                NotifyPropertyChanged("SelectedOpenOrder");
                NotifyPropertyChanged("SelectedReceivedOrder");
            }
        }

        private PurchaseOrder _selectedReceivedOrder;
        public PurchaseOrder SelectedReceivedOrder
        {
            get
            {
                return _selectedReceivedOrder;
            }
            set
            {
                _selectedReceivedOrder = value;

                if (value != null && value.IsPartial)
                    _selectedOpenOrder = value;
                else
                    _selectedOpenOrder = null;

                NotifyPropertyChanged("SelectedOpenOrder");
                NotifyPropertyChanged("SelectedReceivedOrder");
            }
        }
        #endregion

        #region ICommand Bindings and Can Execute
        // Received button in Order Overview form
        public ICommand ReceivedOrdersCommand { get; set; }
        // Clear button in Order Overview form
        public ICommand ClearReceivedCheckCommand { get; set; }
        // View button in Order Overview form
        public ICommand ViewOpenOrderCommand { get; set; }
        // View button in Received order data grid
        public ICommand ViewReceivedOrderCommand { get; set; }
        // Plus button in Order Overview form
        public ICommand AddNewOrderCommand { get; set; }
        // Minus button in Order Overview form for open orders
        public ICommand DeleteOpenOrderCommand { get; set; }
        // Minus button in Order Overview form for received orders
        public ICommand DeleteReceivedOrderCommand { get; set; }
        // Re-Open button
        public ICommand ReOpenOrderCommand { get; set; }
        // Open PO button in Open orders
        public ICommand OpenOpenPOCommand { get; set; }
        // Open PO button in Received orders
        public ICommand OpenReceivedPOCommand { get; set; }
        // Open Rec button in Open orders
        public ICommand OpenRecListCommand { get; set; }
        // Open Rec button in Open orders
        public ICommand ReceivedRecListCommand { get; set; }

        public bool ViewOpenOrderCanExecute
        {
            get
            {
                return SelectedOpenOrder != null;
            }
        }

        public bool ViewReceivedOrderCanExecute
        {
            get
            {
                return SelectedReceivedOrder != null;
            }
        }

        public bool RemoveOpenOrderCanExecute
        {
            get
            {
                return SelectedOpenOrder != null;
            }
        }

        public bool RemoveReceivedOrderCanExecute
        {
            get
            {
                return SelectedReceivedOrder != null;
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

                ReceivedOrdersCommand = new RelayCommand(MoveReceivedOrders);
                ClearReceivedCheckCommand = new RelayCommand(ClearReceivedChecks);
                ViewOpenOrderCommand = new RelayCommand(ViewOpenOrder, x => ViewOpenOrderCanExecute);
                ViewReceivedOrderCommand = new RelayCommand(ViewReceivedOrder, x => ViewReceivedOrderCanExecute);
                AddNewOrderCommand = new RelayCommand(StartNewOrder);
                DeleteOpenOrderCommand = new RelayCommand(RemoveOpenOrder, x => RemoveOpenOrderCanExecute);
                DeleteReceivedOrderCommand = new RelayCommand(RemoveReceivedOrder, x => RemoveReceivedOrderCanExecute);
                ReOpenOrderCommand = new RelayCommand(ReOpenOrder, x => ViewReceivedOrderCanExecute);
                OpenOpenPOCommand = new RelayCommand(ShowOpenPO, x => ViewOpenOrderCanExecute);
                OpenReceivedPOCommand = new RelayCommand(ShowReceivedPO, x => ViewReceivedOrderCanExecute);
                OpenRecListCommand = new RelayCommand(ShowOpenRecList, x => ViewOpenOrderCanExecute);
                ReceivedRecListCommand = new RelayCommand(ShowReceivedRecList, x => ViewReceivedOrderCanExecute);

                TryDBConnect(true);
            }
        }

        #region ICommand Helpers
        /// <summary>
        /// Reset the check boxes in open orders data grid to unchecked
        /// </summary>
        /// <param name="obj"></param>
        private void ClearReceivedChecks(object obj)
        {
            foreach (PurchaseOrder order in OpenOrders)
            {
                order.ReceivedCheck = false;
            }

            RefreshOrderList();
        }

        /// <summary>
        /// Move the received orders
        /// </summary>
        /// <param name="obj"></param>
        private void MoveReceivedOrders(object obj)
        {
            foreach (PurchaseOrder order in OpenOrders.Where(x => x.ReceivedCheck))
            {
                order.Receive();
            }

            RefreshOrderList();
        }

        private void ReOpenOrder(object obj)
        {
            SelectedReceivedOrder.ReOpen();
            RefreshOrderList();
        }

        private void ViewOpenOrder(object obj)
        {
            ViewOrder(SelectedOpenOrder);
        }

        private void ViewReceivedOrder(object obj)
        {
            ViewOrder(SelectedReceivedOrder);
        }

        private void ViewOrder(PurchaseOrder order)
        {
            ViewOrderVM context = new ViewOrderVM(this, order);
            ViewOrderTabControl userControl = new ViewOrderTabControl(context);
            ParentContext.AddTempTab("PO#: " + order.Id, userControl);
        }

        /// <summary>
        /// Display dialog to user to delete, unreceive, or partial receive received order and take appropriate action on user input
        /// </summary>
        /// <param name="obj"></param>
        private void RemoveReceivedOrder(object obj)
        {
            DeleteOrder(SelectedReceivedOrder);
        }

        /// <summary>
        /// Display dialog to user to delete open order
        /// </summary>
        /// <param name="obj"></param>
        private void RemoveOpenOrder(object obj)
        {
            if (SelectedOpenOrder.IsPartial)
            {
                MessageBoxResult result = MessageBox.Show("Are you sure you want to delete partial order PO# " + SelectedOpenOrder.Id.ToString(),
                                                          "Delete PO# " + SelectedOpenOrder.Id.ToString() + "?", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    SelectedOpenOrder.DeleteOpenPartial();
                    RefreshOrderList();
                }
            }
            else
            {
                DeleteOrder(SelectedOpenOrder);
            }
        }

        private void DeleteOrder(PurchaseOrder order)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete PO# " + order.Id.ToString(),
                                                      "Delete PO# " + order.Id.ToString() + "?", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                order.Destroy();
                _models.PurchaseOrders.Remove(order);
                RefreshOrderList();
            }
        }

        /// <summary>
        /// Open new tab to create a new order
        /// </summary>
        /// <param name="obj"></param>
        private void StartNewOrder(object obj)
        {
            //SetLastOrderBreakdown();
            ParentContext.AddTempTab("New Order", new NewOrder(new NewOrderVM(_models, this)));
        }

        private void ShowOpenPO(object obj)
        {
            ShowPO(SelectedOpenOrder);
        }

        private void ShowReceivedPO(object obj)
        {
            ShowPO(SelectedReceivedOrder);
        }

        private void ShowPO(PurchaseOrder po)
        {
            try
            {
                if (File.Exists(po.GetPOPath()))
                {
                    System.Diagnostics.Process.Start(po.GetPOPath());
                }
                else
                {
                    Vendor v = _models.Vendors.FirstOrDefault(x => x.Name == po.VendorName);
                    if (v != null)
                    {
                        ParentContext.GeneratePO(po, v);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Purchase order is open. Close it if you wish to overwrite");
            }
        }

        private void ShowOpenRecList(object obj)
        {
            ShowRecList(SelectedOpenOrder);
        }

        private void ShowReceivedRecList(object obj)
        {
            ShowRecList(SelectedReceivedOrder);
        }

        private void ShowRecList(PurchaseOrder po)
        {
            try
            {
                string poPath = Path.Combine(Properties.Settings.Default.DBLocation, "Receiving Lists", "ReceivingList_" + po.Id.ToString());
                if (File.Exists(poPath))
                {
                    System.Diagnostics.Process.Start(poPath);
                }
                else
                {
                    Vendor v = _models.Vendors.FirstOrDefault(x => x.Name == po.VendorName);
                    if (v != null)
                    {
                        ParentContext.GenerateReceivingList(po, v);
                    }
                }
            }
            catch(Exception e)
            {
                MessageBox.Show("Receiving list is open. Close it if you wish to overwrite");
            }
        }

        #endregion

        #region Initializers
        private bool TryDBConnect(bool connection)
        {
            if (!LoadPreviousOrders() || !connection)
            {
                OrdersNotFound();
                return false;
            }

            //RefreshInventoryList();
            return true;
        }

        /// <summary>
        /// Populate the 2 dataGrids in the Orders overview
        /// </summary>
        /// <returns></returns>
        public bool LoadPreviousOrders()
        {
            if (_models != null && _models.PurchaseOrders != null)
            {
                OpenOrders = new ObservableCollection<PurchaseOrder>(_models.PurchaseOrders.Where(x => !x.Received || x.IsPartial)
                                                                        .OrderBy(x => x.OrderDate));
                ReceivedOrders = new ObservableCollection<PurchaseOrder>(_models.PurchaseOrders.Where(x => x.Received)
                                                                        .OrderByDescending(x => x.ReceivedDate));
                return true;
            }

            return false;
        }

        private void OrdersNotFound()
        {
            OpenOrders = new ObservableCollection<PurchaseOrder>() { new PurchaseOrder() { VendorName = "Orders not found" } };
            ReceivedOrders = new ObservableCollection<PurchaseOrder>() { new PurchaseOrder() { VendorName = "Orders not found" } };
        }

        public ObservableCollection<BreakdownCategoryItem> GetOrderBreakdown(IEnumerable<InventoryItem> orderedItems, out float total)
        {
            ObservableCollection<BreakdownCategoryItem> breakdown = new ObservableCollection<BreakdownCategoryItem>();
            total = 0;

            if (orderedItems != null)
            {
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
            }

            return breakdown;
        }
        #endregion

        #region Update UI
        public void MoveOrderToReceived(PurchaseOrder po)
        {
            po.ReceivedDate = DateTime.Now;
            RefreshOrderList();
        }

        public void RefreshOrderList()
        {
            LoadPreviousOrders();
        }
        #endregion

        public void DeleteTempTab()
        {
            ParentContext.DeleteTempTab();
        }
    }
}
