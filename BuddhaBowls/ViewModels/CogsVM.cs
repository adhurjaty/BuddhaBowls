using BuddhaBowls.Helpers;
using BuddhaBowls.Models;
using BuddhaBowls.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace BuddhaBowls
{
    public class CogsVM : TabVM
    {
        private const int GRID_COLLAPSED_HEIGHT = 30;
        private string[] _subHeaders = new string[] { "Inventories", "Received Purchases" };

        private Inventory _startInventory;
        private Inventory _endInventory;
        private List<PurchaseOrder> _recOrders;
        private List<InventoryItem> _breadOrderItems;
        private ReportsTabVM _reportsTab;

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

        public PeriodSelectorVM PeriodSelector
        {
            get
            {
                return _reportsTab.PeriodSelector;
            }
            set
            {
                _reportsTab.PeriodSelector = value;
                NotifyPropertyChanged("PeriodSelector");
            }
        }
        #endregion

        #region ICommand and CanExecute

        public ICommand StartInvCommand { get; set; }
        public ICommand EndInvCommand { get; set; }

        #endregion

        public CogsVM(ReportsTabVM tabContext) : base()
        {
            _reportsTab = tabContext;

            StartInvCommand = new RelayCommand(OpenStartInv);
            EndInvCommand = new RelayCommand(OpenEndInv);
        }

        #region ICommand Helpers

        public void CalculateCogs(WeekMarker week)
        {
            CategoryList = new ObservableCollection<CogsCategory>(GetCogs(week));
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
            NewInventoryVM tabVM = new NewInventoryVM(_startInventory);
            tabVM.Add("View Inventory");
        }

        public void OpenEndInv(object obj)
        {
            NewInventoryVM tabVM = new NewInventoryVM(_endInventory);
            tabVM.Add("View Inventory");
        }

        private void SetCogsDetails()
        {
            if (SelectedCogs == null)
            {
                CogInfoVisibility = Visibility.Hidden;
            }
            else
            {
                CogInfoVisibility = Visibility.Visible;
                CatItems = new ObservableCollection<CatItem>(SelectedCogs.CatItems);
            }
        }

        public void CogsUpdated()
        {
            CalculateCogs(PeriodSelector.SelectedWeek);
        }

        public void BreadUpdated()
        {
            CogsCategory breadCat = CategoryList.First(x => x.Name == "Bread");
            breadCat.SetBreadOrders(_models.GetBreadPeriodOrders(PeriodSelector.SelectedWeek).ToList());
            breadCat.UpdateProperties();

            if(SelectedCogs != null && SelectedCogs.Name == "Bread")
            {
                CatItems = new ObservableCollection<CatItem>(SelectedCogs.CatItems);
            }
        }
        #endregion

        public IEnumerable<CogsCategory> GetCogs(PeriodMarker timeFrame)
        {
            SetInvAndOrders(timeFrame);
            CogsCategory totalCogs = new CogsCategory("Total", ref _startInventory, ref _endInventory, ref _recOrders, 0);
            foreach (string category in _models.GetInventoryCategories().Concat(new List<string> { "Food Total" }))
            {
                if (category == "Bread")
                    yield return new CogsCategory(category, ref _startInventory, ref _endInventory, ref _breadOrderItems, totalCogs.CogsCost);
                else
                    yield return new CogsCategory(category, ref _startInventory, ref _endInventory, ref _recOrders, totalCogs.CogsCost);
            }
            yield return totalCogs;
        }

        private void SetInvAndOrders(PeriodMarker timeFrame)
        {
            List<Inventory> inventoryList = _models.Inventories.OrderByDescending(x => x.Date).ToList();
            List<Inventory> periodInvList = inventoryList.Where(x => timeFrame.StartDate <= x.Date && x.Date <= timeFrame.EndDate).ToList();
            _endInventory = inventoryList.Where(x => x.Date > timeFrame.EndDate).OrderBy(x => x.Date).FirstOrDefault();
            if(_endInventory == null)
                _endInventory = inventoryList.Where(x => x.Date <= timeFrame.EndDate).OrderByDescending(x => x.Date).FirstOrDefault();

                if (periodInvList.Count == 0)
                periodInvList.Add(_endInventory);

            if (_endInventory != null)
            {
                _startInventory = inventoryList.Where(x => x.Date >= timeFrame.StartDate && x.Date < timeFrame.EndDate)
                                               .OrderBy(x => x.Date).FirstOrDefault();
                if (_startInventory == null)
                    _startInventory = inventoryList.Last();

                _recOrders = _models.PurchaseOrders.Where(x => x.ReceivedDate >= timeFrame.StartDate &&
                                                               x.ReceivedDate <= timeFrame.EndDate.Date.AddDays(1)).ToList();
            }
            _breadOrderItems = _models.GetBreadPeriodOrders(timeFrame).ToList();
        }

        private List<InventoryItem> GetFoodInv(List<InventoryItem> invList)
        {
            return invList.Where(x => Properties.Settings.Default.FoodCategories.Contains(x.Category)).ToList();
        }

        private List<InventoryItem> GetItemsFromPurchases(List<PurchaseOrder> orders)
        {
            Dictionary<int, InventoryItem> invDict = new Dictionary<int, InventoryItem>();

            foreach (PurchaseOrder order in orders)
            {
                foreach (InventoryItem item in order.GetPOItems())
                {
                    if (invDict.ContainsKey(item.Id))
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

    public class CogsCategory : INotifyPropertyChanged
    {
        private Inventory _startInv;
        private Inventory _endInv;
        private List<PurchaseOrder> _purchases;
        private List<InventoryItem> _breadOrders;

        // INotifyPropertyChanged event and method
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Name { get; set; }

        public float StartInv
        {
            get
            {
                List<InventoryItem> startInvItems = GetInvItems(_startInv);
                if (startInvItems == null)
                    return 0;
                return startInvItems.Sum(x => x.PriceExtension);
            }
        }
        public float EndInv
        {
            get
            {
                List<InventoryItem> endInvItems = GetInvItems(_endInv);
                if (endInvItems == null)
                    return 0;
                return endInvItems.Sum(x => x.PriceExtension);
            }
        }
        public float Purchases
        {
            get
            {
                List<InventoryItem> purchasedItems = GetInvItems(_purchases);
                if (purchasedItems == null)
                    return 0;
                return purchasedItems.Sum(x => x.PurchaseExtension);
            }
        }
        public float CatPercent
        {
            get
            {
                if (TotalCogs == 0)
                    return 1;
                return CogsCost / TotalCogs;
            }
        }
        public float CogsCost
        {
            get
            {
                return StartInv + Purchases - EndInv;
            }
        }
        public float TotalCogs { get; set; }

        public List<CatItem> CatItems
        {
            get
            {
                List<InventoryItem> endInvItems = GetInvItems(_endInv);
                List<InventoryItem> purchasedItems = GetInvItems(_purchases);
                return GetInvItems(_startInv).Select(x => new CatItem(x.Name, x, endInvItems.FirstOrDefault(y => y.Name == x.Name),
                                                                      purchasedItems.Where(y => y.Name == x.Name))).ToList();
            }
        }

        public CogsCategory(string category, ref Inventory startInv, ref Inventory endInv, ref List<PurchaseOrder> purchased, float totalCogs)
        {
            Name = category;
            _startInv = startInv;
            _endInv = endInv;
            _purchases = purchased;
            TotalCogs = totalCogs;
        }

        /// <summary>
        /// Used only for bread COGS
        /// </summary>
        /// <param name="category"></param>
        /// <param name="startInv"></param>
        /// <param name="endInv"></param>
        /// <param name="breadOrders"></param>
        /// <param name="totalCogs"></param>
        public CogsCategory(string category, ref Inventory startInv, ref Inventory endInv, ref List<InventoryItem> breadOrders, float totalCogs)
        {
            Name = category;
            _startInv = startInv;
            _endInv = endInv;
            _breadOrders = breadOrders;
            TotalCogs = totalCogs;
        }

        public void UpdateProperties()
        {
            NotifyPropertyChanged("StartInv");
            NotifyPropertyChanged("EndInv");
            NotifyPropertyChanged("Purchases");
            NotifyPropertyChanged("CatPercent");
            NotifyPropertyChanged("CogsCost");
            NotifyPropertyChanged("CatItems");
        }

        public void SetBreadOrders(List<InventoryItem> purchases)
        {
            _breadOrders = purchases;
        }

        private List<InventoryItem> GetInvItems(Inventory inv)
        {
            Dictionary<string, List<InventoryItem>> catDict = inv.CategoryItemsDict;
            if (Name == "Total")
                return catDict.SelectMany(x => x.Value).ToList();
            if (Name == "Food Total")
                return catDict.Where(x => Properties.Settings.Default.FoodCategories.Contains(x.Key)).SelectMany(x => x.Value).ToList();
            if (!catDict.ContainsKey(Name))
                return new List<InventoryItem>();
            return catDict[Name];
        }

        private List<InventoryItem> GetInvItems(List<PurchaseOrder> orders)
        {
            if (Name == "Total")
                return _purchases.SelectMany(x => x.RecCategoryItemsDict.SelectMany(y => y.Value)).ToList();
            if (Name == "Food Total")
                return _purchases.SelectMany(x => x.RecCategoryItemsDict.Where(y => Properties.Settings.Default.FoodCategories.Contains(y.Key))
                                                                        .SelectMany(y => y.Value)).ToList();
            if (Name == "Bread")
                return _breadOrders;
            return _purchases.Where(x => x.RecCategoryItemsDict.ContainsKey(Name)).SelectMany(x => x.RecCategoryItemsDict[Name]).ToList();
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

        public CatItem(string name, InventoryItem startItem, InventoryItem endItem, IEnumerable<InventoryItem> purchasedItem)
        {
            Name = name;
            StartCount = startItem.Count;
            StartValue = startItem.PriceExtension;

            if (endItem != null)
            {
                EndCount = endItem.Count;
                EndValue = endItem.PriceExtension;
            }
            if (purchasedItem != null && purchasedItem.Count() > 0)
            {
                RecCount = purchasedItem.Sum(x => x.LastOrderAmount) * purchasedItem.First().Conversion;
                RecValue = purchasedItem.Sum(x => x.PurchaseExtension);
            }
        }
    }
}
