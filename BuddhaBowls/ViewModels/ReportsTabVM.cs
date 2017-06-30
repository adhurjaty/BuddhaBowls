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

        private Visibility _cogInfoVisibility;
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
        #endregion

        #region ICommand and CanExecute

        #endregion

        public ReportsTabVM() : base()
        {
            Header = "Cost of Goods Sold";
            InitSwitchButtons(new string[] { "COGS", "P & L" });

            if (DBConnection)
            {
                PeriodSelector = new PeriodSelectorVM(_models, SwitchedPeriod, hasShowAll: false);
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
            Inventory endInv = inventoryList.FirstOrDefault(x => x.Date <= week.EndDate);

            if (periodInvList.Count == 0)
                periodInvList.Add(endInv);

            CategoryList = new ObservableCollection<CogsCategory>();
            if(endInv != null)
            {
                Inventory startInv = inventoryList.FirstOrDefault(x => x.Date <= week.StartDate);
                if (startInv == null)
                    startInv = inventoryList.Last();

                List<InventoryItem> startList = startInv.GetInventoryHistory();
                List<InventoryItem> endList = endInv.GetInventoryHistory();
                if (startList != null && endList != null)
                {
                    IEnumerable<IGrouping<string, InventoryItem>> startItems = MainHelper.CategoryGrouping(startList);
                    IEnumerable<IGrouping<string, InventoryItem>> endItems = MainHelper.CategoryGrouping(endList);
                    Dictionary<string, List<InventoryItem>> purchaseDict = GetPurchasedByCategory(week.StartDate, week.EndDate);
                    foreach (string category in _models.GetInventoryCategories())
                    {
                        IGrouping<string, InventoryItem> startGroup = startItems.FirstOrDefault(x => x.Key == category);
                        IGrouping<string, InventoryItem> endGroup = endItems.FirstOrDefault(x => x.Key == category);

                        if (startGroup != null && endGroup != null)
                        {
                            List<InventoryItem> purchasedItems;
                            purchaseDict.TryGetValue(category, out purchasedItems);
                            CategoryList.Add(new CogsCategory(category, startGroup.ToList(), endGroup.ToList(),
                                                purchasedItems ?? new List<InventoryItem>()));
                        }
                    }
                }
            }
        }

        #endregion

        #region Initializers

        #endregion

        #region UI Updaters

        public void SubItemDoubleClicked(IInvEvent invEvent)
        {
            if(invEvent.GetType() == typeof(PurchaseOrder))
            {
                ViewOrderVM tabVM = new ViewOrderVM((PurchaseOrder)invEvent, ParentContext.OrderTab.RefreshOrderList);
                tabVM.Add("PO#: " + ((PurchaseOrder)invEvent).Id);
            }
            else if(invEvent.GetType() == typeof(Inventory))
            {
                NewInventoryVM tabVM = new NewInventoryVM(ParentContext.InventoryTab.Refresh, (Inventory)invEvent);
                tabVM.Add("View Inventory");
            }
        }

        #endregion

        private Dictionary<string, List<InventoryItem>> GetPurchasedByCategory(DateTime start, DateTime end)
        {
            // AddDays to include the end date as opposed to all received times up to and not including end date
            List<PurchaseOrder> orders = _models.PurchaseOrders.Where(x => x.ReceivedDate >= start &&
                                                                           x.ReceivedDate < end.Date.AddDays(1)).ToList();
            Dictionary<string, List<InventoryItem>> purchaseDict = new Dictionary<string, List<InventoryItem>>();

            foreach (PurchaseOrder order in orders)
            {
                foreach (InventoryItem item in order.GetReceivedPOItems())
                {
                    if (!purchaseDict.ContainsKey(item.Category))
                        purchaseDict[item.Category] = new List<InventoryItem>();

                    purchaseDict[item.Category].Add(item);
                }
            }

            return purchaseDict;
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
