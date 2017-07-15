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

        private ObservableCollection<CatItem> _catItems;
        public ObservableCollection<CatItem> CatItems
        {
            get
            {
                return _catItems;
            }
            set
            {
                _catItems = value;
                NotifyPropertyChanged("CatItems");
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

        public void CalculateCogs(WeekMarker week)
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
                    List<InventoryItem> recPurchasedItems = GetItemsFromPurchases(recOrdersList);

                    CogsCategory totalCogs = new CogsCategory("Total", _startInvList, _endingInvList, recPurchasedItems);
                    foreach (string category in _models.GetInventoryCategories().Concat(new List<string> { "Food Total" }))
                    {
                        IGrouping<string, InventoryItem> startGroup = startItems.FirstOrDefault(x => x.Key == category);
                        IGrouping<string, InventoryItem> endGroup = endItems.FirstOrDefault(x => x.Key == category);

                        if (startGroup != null && endGroup != null)
                        {
                            if (category == "Bread")
                            {
                                CategoryList.Add(new CogsCategory(category, startGroup.ToList(), endGroup.ToList(),
                                                    _models.GetBreadWeekOrders(week).ToList(), totalCogs.CogsCost));
                            }
                            else
                            {
                                CategoryList.Add(new CogsCategory(category, startGroup.ToList(), endGroup.ToList(),
                                                                  recPurchasedItems.Where(x => x.Category == category).ToList(),
                                                                  totalCogs.CogsCost));
                            }
                        }

                        if(category == "Food Total")
                            CategoryList.Add(new CogsCategory(category, GetFoodInv(_startInvList), GetFoodInv(_endingInvList),
                                                              recPurchasedItems.Where(x => Properties.Settings.Default.FoodCategories.Contains(x.Category)).ToList(),
                                                              totalCogs.CogsCost));
                    }
                    CategoryList.Add(totalCogs);
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
                CatItems = new ObservableCollection<CatItem>(SelectedCogs.CatItems);
                //if (SelectedCogs.Name == "Total")
                //{
                //    StartInv = new ObservableCollection<InventoryItem>(_startInvList);
                //    EndingInv = new ObservableCollection<InventoryItem>(_endingInvList);
                //}
                //else if(SelectedCogs.Name == "Food Total")
                //{
                //    StartInv = new ObservableCollection<InventoryItem>(_startInvList.Where(x => Properties.Settings.Default.FoodCategories.Contains(x.Category)));
                //    EndingInv = new ObservableCollection<InventoryItem>(_endingInvList.Where(x => Properties.Settings.Default.FoodCategories.Contains(x.Category)));
                //}
                //else
                //{ 
                //    StartInv = new ObservableCollection<InventoryItem>(_startInvList.Where(x => x.Category == SelectedCogs.Name));
                //    EndingInv = new ObservableCollection<InventoryItem>(_endingInvList.Where(x => x.Category == SelectedCogs.Name));
                //}

                //RecOrders = new ObservableCollection<CatPO>(_catPoList.Where(x => x.Category == SelectedCogs.Name));
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

        private List<InventoryItem> GetItemsFromPurchases(List<PurchaseOrder> orders)
        {
            Dictionary<int, InventoryItem> invDict = new Dictionary<int, InventoryItem>();

            foreach (PurchaseOrder order in orders)
            {
                foreach (InventoryItem item in order.GetReceivedPOItems())
                {
                    if(invDict.ContainsKey(item.Id))
                    {
                        invDict[item.Id].LastOrderAmount += item.LastOrderAmount;
                    }
                    else
                    {
                        invDict[item.Id] = item;
                    }
                }
            }

            return invDict.Values.ToList();
        }
    }

    public class CogsCategory
    {
        public string Name { get; set; }
        public float StartInv { get; set; }
        public float EndInv { get; set; }
        public float Purchases { get; set; }
        public float CatPercent { get; set; } = 1;
        public float CogsCost
        {
            get
            {
                return StartInv + Purchases - EndInv;
            }
        }
        public List<CatItem> CatItems { get; set; }

        public CogsCategory(string category, List<InventoryItem> startInv, List<InventoryItem> endInv, List<InventoryItem> purchased)
        {
            Name = category;
            StartInv = startInv.Sum(x => x.PriceExtension);
            EndInv = endInv.Sum(x => x.PriceExtension);
            Purchases = purchased.Sum(x => x.PurchaseExtension);
            CatItems = startInv.Select(x => new CatItem(x.Name, x, endInv.FirstOrDefault(y => y.Name == x.Name),
                                                        purchased.FirstOrDefault(y => y.Name == x.Name))).ToList();
            //Useage = startInv.Sum(x => x.Count) + purchased.Sum(x => x.LastOrderAmount * x.Conversion) - endInv.Sum(x => x.Count);
        }

        public CogsCategory(string category, List<InventoryItem> startInv, List<InventoryItem> endInv, List<InventoryItem> purchased,
                            float totalCogs) : this(category, startInv, endInv, purchased)
        {
            if (totalCogs == 0)
                CatPercent = 0;
            else
                CatPercent = CogsCost / totalCogs;
        }
    }

    public class CatItem
    {
        public string Name { get; set; }
        public float StartCount { get; set; }
        public float StartValue { get; set; }
        public float RecCount { get; set; }
        public float RecValue { get; set; }
        public float EndCount { get; set; }
        public float EndValue { get; set; }

        public CatItem(string name, InventoryItem startItem, InventoryItem endItem, InventoryItem purchasedItem)
        {
            Name = name;
            StartCount = startItem.Count;
            StartValue = startItem.PriceExtension;

            if (endItem != null)
            {
                EndCount = endItem.Count;
                EndValue = endItem.PriceExtension;
            }
            if (purchasedItem != null)
            {
                RecCount = purchasedItem.LastOrderAmount * purchasedItem.Conversion;
                RecValue = purchasedItem.PurchaseExtension;
            }
        }
    }
}
