﻿using BuddhaBowls.Helpers;
using BuddhaBowls.Messengers;
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
        private Dictionary<string, List<InventoryItem>> _recOrdersDict;
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

            Messenger.Instance.Register<Message>(MessageTypes.INVENTORY_CHANGED, (msg) => InventoryChanged());
            Messenger.Instance.Register(MessageTypes.PO_CHANGED, new Action<Message>(POChanged));
            Messenger.Instance.Register<Message>(MessageTypes.BREAD_CHANGED, (msg) => HandleBread((BreadWeekContainer)msg.Payload));
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
            ViewOrderVM tabVM = new ViewOrderVM(order);
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
            CogsCategory totalCogs = new CogsCategory("Total", _startInventory, _endInventory, _recOrdersDict["Total"], 0);
            foreach (string category in _models.GetInventoryCategories().Concat(new List<string> { "Food Total" }))
            {
                yield return new CogsCategory(category, _startInventory, _endInventory, _recOrdersDict[category], totalCogs.CogsCost);
                //if (category == "Bread")
                //    yield return new CogsCategory(category, _startInventory, _endInventory, _breadOrderItems, totalCogs.CogsCost);
                //else
                //    yield return new CogsCategory(category, _startInventory, _endInventory, _recOrders, totalCogs.CogsCost);
            }
            yield return totalCogs;
        }

        private void SetInvAndOrders(PeriodMarker timeFrame)
        {
            SetInv(timeFrame);
            InitRecOrdersDict(timeFrame);
            if (timeFrame.GetType() == typeof(WeekMarker))
                HandleBread(_models.GetBreadWeek((WeekMarker)timeFrame), (WeekMarker)timeFrame);
        }

        private void SetInv(PeriodMarker timeFrame)
        {
            List<Inventory> inventoryList = _models.InContainer.Items.OrderByDescending(x => x.Date).ToList();
            List<Inventory> periodInvList = inventoryList.Where(x => timeFrame.StartDate <= x.Date && x.Date <= timeFrame.EndDate).ToList();

            // end inventory should be the first inventory after the end of the week
            _endInventory = inventoryList.Where(x => x.Date > timeFrame.EndDate).OrderBy(x => x.Date).FirstOrDefault();

            // fallback to the last inventory of the week
            if (_endInventory == null)
                _endInventory = inventoryList.Where(x => x.Date <= timeFrame.EndDate).OrderByDescending(x => x.Date).FirstOrDefault();

            if (periodInvList.Count == 0)
                periodInvList.Add(_endInventory);

            if (_endInventory != null)
            {
                // start inventory is the first inventory in the week
                _startInventory = inventoryList.Where(x => x.Date >= timeFrame.StartDate && x.Date < timeFrame.EndDate)
                                               .OrderBy(x => x.Date).FirstOrDefault();

                // fallback to making start and end inventories the same
                if (_startInventory == null)
                    _startInventory = _endInventory;
            }
        }

        private void InitRecOrdersDict(PeriodMarker timeFrame)
        {
            List<PurchaseOrder> recOrders = _models.POContainer.Items.Where(x => x.ReceivedDate >= timeFrame.StartDate &&
                                                                  x.ReceivedDate <= timeFrame.EndDate.Date.AddDays(1)).ToList();

            _recOrdersDict = _models.GetInventoryCategories().ToDictionary(x => x, x => new List<InventoryItem>());
            foreach (KeyValuePair<string, List<InventoryItem>> itemDict in recOrders.SelectMany(x => x.RecCategoryItemsDict))
            {
                _recOrdersDict[itemDict.Key].AddRange(itemDict.Value);
            }
            _recOrdersDict["Total"] = _recOrdersDict.Values.SelectMany(x => x).ToList();
            _recOrdersDict["Food Total"] = _recOrdersDict.Where(x => Properties.Settings.Default.FoodCategories.Contains(x.Key))
                                                         .SelectMany(x => x.Value).ToList();
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
                foreach (InventoryItem item in order.ItemList)
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

        private void POChanged(Message msg)
        {
            PurchaseOrder order = (PurchaseOrder)msg.Payload;
            if(order.Received && PeriodSelector.SelectedWeek.StartDate <= order.ReceivedDate && order.ReceivedDate <= PeriodSelector.SelectedWeek.EndDate)
            {
                InitRecOrdersDict(PeriodSelector.SelectedWeek);
                foreach (CogsCategory cogs in CategoryList.Where(x => x.Name != "Bread"))
                {
                    cogs.SetPOItems(_recOrdersDict[cogs.Name]);
                }
            }
        }

        private void HandleBread(BreadWeekContainer container, WeekMarker timeframe = null)
        {
            DateTime endDate = container.Week[0].Date.Date;
            timeframe = timeframe ?? PeriodSelector.SelectedWeek;
            if (endDate <= timeframe.EndDate && endDate >= timeframe.StartDate)
            {
                List<InventoryItem> breadItems = container.GetWeekAsInvItems().ToList();

                _recOrdersDict["Bread"] = breadItems;

                int idx = 0;
                foreach (string breadType in container.Week[0].BreadDescDict.Keys)
                {
                    int invIdx = _startInventory.InvItemsContainer.Items.FindIndex(x => x.Name == breadType);
                    _startInventory.InvItemsContainer.Items[invIdx] = breadItems[idx];
                    invIdx = _endInventory.InvItemsContainer.Items.FindIndex(x => x.Name == breadType);
                    _endInventory.InvItemsContainer.Items[invIdx] = breadItems[breadItems.Count - container.Week[0].BreadDescDict.Count + idx];
                    idx++;
                }

                if (CategoryList != null)
                {
                    CogsCategory breadCategory = CategoryList.FirstOrDefault(x => x.Name == "Bread");
                    breadCategory.SetStartInventory(_startInventory, true);
                    breadCategory.SetEndInventory(_endInventory, true);
                    breadCategory.SetPOItems(_recOrdersDict["Bread"]);
                }
            }
        }

        private void InventoryChanged()
        {
            Inventory prevStartInv = _startInventory;
            Inventory prevEndInv = _endInventory;

            SetInv(PeriodSelector.SelectedWeek);
            
            foreach (CogsCategory cogs in CategoryList.Where(x => x.Name != "Bread"))
            {
                if (prevStartInv != _startInventory)
                    cogs.SetStartInventory(_startInventory, silent: true);
                if (prevEndInv != _endInventory)
                    cogs.SetEndInventory(_endInventory, silent: true);
                cogs.UpdateProperties();
            }
        }

    }

    public class CogsCategory : ObservableObject
    {
        private Inventory _startInv;
        private Inventory _endInv;
        private List<InventoryItem> _purchases;
        private List<InventoryItem> _breadOrders;

        public string Name { get; set; }

        public float StartInv
        {
            get
            {
                List<InventoryItem> startInvItems = GetInvItems(_startInv);
                return startInvItems.Sum(x => x.PriceExtension);
            }
        }

        public float EndInv
        {
            get
            {
                List<InventoryItem> endInvItems = GetInvItems(_endInv);
                return endInvItems.Sum(x => x.PriceExtension);
            }
        }

        public float Purchases
        {
            get
            {
                return _purchases.Sum(x => x.PurchaseExtension);
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
                return MainHelper.SortItems(GetInvItems(_startInv)).Select(x => new CatItem(x.Name, x,
                                                                                            endInvItems.FirstOrDefault(y => y.Name == x.Name),
                                                                           _purchases.Where(y => y.Name == x.Name))).ToList();
            }
        }

        public CogsCategory(string category, Inventory startInv, Inventory endInv, List<InventoryItem> purchased, float totalCogs)
        {
            Name = category;
            _startInv = startInv;
            _endInv = endInv;
            _purchases = purchased;
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

        public void SetStartInventory(Inventory inv, bool silent = false)
        {
            _startInv = inv;
            if(!silent)
                UpdateProperties();
        }

        public void SetEndInventory(Inventory inv, bool silent = false)
        {
            _endInv = inv;
            if(!silent)
                UpdateProperties();
        }

        public void SetPOItems(List<InventoryItem> items, bool silent = false)
        {
            _purchases = items;
            if(!silent)
                UpdateProperties();
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

    }

    public class CatItem : ObservableObject
    {
        private string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                NotifyPropertyChanged("Name");
            }
        }

        private float _startCount;
        public float StartCount
        {
            get
            {
                return _startCount;
            }
            set
            {
                _startCount = value;
                NotifyPropertyChanged("StartCount");
                NotifyPropertyChanged("Usage");
            }
        }

        private float _startValue;
        public float StartValue
        {
            get
            {
                return _startValue;
            }
            set
            {
                _startValue = value;
                NotifyPropertyChanged("StartValue");
            }
        }

        private float _recCount;
        public float RecCount
        {
            get
            {
                return _recCount;
            }
            set
            {
                _recCount = value;
                NotifyPropertyChanged("RecCount");
                NotifyPropertyChanged("Usage");
            }
        }

        private float _recValue;
        public float RecValue
        {
            get
            {
                return _recValue;
            }
            set
            {
                _recValue = value;
                NotifyPropertyChanged("RecValue");
            }
        }

        private float _endCount;
        public float EndCount
        {
            get
            {
                return _endCount;
            }
            set
            {
                _endCount = value;
                NotifyPropertyChanged("EndCount");
                NotifyPropertyChanged("Usage");
            }
        }

        private float _endValue;
        public float EndValue
        {
            get
            {
                return _endValue;
            }
            set
            {
                _endValue = value;
                NotifyPropertyChanged("EndValue");
            }
        }

        public float Usage
        {
            get
            {
                return StartCount - EndCount + RecCount;
            }
        }
        public float UsageValue { get; set; }

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

            UsageValue = Usage * endItem.CountPrice;
        }
    }
}
