﻿using BuddhaBowls.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace BuddhaBowls.Models
{
    public class PurchaseOrder : Model
    {
        public string VendorName { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? ReceivedDate { get; set; }

        public DateTime Date
        {
            get
            {
                return ReceivedDate ?? default(DateTime);
            }
        }

        public bool ReceivedCheck { get; set; }
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

            // need to insert here to get the ID, though I don't like it
            Insert();

            // double check that there are no duplicates in the inventory items. Correct this and notify user that there has been a problem if so
            // Remove this when I find the cause of the problem
            inventoryItems = RemoveDuplicates(inventoryItems);

            foreach (InventoryItem item in inventoryItems)
            {
                item.LastPurchasedDate = item.LastPurchasedDate == null || item.LastPurchasedDate < orderDate ? orderDate : item.LastPurchasedDate;
            }
            ModelHelper.CreateTable(inventoryItems, GetOrderTableName());

            // update the vendor table last order amounts adn prices
            vendor.Update(inventoryItems);
        }

        public void Receive()
        {
            ReceivedDate = DateTime.Now;
            Update();
        }

        public void ReOpen()
        {
            ReceivedDate = null;
            Update();
        }

        /// <summary>
        /// Get the ordered inventory items - { open items, received items }
        /// </summary>
        /// <returns></returns>
        public List<InventoryItem>[] GetPOItems()
        {
            List<InventoryItem> openItems = null;
            List<InventoryItem> receivedItems = null;

            if(Received)
                receivedItems = ModelHelper.InstantiateList<InventoryItem>(GetOrderTableName(), isModel: false);
            else
                openItems = ModelHelper.InstantiateList<InventoryItem>(GetOrderTableName(), isModel: false);
            return new List<InventoryItem>[] { openItems, receivedItems };
        }

        public List<InventoryItem> GetOpenPOItems()
        {
            return GetPOItems()[0];
        }

        public List<InventoryItem> GetReceivedPOItems()
        {
            return GetPOItems()[1];
        }

        public float GetTotalCost()
        {
            return GetReceivedPOItems().Sum(x => x.PurchaseExtension);
        }

        public Dictionary<string, float> GetCategoryCosts()
        {
            Dictionary<string, float> catCosts = GetReceivedPOItems().GroupBy(x => x.Category).ToDictionary(x => x.Key, x => x.Sum(y => y.PurchaseExtension));
            float total = catCosts.Sum(x => x.Value);
            catCosts["Food Total"] = catCosts.Where(x => Properties.Settings.Default.FoodCategories.Contains(x.Key)).Sum(x => x.Value);
            catCosts["Total"] = total;
            return catCosts;
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

        public string[] GetPartialOrderPaths()
        {
            List<string> paths = new List<string>();
            string[] tables = new string[] { GetOpenPartialOrderTableName(), GetReceivedPartialOrderTableName() };
            foreach (string table in tables)
            {
                paths.Add(Path.Combine(Properties.Settings.Default.DBLocation, table + ".csv"));
            }

            return paths.ToArray();
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
            base.Destroy();
        }
        #endregion

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
