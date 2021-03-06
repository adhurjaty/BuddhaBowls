﻿using BuddhaBowls.Helpers;
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
    /// <summary>
    /// Temp tab to compare inventories from different dates - opened from the Inventory tab
    /// </summary>
    public class CompareInvVM : TempTabVM
    {
        private Inventory _beginInv;
        private Inventory _endInv;
        private List<CompareItem> _compareItems;

        #region Data Bindings
        public string FilterText { get; set; }

        private ObservableCollection<CompareItem> _filteredCompItems;
        public ObservableCollection<CompareItem> FilteredCompItems
        {
            get
            {
                return _filteredCompItems;
            }
            set
            {
                _filteredCompItems = value;
                NotifyPropertyChanged("FilteredCompItems");
            }
        }

        public Vendor SelectedCompItem { get; set; }
        public ObservableCollection<FieldSetting> FieldsCollection { get; private set; }

        private string _dateRange;
        public string DateRange
        {
            get
            {
                return _dateRange;
            }
            set
            {
                _dateRange = value;
                NotifyPropertyChanged("DateRange");
            }
        }
        #endregion

        #region ICommand Bindings and Can Execute

        public ICommand CloseCommand { get; set; }
        // Change Rec List button in vendor main tab
        public ICommand ChangeRecListOrderCommand { get; set; }
        public ICommand ChangeVendorItemsCommand { get; set; }
        public ICommand GetOrderSheetCommand { get; set; }

        public bool SelectedVendorCanExecute
        {
            get
            {
                return SelectedCompItem != null;
            }
        }

        #endregion

        public CompareInvVM(Inventory beginInv, Inventory endInv) : base()
        {
            _beginInv = beginInv;
            _endInv = endInv;
            _tabControl = new CompareInventoriesControl(this);

            DateRange = beginInv.Date.ToString("M/dd/yy") + " - " + endInv.Date.ToString("M/dd/yy");

            CloseCommand = new RelayCommand(CloseTab);

            InitCompList();
            Refresh();
        }

        #region ICommand Helpers

        private void CloseTab(object obj)
        {
            Close();
        }

        #endregion

        #region Initializers

        private void InitCompList()
        {
            _compareItems = new List<CompareItem>();
            List<InventoryItem> beginInvList = _beginInv.GetInventoryHistory();
            List<InventoryItem> endInvList = _endInv.GetInventoryHistory();
            MatchInvLists(ref beginInvList, ref endInvList);

            // AddDays to include the end date as opposed to all received times up to and not including end date
            List<PurchaseOrder> orders = _models.PurchaseOrders.Where(x => x.ReceivedDate >= _beginInv.Date &&
                                                                           x.ReceivedDate < _endInv.Date.AddDays(1)).ToList();
            Dictionary<int, float> amountOrdered = new Dictionary<int, float>();
            Dictionary<int, float> totalItemPrice = new Dictionary<int, float>();

            foreach (PurchaseOrder order in orders)
            {
                foreach (InventoryItem item in order.GetReceivedPOItems())
                {
                    if (!amountOrdered.Keys.Contains(item.Id))
                    {
                        amountOrdered[item.Id] = 0;
                        totalItemPrice[item.Id] = 0;
                    }

                    amountOrdered[item.Id] += item.LastOrderAmount * item.Conversion;
                    totalItemPrice[item.Id] += item.LastOrderAmount * item.LastPurchasedPrice;
                }
            }

            for (int i = 0; i < beginInvList.Count; i++)
            {
                CompareItem compItem = new CompareItem(beginInvList[i], endInvList[i]);
                float orderedAmount = 0;
                float itemPrice = 0;
                amountOrdered.TryGetValue(beginInvList[i].Id, out orderedAmount);
                totalItemPrice.TryGetValue(beginInvList[i].Id, out itemPrice);

                compItem.Usage = compItem.Count + orderedAmount - compItem.EndCount;
                compItem.Cost = compItem.Count * beginInvList[i].LastPurchasedPrice + itemPrice -
                                compItem.EndCount * endInvList[i].LastPurchasedPrice;
                _compareItems.Add(compItem);
            }
        }

        /// <summary>
        /// Alter parameter lists such that the Names and Ids match at same index
        /// </summary>
        /// <param name="list1"></param>
        /// <param name="list2"></param>
        private void MatchInvLists(ref List<InventoryItem> list1, ref List<InventoryItem> list2)
        {
            list1 = list1.OrderBy(x => x.Id).ToList();
            list2 = list2.OrderBy(x => x.Id).ToList();

            for(int i = 0; i < list1.Count; i++)
            {
                if(list1[i].Id > list2[i].Id)
                {
                    list1.Insert(i, new InventoryItem() { Id = list2[i].Id, Name = list2[i].Name });
                    i--;
                }
                if(list1[i].Id < list2[i].Id)
                {
                    list2.Insert(i, new InventoryItem() { Id = list1[i].Id, Name = list1[i].Name });
                }
            }
        }
        #endregion

        #region Update UI Methods
        public override void Refresh()
        {
            FilteredCompItems = new ObservableCollection<CompareItem>(MainHelper.SortItems(_compareItems));
        }

        public override void FilterItems(string filterStr)
        {
            if (string.IsNullOrWhiteSpace(filterStr))
                Refresh();
            else
            {
                FilteredCompItems = new ObservableCollection<CompareItem>
                    (
                        MainHelper.SortItems(_compareItems.Where(x => x.Name.ToUpper().Contains(filterStr.ToUpper())))
                    );
            }
        }

        #endregion
    }

    // TODO: figure out IItem
    public class CompareItem : Model, IItem
    {
        public string Category { get; set; }
        public float Count { get; set; }
        public string CountUnit { get; set; }
        public float EndCount { get; set; }
        public string Name { get; set; }
        public string RecipeUnit { get; set; }
        public float? RecipeUnitConversion { get; set; }
        public float Usage { get; set; }
        public float Cost { get; set; }

        public CompareItem() { }

        public CompareItem(InventoryItem beforeItem, InventoryItem afterItem)
        {
            if (beforeItem.Id != afterItem.Id)
                throw new Exception("Items must have the same ID");

            Name = beforeItem.Name;
            Id = beforeItem.Id;
            Count = beforeItem.Count;
            EndCount = afterItem.Count;
        }

        public float GetCost()
        {
            throw new NotImplementedException();
        }

        public IItem Copy()
        {
            return Copy<CompareItem>();
        }

    }
}
