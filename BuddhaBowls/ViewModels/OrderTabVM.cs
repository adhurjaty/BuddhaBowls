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
using System.Windows.Media;

namespace BuddhaBowls
{
    /// <summary>
    /// Permanent tab showing the open and received orders
    /// </summary>
    public class OrderTabVM : TabVM
    {
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
                SelectedReceivedOrder = null;

                NotifyPropertyChanged("SelectedOpenOrder");
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

                SelectedOpenOrder = null;

                NotifyPropertyChanged("SelectedOpenOrder");
                NotifyPropertyChanged("SelectedReceivedOrder");

                //ChangeOrderStats();
            }
        }

        private PeriodSelectorVM _periodSelector;
        public PeriodSelectorVM PeriodSelector
        {
            get
            {
                return _periodSelector;
            }
            set
            {
                _periodSelector = value;
                NotifyPropertyChanged("PeriodSelector");
            }
        }

        private OrderStat _totalOrders;
        public OrderStat TotalOrders
        {
            get
            {
                return _totalOrders;
            }
            set
            {
                _totalOrders = value;
                NotifyPropertyChanged("TotalOrders");
            }
        }

        private OrderStat _weekCostTotal;
        public OrderStat WeekCostTotal
        {
            get
            {
                return _weekCostTotal;
            }
            set
            {
                _weekCostTotal = value;
                NotifyPropertyChanged("WeekCostTotal");
            }
        }

        private string _weekUpDownIcon;
        public string WeekUpDownIcon
        {
            get
            {
                return _weekUpDownIcon;
            }
            set
            {
                _weekUpDownIcon = value;
                NotifyPropertyChanged("WeekUpDownIcon");
            }
        }

        private float _weekTrend;
        public float WeekTrend
        {
            get
            {
                return Math.Abs(_weekTrend);
            }
            set
            {
                _weekTrend = value;
                NotifyPropertyChanged("WeekTrend");

                if (_weekTrend < 0)
                    WeekUpDownIcon = "ArrowDown";
                else
                    WeekUpDownIcon = "ArrowUp";
            }
        }

        private OrderStat _periodCostTotal;
        public OrderStat PeriodCostTotal
        {
            get
            {
                return _periodCostTotal;
            }
            set
            {
                _periodCostTotal = value;
                NotifyPropertyChanged("PeriodCostTotal");
            }
        }

        private string _periodUpDownIcon;
        public string PeriodUpDownIcon
        {
            get
            {
                return _periodUpDownIcon;
            }
            set
            {
                _periodUpDownIcon = value;
                NotifyPropertyChanged("PeriodUpDownIcon");
            }
        }

        private float _periodTrend;
        public float PeriodTrend
        {
            get
            {
                return Math.Abs(_periodTrend);
            }
            set
            {
                _periodTrend = value;
                NotifyPropertyChanged("PeriodTrend");

                if (_periodTrend < 0)
                    PeriodUpDownIcon = "ArrowDown";
                else
                    PeriodUpDownIcon = "ArrowUp";
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

        public OrderTabVM() : base()
        {

            ReceivedOrdersCommand = new RelayCommand(MoveToReceivedOrders, x => DBConnection);
            ClearReceivedCheckCommand = new RelayCommand(ClearReceivedChecks, x => DBConnection);
            ViewOpenOrderCommand = new RelayCommand(ViewOpenOrder, x => ViewOpenOrderCanExecute && DBConnection);
            ViewReceivedOrderCommand = new RelayCommand(ViewReceivedOrder, x => ViewReceivedOrderCanExecute && DBConnection);
            AddNewOrderCommand = new RelayCommand(StartNewOrder, x => DBConnection);
            DeleteOpenOrderCommand = new RelayCommand(RemoveOpenOrder, x => RemoveOpenOrderCanExecute && DBConnection);
            DeleteReceivedOrderCommand = new RelayCommand(RemoveReceivedOrder, x => RemoveReceivedOrderCanExecute && DBConnection);
            ReOpenOrderCommand = new RelayCommand(ReOpenOrder, x => ViewReceivedOrderCanExecute && DBConnection);
            OpenOpenPOCommand = new RelayCommand(ShowOpenPO, x => ViewOpenOrderCanExecute && DBConnection);
            OpenReceivedPOCommand = new RelayCommand(ShowReceivedPO, x => ViewReceivedOrderCanExecute && DBConnection);
            OpenRecListCommand = new RelayCommand(ShowOpenRecList, x => ViewOpenOrderCanExecute && DBConnection);
            ReceivedRecListCommand = new RelayCommand(ShowReceivedRecList, x => ViewReceivedOrderCanExecute && DBConnection);

            if (DBConnection)
            {
                TotalOrders = new OrderStat() { Label = "Total Orders" };
                PeriodCostTotal = new OrderStat() { Label = "Total Cost for Period" };
                WeekCostTotal = new OrderStat() { Label = "Total Cost for Week" };

                PeriodSelector = new PeriodSelectorVM(_models, LoadPreviousOrders);
            }
            else
            {
                OrdersNotFound();
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
        private void MoveToReceivedOrders(object obj)
        {
            foreach (PurchaseOrder order in OpenOrders.Where(x => x.ReceivedCheck))
            {
                order.Receive();
            }
            SelectedOpenOrder = null;

            RefreshOrderList();
        }

        private void ReOpenOrder(object obj)
        {
            SelectedReceivedOrder.ReceivedCheck = false;
            SelectedReceivedOrder.ReOpen();
            SelectedReceivedOrder = null;
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
            ViewOrderVM tabVM = new ViewOrderVM(order, RefreshOrderList);
            tabVM.Add("PO#: " + order.Id);
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
            DeleteOrder(SelectedOpenOrder);
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
            NewOrderVM tabVM = new NewOrderVM(RefreshOrderList);
            tabVM.Add("New Order");
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
                MessageBox.Show("Purchase order is open. Close and delete it if you wish to overwrite");
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
                MessageBox.Show("Receiving list is open. Close and delete it if you wish to overwrite");
            }
        }

        //private void ChangeOrderStats()
        //{
        //    if(SelectedReceivedOrder != null && !OrderStatsDict.ContainsKey(SelectedReceivedOrder.Id))
        //    {
        //        OrderStatsDict[SelectedReceivedOrder.Id] = new List<OrderStat>(SelectedReceivedOrder.GetCategoryCosts()
        //                                                            .Select(x => new OrderStat() { Label = x.Key, Value = x.Value }));
        //    }
        //}

        #endregion

        #region Initializers

        /// <summary>
        /// Populate the 2 dataGrids in the Orders overview
        /// </summary>
        /// <returns></returns>
        public void LoadPreviousOrders(PeriodMarker period, WeekMarker week)
        {
            if (_models != null && _models.PurchaseOrders != null)
            {
                OpenOrders = new ObservableCollection<PurchaseOrder>(_models.PurchaseOrders.Where(x => !x.Received)
                                                                        .OrderBy(x => x.OrderDate));
                ReceivedOrders = new ObservableCollection<PurchaseOrder>(_models.PurchaseOrders.Where(x => x.Received &&
                                                                                                      week.StartDate <= x.ReceivedDate &&
                                                                                                      x.ReceivedDate < week.EndDate)
                                                                        .OrderByDescending(x => x.ReceivedDate));
                TotalOrders.Value = ReceivedOrders.Count;
                SetPeriodWeekTotals(week, period);
            }
        }

        private void SetPeriodWeekTotals(WeekMarker week, PeriodMarker period)
        {
            WeekCostTotal.Value = ReceivedOrders.Sum(x => x.GetTotalCost());
            float lastWeekTotal = GetWeekTotal(week.GetPrevWeek());
            WeekTrend = (WeekCostTotal.Value - lastWeekTotal) / lastWeekTotal;
            PeriodCostTotal.Value = GetPeriodTotal(period);
            float lastPeriodTotal = GetPeriodTotal(period.GetPrevPeriod());
            PeriodTrend = (PeriodCostTotal.Value - lastPeriodTotal) / lastPeriodTotal;
        }

        private void OrdersNotFound()
        {
            OpenOrders = new ObservableCollection<PurchaseOrder>() { new PurchaseOrder() { VendorName = "Orders not found"  } };
            ReceivedOrders = new ObservableCollection<PurchaseOrder>() { new PurchaseOrder() { VendorName = "Orders not found" } };
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
            LoadPreviousOrders(PeriodSelector.SelectedPeriod, PeriodSelector.SelectedWeek);
        }

        public void UpdateRecDate(PurchaseOrder order)
        {
            order.Update();
            //ReceivedOrders = new ObservableCollection<PurchaseOrder>(_models.PurchaseOrders.Where(x => x.Received)
            //                                                            .OrderByDescending(x => x.ReceivedDate));
            LoadPreviousOrders(PeriodSelector.SelectedPeriod, PeriodSelector.SelectedWeek);
        }
        #endregion

        private float GetPeriodTotal(PeriodMarker period)
        {
            if (period == null)
                return PeriodCostTotal != null ? PeriodCostTotal.Value : 0;
            return _models.PurchaseOrders.Where(x => x.Received && period.StartDate <= x.ReceivedDate &&
                                                x.ReceivedDate < period.EndDate).Sum(x => x.GetTotalCost());
        }

        private float GetWeekTotal(WeekMarker week)
        {
            if (week == null)
                return WeekCostTotal != null ? WeekCostTotal.Value : 0;
            return _models.PurchaseOrders.Where(x => x.Received && week.StartDate <= x.ReceivedDate &&
                                                x.ReceivedDate < week.EndDate).Sum(x => x.GetTotalCost());
        }
    }

}
