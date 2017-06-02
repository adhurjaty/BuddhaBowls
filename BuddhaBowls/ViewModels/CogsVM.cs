﻿using BuddhaBowls.Helpers;
using BuddhaBowls.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BuddhaBowls
{
    public class CogsVM : TabVM
    {
        #region Content Binders

        private string _header;
        public string Header
        {
            get
            {
                return _header;
            }
            set
            {
                _header = value;
                NotifyPropertyChanged("Header");
            }
        }

        private DateTime _startDate;
        public DateTime StartDate
        {
            get
            {
                return _startDate;
            }
            set
            {
                _startDate = value;
                NotifyPropertyChanged("StartDate");
                if (DBConnection && StartDate < EndDate)
                    CalculateCogs();
            }
        }

        private DateTime _endDate;
        public DateTime EndDate
        {
            get
            {
                return _endDate;
            }
            set
            {
                _endDate = value;
                NotifyPropertyChanged("EndDate");
                if (DBConnection && StartDate < EndDate)
                    CalculateCogs();
            }
        }

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
        #endregion

        #region ICommand and CanExecute

        #endregion

        public CogsVM() : base()
        {
            Header = "Cost of Goods Sold";

            if(DBConnection)
                SetInitCogs();
        }

        #region ICommand Helpers

        private void CalculateCogs()
        {
            List<Inventory> inventoryList = _models.Inventories.OrderByDescending(x => x.Date).ToList();
            Inventory endInv = inventoryList.FirstOrDefault(x => x.Date <= EndDate);

            CategoryList = new ObservableCollection<CogsCategory>();
            if(endInv != null)
            {
                Inventory startInv = inventoryList.FirstOrDefault(x => x.Date <= StartDate);
                if (startInv == null)
                    startInv = inventoryList.Last();

                IEnumerable<IGrouping<string, InventoryItem>> startItems = MainHelper.CategoryGrouping(startInv.GetInventoryHistory());
                IEnumerable <IGrouping <string, InventoryItem>> endItems = MainHelper.CategoryGrouping(endInv.GetInventoryHistory());
                Dictionary<string, List<InventoryItem>> purchaseDict = GetPurchasedByCategory(StartDate, EndDate);
                foreach (string category in _models.GetInventoryCategories())
                {
                    IGrouping<string, InventoryItem> startGroup = startItems.FirstOrDefault(x => x.Key == category);
                    IGrouping<string, InventoryItem> endGroup = endItems.FirstOrDefault(x => x.Key == category);

                    if(startGroup != null && endGroup != null)
                    {
                        List<InventoryItem> purchasedItems;
                        purchaseDict.TryGetValue(category, out purchasedItems);
                        CategoryList.Add(new CogsCategory(category, startGroup.ToList(), endGroup.ToList(),
                                            purchasedItems ?? new List<InventoryItem>()));
                    }
                }
            }
        }

        #endregion

        #region Initializers

        private void SetInitCogs()
        {
            List<Inventory> inventoryList = _models.Inventories.OrderByDescending(x => x.Date).ToList();
            if(inventoryList.Count >= 2)
            {
                EndDate = inventoryList[0].Date;
                StartDate = inventoryList[1].Date;
                CalculateCogs();
            }
        }

        #endregion

        #region UI Updaters

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
