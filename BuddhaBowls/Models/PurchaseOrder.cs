using BuddhaBowls.Helpers;
using BuddhaBowls.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace BuddhaBowls.Models
{
    public class PurchaseOrder : Model
    {
        private InventoryItemsContainer _invItemsContainer;

        private string _vendorName;
        public string VendorName
        {
            get
            {
                return _vendorName;
            }
            set
            {
                _vendorName = value;
                NotifyPropertyChanged("VendorName");
            }
        }

        private DateTime _orderDate;
        public DateTime OrderDate
        {
            get
            {
                return _orderDate;
            }
            set
            {
                _orderDate = value;
                NotifyPropertyChanged("OrderDate");
            }
        }

        private DateTime? _receivedDate;
        public DateTime? ReceivedDate
        {
            get
            {
                return _receivedDate;
            }
            set
            {
                _receivedDate = value;
                NotifyPropertyChanged("ReceivedDate");
                NotifyPropertyChanged("Received");
            }
        }

        private bool _receivedCheck;
        public bool ReceivedCheck
        {
            get
            {
                return _receivedCheck;
            }
            set
            {
                _receivedCheck = value;
                NotifyPropertyChanged("ReceivedCheck");
            }
        }

        public List<InventoryItem> ItemList
        {
            get
            {
                if (_invItemsContainer == null)
                    SetItemsContainer();
                return _invItemsContainer.Items;
            }
        }

        public bool Received
        {
            get
            {
                return ReceivedDate != null;
            }
        }

        // only use for datagrid
        public float TotalCost
        {
            get
            {
                return GetTotalCost();
            }
        }

        // only use for datagrid
        public List<OrderStat> OrderStats
        {
            get
            {
                return new List<OrderStat>(GetCategoryCosts().Select(x => new OrderStat() { Label = x.Key, Value = x.Value })); ;
            }
        }

        private Dictionary<string, List<InventoryItem>> _recCategoryItemsDict;
        public Dictionary<string, List<InventoryItem>> RecCategoryItemsDict
        {
            get
            {
                if (_orderChanged)
                {
                    _recCategoryItemsDict = GetPOItems().GroupBy(x => x.Category).ToDictionary(x => x.Key, x => x.ToList());
                    _orderChanged = false;
                }
                return _recCategoryItemsDict;
            }
        }

        public PurchaseOrder() : base()
        {
            _tableName = "PurchaseOrder";
        }

        /// <summary>
        /// Contruct Purchase Order - add to DB and create a table of inventory items
        /// </summary>
        /// <param name="vendor">Vendor for which the purhchase order is being made</param>
        /// <param name="inventoryItems">Items that are being purchased</param>
        /// <param name="orderDate">Date at which the order is being created</param>
        public PurchaseOrder(Vendor vendor, List<InventoryItem> inventoryItems, DateTime orderDate) : this()
        {
            VendorName = vendor.Name;
            OrderDate = orderDate;

            // double check that there are no duplicates in the inventory items. Correct this and notify user that there has been a problem if so
            // Remove this when I find the cause of the problem
            inventoryItems = RemoveDuplicates(inventoryItems);

            foreach (InventoryItem item in inventoryItems)
            {
                item.LastPurchasedDate = item.LastPurchasedDate == null || item.LastPurchasedDate < orderDate ? orderDate : item.LastPurchasedDate;
            }
            ModelHelper.CreateTable(inventoryItems, GetOrderTableName());

            // update the vendor table last order amounts and prices
            //vendor.Update(inventoryItems);
        }

        public void Receive()
        {
            ReceivedDate = DateTime.Now;
            Update();
            _orderChanged = true;
        }

        public void ReOpen()
        {
            ReceivedDate = null;
            Update();
            _orderChanged = true;
        }

        /// <summary>
        /// Get the ordered inventory items - { open items, received items }
        /// </summary>
        /// <returns></returns>
        public List<InventoryItem> GetPOItems()
        {
            return ModelHelper.InstantiateList<InventoryItem>(GetOrderTableName(), isModel: false);
        }

        public float GetTotalCost()
        {
            List<InventoryItem> items = GetPOItems();
            return items.Sum(x => x.PurchaseExtension);
        }

        public Dictionary<string, float> GetCategoryCosts()
        {
            Dictionary<string, float> catCosts = RecCategoryItemsDict.ToDictionary(x => x.Key, x => x.Value.Sum(y => y.PurchaseExtension));
            float total = catCosts.Sum(x => x.Value);
            catCosts["Food Total"] = catCosts.Where(x => Properties.Settings.Default.FoodCategories.Contains(x.Key)).Sum(x => x.Value);
            catCosts["Total"] = total;
            return catCosts;
        }

        public void UpdateItem(InventoryItem item)
        {
            List<InventoryItem> invItems = GetPOItems();
            int idx = invItems.FindIndex(x => x.Id == item.Id);
            invItems[idx] = item;

            ModelHelper.CreateTable(invItems, GetOrderTableName());
            _orderChanged = true;
        }

        public string GetPOPath()
        {
            return Path.Combine(Properties.Settings.Default.DBLocation, "Purchase Orders", VendorName + " " + OrderDate.ToString("MM-dd-yyyy")
                                + ".xlsx");
        }

        public string GetOrderPath()
        {
            return Path.Combine(Properties.Settings.Default.DBLocation, GetOrderTableName() + ".csv");
        }

        private void SetItemsContainer()
        {
            throw new NotImplementedException();
        }

        private string GetOrderTableName()
        {
            return @"Orders\" + VendorName + "_" + Id.ToString();
        }

        private string GetReceivedPartialOrderTableName()
        {
            return @"Orders\Partial_Received_" + VendorName + "_" + Id.ToString();
        }

        private string GetOpenPartialOrderTableName()
        {
            return @"Orders\Partial_Open_" + VendorName + "_" + Id.ToString();
        }

        #region Overrides
        public override string[] GetPropertiesDB(string[] omit = null)
        {
            string[] theseOmissions = new string[] { "Received", "ReceivedCheck", "TotalCost", "OrderStats" };
            return base.GetPropertiesDB(ModelHelper.CombineArrays(omit, theseOmissions));
        }

        public override void Destroy()
        {
            _dbInt.DestroyTable(GetOrderTableName());
            //_orderChanged = true;
            base.Destroy();
        }
        #endregion

        /// <summary>
        /// Updates the PO setting items as entire list of PO items
        /// </summary>
        /// <param name="items"></param>
        public void Update(List<InventoryItem> items)
        {
            ModelHelper.CreateTable(items, GetOrderTableName());
            _orderChanged = true;
            base.Update();
        }

        private List<InventoryItem> RemoveDuplicates(List<InventoryItem> inventoryItems, bool showMessage = true)
        {
            List<InventoryItem> outList = new List<InventoryItem>();
            HashSet<int> uniqueIds = new HashSet<int>(inventoryItems.Select(x => x.Id));
            if (inventoryItems.Count != uniqueIds.Count)
            {
                foreach (int id in uniqueIds)
                {
                    outList.Add(inventoryItems.First(x => x.Id == id));
                }

                if (showMessage)
                {
                    MessageBox.Show("Duplicates found when SAVING order. This has been corrected (hopefully). Screenshot this notification." +
                                    " Please contact Anil");
                }
            }
            else
                return inventoryItems;

            return outList;
        }

        public List<InventoryItem> RemoveViewingDuplicates(List<InventoryItem> inventoryItems)
        {
            List<InventoryItem> outList = RemoveDuplicates(inventoryItems, false);

            if(outList != inventoryItems)
            {
                MessageBox.Show("Duplicates found when VIEWING order. This has been corrected (hopefully). Screenshot this notification." +
                                    " Please contact Anil");
                ModelHelper.CreateTable(outList, GetOrderTableName());
            }

            return outList;
        }
    }
}
