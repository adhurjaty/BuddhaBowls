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
        List<CatPO> _catPoList;

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

        private ObservableCollection<CatPO> _recOrders;
        public ObservableCollection<CatPO> RecOrders
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

        //private PurchaseOrder _selectedOrder;
        //public PurchaseOrder SelectedOrder
        //{
        //    get
        //    {
        //        return _selectedOrder;
        //    }
        //    set
        //    {
        //        _selectedOrder = value;
        //        NotifyPropertyChanged("SelectedOrder");

        //        SetReceivedDetails();
        //    }
        //}

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

        public void CalculateCogs(WeekMarker week)
        {
            List<Inventory> inventoryList = _models.Inventories.OrderByDescending(x => x.Date).ToList();
            List<Inventory> periodInvList = inventoryList.Where(x => week.StartDate <= x.Date && x.Date <= week.EndDate).ToList();
            _endInventory = inventoryList.FirstOrDefault(x => x.Date <= week.EndDate);
            _catPoList = new List<CatPO>();

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
                    //_purchaseItemsCatDict = GetPurchasedByCategory(recOrdersList);
                    foreach (string category in _models.GetInventoryCategories().Concat(new List<string>() { "Food Total", "Total" }))
                    {
                        IGrouping<string, InventoryItem> startGroup = startItems.FirstOrDefault(x => x.Key == category);
                        IGrouping<string, InventoryItem> endGroup = endItems.FirstOrDefault(x => x.Key == category);

                        foreach (PurchaseOrder po in recOrdersList)
                        {
                            CatPO c = new CatPO(po, category);
                            if (c.InvItems.Count > 0)
                                _catPoList.Add(c);
                        }

                        if (startGroup != null && endGroup != null)
                        {
                            if (category == "Bread")
                            {
                                CategoryList.Add(new CogsCategory(category, startGroup.ToList(), endGroup.ToList(), _models.GetBreadWeekOrders(week).ToList()));
                            }
                            else
                            {
                                CategoryList.Add(new CogsCategory(category, startGroup.ToList(), endGroup.ToList(),
                                                    _catPoList.Where(x => x.Category == category).SelectMany(x => x.InvItems).ToList()));
                            }
                        }

                        if(category == "Food Total")
                            CategoryList.Add(new CogsCategory(category, GetFoodInv(_startInvList), GetFoodInv(_endingInvList),
                                                              _catPoList.Where(x => x.Category == category).SelectMany(x => x.InvItems).ToList()));
                        if (category == "Total")
                            CategoryList.Add(new CogsCategory(category, _startInvList, _endingInvList,
                                                              _catPoList.Where(x => x.Category == category).SelectMany(x => x.InvItems).ToList()));
                    }
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

                RecOrders = new ObservableCollection<CatPO>(_catPoList.Where(x => x.Category == SelectedCogs.Name));
            }
        }

        #endregion

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
        public float Useage { get; set; }
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
            Useage = startInv.Sum(x => x.Count) + purchased.Sum(x => x.LastOrderAmount * x.Conversion) - endInv.Sum(x => x.Count);
        }
    }

    public class CatPO
    {
        private PurchaseOrder _po;

        public int Id
        {
            get
            {
                return _po.Id;
            }
        }

        public DateTime? ReceivedDate
        {
            get
            {
                return _po.ReceivedDate;
            }
        }

        public float TotalCost
        {
            get
            {
                return _po.TotalCost;
            }
        }

        public List<InventoryItem> InvItems
        {
            get
            {
                return GetCategoryItems().ToList();
            }
        }

        public string Category { get; set; }

        public CatPO(PurchaseOrder po, string categoryName)
        {
            _po = po;
            Category = categoryName;
        }

        private IEnumerable<InventoryItem> GetCategoryItems()
        {
            foreach (InventoryItem item in _po.GetReceivedPOItems())
            {
                if (Category == item.Category || Category == "Total" ||
                    (Category == "Food Total" && Properties.Settings.Default.FoodCategories.Contains(item.Category)))
                {
                    yield return item;
                }
            }
        }

        public PurchaseOrder GetPO()
        {
            return _po;
        }
    }
}
