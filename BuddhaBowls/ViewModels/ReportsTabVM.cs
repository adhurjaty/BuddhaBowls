using BuddhaBowls.Helpers;
using BuddhaBowls.Models;
using BuddhaBowls.Services;
using BuddhaBowls.UserControls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace BuddhaBowls
{
    public class ReportsTabVM : ChangeableTabVM
    {
        private const int GRID_COLLAPSED_HEIGHT = 30;

        private string[] _subHeaders = new string[] { "Inventories", "Received Purchases" };
        private Inventory _startInventory;
        private List<InventoryItem> _startInvList;
        private Inventory _endInventory;
        private List<InventoryItem> _endingInvList;
        Dictionary<string, Tuple<List<PurchaseOrder>, List<InventoryItem>>> _purchaseItemsCatDict;

        #region Content Binders

        private ObservableCollection<CogsCategory> _categoryList;
        public ObservableCollection<CogsCategory> CategoryList
        {
            get
            {
                return _categoryList;
            }
            set
            {
                _categoryList = value;
                NotifyPropertyChanged("CategoryList");
            }
        }

        private CogsCategory _selectedCogs;
        public CogsCategory SelectedCogs
        {
            get
            {
                return _selectedCogs;
            }
            set
            {
                _selectedCogs = value;
                NotifyPropertyChanged("SelectedCogs");

                SetCogsDetails();
            }
        }

        private Visibility _cogInfoVisibility = Visibility.Hidden;
        public Visibility CogInfoVisibility
        {
            get
            {
                return _cogInfoVisibility;
            }
            set
            {
                _cogInfoVisibility = value;
                NotifyPropertyChanged("CogInfoVisibility");
            }
        }

        private ObservableCollection<InventoryItem> _startInv;
        public ObservableCollection<InventoryItem> StartInv
        {
            get
            {
                return _startInv;
            }
            set
            {
                _startInv = value;
                NotifyPropertyChanged("StartInv");
            }
        }

        private ObservableCollection<InventoryItem> _endingInv;
        public ObservableCollection<InventoryItem> EndingInv
        {
            get
            {
                return _endingInv;
            }
            set
            {
                _endingInv = value;
                NotifyPropertyChanged("EndingInv");
            }
        }

        private ObservableCollection<PurchaseOrder> _recOrders;
        public ObservableCollection<PurchaseOrder> RecOrders
        {
            get
            {
                return _recOrders;
            }
            set
            {
                _recOrders = value;
                NotifyPropertyChanged("RecOrders");
            }
        }

        private PurchaseOrder _selectedOrder;
        public PurchaseOrder SelectedOrder
        {
            get
            {
                return _selectedOrder;
            }
            set
            {
                _selectedOrder = value;
                NotifyPropertyChanged("SelectedOrder");

                SetReceivedDetails();
            }
        }

        private float _selectedTotalCost;
        public float SelectedTotalCost
        {
            get
            {
                return _selectedTotalCost;
            }
            set
            {
                _selectedTotalCost = value;
                NotifyPropertyChanged("SelectedTotalCost");
            }
        }

        private float _selectedTotalFoodCost;
        public float SelectedTotalFoodCost
        {
            get
            {
                return _selectedTotalFoodCost;
            }
            set
            {
                _selectedTotalFoodCost = value;
                NotifyPropertyChanged("SelectedTotalFoodCost");
            }
        }

        private float _selectedTotalCategoryCost;
        public float SelectedTotalCategoryCost
        {
            get
            {
                return _selectedTotalCategoryCost;
            }
            set
            {
                _selectedTotalCategoryCost = value;
                NotifyPropertyChanged("SelectedTotalCategoryCost");
            }
        }

        private DateTime _selectedOrderedDate;
        public DateTime SelectedOrderedDate
        {
            get
            {
                return _selectedOrderedDate;
            }
            set
            {
                _selectedOrderedDate = value;
                NotifyPropertyChanged("SelectedOrderedDate");
            }
        }

        private Visibility _orderDetailsVisibility = Visibility.Hidden;
        public Visibility OrderDetailsVisibility
        {
            get
            {
                return _orderDetailsVisibility;
            }
            set
            {
                _orderDetailsVisibility = value;
                NotifyPropertyChanged("OrderDetailsVisibility");
            }
        }
        #endregion

        #region ICommand and CanExecute

        public ICommand StartInvCommand { get; set; }
        public ICommand EndInvCommand { get; set; }

        #endregion

        public ReportsTabVM() : base()
        {
            Header = "Cost of Goods Sold";
            InitSwitchButtons(new string[] { "COGS", "P & L" });

            if (DBConnection)
            {
                PeriodSelector = new PeriodSelectorVM(_models, SwitchedPeriod, hasShowAll: false);

                StartInvCommand = new RelayCommand(OpenStartInv);
                EndInvCommand = new RelayCommand(OpenEndInv);
            }
        }

        #region ICommand Helpers

        private void SwitchedPeriod(PeriodMarker period, WeekMarker week)
        {
            CalculateCogs(week);
        }

        private void CalculateCogs(WeekMarker week)
        {
            List<Inventory> inventoryList = _models.Inventories.OrderByDescending(x => x.Date).ToList();
            List<Inventory> periodInvList = inventoryList.Where(x => week.StartDate <= x.Date && x.Date <= week.EndDate).ToList();
            _endInventory = inventoryList.FirstOrDefault(x => x.Date <= week.EndDate);

            if (periodInvList.Count == 0)
                periodInvList.Add(_endInventory);

            CategoryList = new ObservableCollection<CogsCategory>();
            if(_endInventory != null)
            {
                _startInventory = inventoryList.FirstOrDefault(x => x.Date <= week.StartDate);
                if (_startInventory == null)
                    _startInventory = inventoryList.Last();

                _startInvList = _startInventory.GetInventoryHistory();
                _endingInvList = _endInventory.GetInventoryHistory();
                if (_startInvList != null && _endingInvList != null)
                {
                    IEnumerable<IGrouping<string, InventoryItem>> startItems = MainHelper.CategoryGrouping(_startInvList);
                    IEnumerable<IGrouping<string, InventoryItem>> endItems = MainHelper.CategoryGrouping(_endingInvList);

                    List<PurchaseOrder> recOrdersList = _models.PurchaseOrders.Where(x => x.ReceivedDate >= week.StartDate && 
                                                                    x.ReceivedDate <= week.EndDate.Date.AddDays(1)).ToList();
                    _purchaseItemsCatDict = GetPurchasedByCategory(recOrdersList);
                    foreach (string category in _models.GetInventoryCategories())
                    {
                        IGrouping<string, InventoryItem> startGroup = startItems.FirstOrDefault(x => x.Key == category);
                        IGrouping<string, InventoryItem> endGroup = endItems.FirstOrDefault(x => x.Key == category);

                        if (startGroup != null && endGroup != null)
                        {
                            List<InventoryItem> orderList = new List<InventoryItem>();
                            if(_purchaseItemsCatDict.ContainsKey(category))
                            {
                                orderList = _purchaseItemsCatDict[category].Item2;
                            }
                            CategoryList.Add(new CogsCategory(category, startGroup.ToList(), endGroup.ToList(), orderList));
                        }
                    }

                    CategoryList.Add(new CogsCategory("Food Total", GetFoodInv(_startInvList), GetFoodInv(_endingInvList), _purchaseItemsCatDict["Food Total"].Item2));
                    CategoryList.Add(new CogsCategory("Total", _startInvList, _endingInvList, _purchaseItemsCatDict["Total"].Item2));
                }
            }
        }

        #endregion

        #region Initializers

        #endregion

        #region UI Updaters

        public void RecOrderDouleClicked(PurchaseOrder order)
        {
            ViewOrderVM tabVM = new ViewOrderVM(order, ParentContext.OrderTab.RefreshOrderList);
            tabVM.Add("PO#: " + order.Id);
        }

        public void OpenStartInv(object obj)
        {
            NewInventoryVM tabVM = new NewInventoryVM(ParentContext.InventoryTab.Refresh, _startInventory);
            tabVM.Add("View Inventory");
        }

        public void OpenEndInv(object obj)
        {
            NewInventoryVM tabVM = new NewInventoryVM(ParentContext.InventoryTab.Refresh, _endInventory);
            tabVM.Add("View Inventory");
        }

        public void SetReceivedDetails()
        {
            if(SelectedOrder == null)
            {
                OrderDetailsVisibility = Visibility.Hidden;
            }
            else
            {
                OrderDetailsVisibility = Visibility.Visible;
                SelectedTotalCost = SelectedOrder.GetTotalCost();
                Dictionary<string, float> catCosts = SelectedOrder.GetCategoryCosts();
                SelectedTotalFoodCost = MainHelper.GetFoodCost(catCosts);
                SelectedTotalCategoryCost = catCosts[SelectedCogs.Name];
                SelectedOrderedDate = SelectedOrder.OrderDate;
            }
        }

        private void SetCogsDetails()
        {
            if(SelectedCogs == null)
            {
                CogInfoVisibility = Visibility.Hidden;
            }
            else
            {
                CogInfoVisibility = Visibility.Visible;
                if (SelectedCogs.Name == "Total")
                {
                    StartInv = new ObservableCollection<InventoryItem>(_startInvList);
                    EndingInv = new ObservableCollection<InventoryItem>(_endingInvList);
                }
                else if(SelectedCogs.Name == "Food Total")
                {
                    StartInv = new ObservableCollection<InventoryItem>(_startInvList.Where(x => Properties.Settings.Default.FoodCategories.Contains(x.Category)));
                    EndingInv = new ObservableCollection<InventoryItem>(_endingInvList.Where(x => Properties.Settings.Default.FoodCategories.Contains(x.Category)));
                }
                else
                { 
                    StartInv = new ObservableCollection<InventoryItem>(_startInvList.Where(x => x.Category == SelectedCogs.Name));
                    EndingInv = new ObservableCollection<InventoryItem>(_endingInvList.Where(x => x.Category == SelectedCogs.Name));
                }
                if (_purchaseItemsCatDict.ContainsKey(SelectedCogs.Name))
                    RecOrders = new ObservableCollection<PurchaseOrder>(_purchaseItemsCatDict[SelectedCogs.Name].Item1);
                else
                    RecOrders = new ObservableCollection<PurchaseOrder>();
            }
        }

        #endregion

        private Dictionary<string, Tuple<List<PurchaseOrder>, List<InventoryItem>>> GetPurchasedByCategory(List<PurchaseOrder> orders)
        {
            Dictionary<string, Tuple<List<PurchaseOrder>, List<InventoryItem>>> purchaseDict = new Dictionary<string, Tuple<List<PurchaseOrder>, List<InventoryItem>>>();

            purchaseDict["Total"] = new Tuple<List<PurchaseOrder>, List<InventoryItem>>(orders, new List<InventoryItem>());
            purchaseDict["Food Total"] = new Tuple<List<PurchaseOrder>, List<InventoryItem>>(new List<PurchaseOrder>(), new List<InventoryItem>());
            foreach (PurchaseOrder order in orders)
            {
                foreach (InventoryItem item in order.GetReceivedPOItems())
                {
                    if (!purchaseDict.ContainsKey(item.Category))
                        purchaseDict[item.Category] = new Tuple<List<PurchaseOrder>, List<InventoryItem>>(new List<PurchaseOrder>() { order }, new List<InventoryItem>());
                    if (!purchaseDict[item.Category].Item1.Contains(order))
                        purchaseDict[item.Category].Item1.Add(order);

                    if(Properties.Settings.Default.FoodCategories.Contains(item.Category))
                    {
                        if (!purchaseDict["Food Total"].Item1.Contains(order))
                            purchaseDict["Food Total"].Item1.Add(order);
                        purchaseDict["Food Total"].Item2.Add(item);
                    }

                    purchaseDict["Total"].Item2.Add(item);
                    purchaseDict[item.Category].Item2.Add(item);
                }
            }

            return purchaseDict;
        }

        private List<InventoryItem> GetFoodInv(List<InventoryItem> invList)
        {
            return invList.Where(x => Properties.Settings.Default.FoodCategories.Contains(x.Category)).ToList();
        }

        protected override void ChangePageState(int pageIdx)
        {
            base.ChangePageState(pageIdx);

            switch (pageIdx)
            {
                case 0:
                    TabControl = _tabCache[0] ?? new CogsControl(this);
                    break;
                case 1:
                    //if (PrepItemList == null)
                    //    PrepItemList = new ObservableCollection<PrepItem>(_models.PrepItems.OrderBy(x => x.Name));
                    //TabControl = _tabCache[1] ?? new PrepListControl(this);
                    break;
                case -1:
                    //DisplayItemsNotFound();
                    break;
            }
        }
    }

    public class CogsCategory
    {
        public string Name { get; set; }
        public float StartInv { get; set; }
        public float EndInv { get; set; }
        public float Purchases { get; set; }
        public float CogsCost
        {
            get
            {
                return StartInv + Purchases - EndInv;
            }
        }

        public CogsCategory(string category, List<InventoryItem> startInv, List<InventoryItem> endInv, List<InventoryItem> purchased)
        {
            Name = category;
            StartInv = startInv.Sum(x => x.PriceExtension);
            EndInv = endInv.Sum(x => x.PriceExtension);
            Purchases = purchased.Sum(x => x.PurchaseExtension);
        }
    }
}
