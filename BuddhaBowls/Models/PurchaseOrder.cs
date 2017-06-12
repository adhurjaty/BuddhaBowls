using BuddhaBowls.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BuddhaBowls.Models
{
    public class PurchaseOrder : Model
    {
        public string VendorName { get; set; }
        public DateTime OrderDate { get; set; }
        public bool IsPartial { get; set; }
        public DateTime? ReceivedDate { get; set; }

        public bool ReceivedCheck { get; set; }
        public bool Received
        {
            get
            {
                return ReceivedDate != null;
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

            // need to insert here to get the ID
            Insert();

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
            if (IsPartial)
            {
                CombinePartial();
            }
            ReceivedDate = DateTime.Now;
            Update();
        }

        public void ReOpen()
        {
            if (IsPartial)
            {
                CombinePartial();
            }
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

            if(IsPartial)
            {
                openItems = ModelHelper.InstantiateList<InventoryItem>(GetOpenPartialOrderTableName(), isModel: false);
                receivedItems = ModelHelper.InstantiateList<InventoryItem>(GetReceivedPartialOrderTableName(), isModel: false);
            }
            else
            {
                if(Received)
                    receivedItems = ModelHelper.InstantiateList<InventoryItem>(GetOrderTableName(), isModel: false);
                else
                    openItems = ModelHelper.InstantiateList<InventoryItem>(GetOrderTableName(), isModel: false);
            }
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

        public void SplitToPartials(List<InventoryItem> openItems, List<InventoryItem> receivedItems)
        {
            IsPartial = true;
            ReceivedDate = DateTime.Now;
            Update();

            _dbInt.DestroyTable(GetOrderTableName());
            ModelHelper.CreateTable(openItems, GetOpenPartialOrderTableName());
            ModelHelper.CreateTable(receivedItems, GetReceivedPartialOrderTableName());
        }

        public void DeleteOpenPartial()
        {
            IsPartial = false;
            Update();

            _dbInt.DestroyTable(GetOpenPartialOrderTableName());
            _dbInt.RenameTable(GetReceivedPartialOrderTableName(), GetOrderTableName());
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

        private void CombinePartial()
        {
            List<InventoryItem>[] bothItems = GetPOItems();
            List<InventoryItem> items = bothItems[0].Concat(bothItems[1]).ToList();
            _dbInt.DestroyTable(GetOpenPartialOrderTableName());
            _dbInt.DestroyTable(GetReceivedPartialOrderTableName());
            ModelHelper.CreateTable(items, GetOrderTableName());
            IsPartial = false;
        }

        #region Overrides
        public override string[] GetPropertiesDB(string[] omit = null)
        {
            string[] theseOmissions = new string[] { "Received", "ReceivedCheck" };
            return base.GetPropertiesDB(ModelHelper.CombineArrays(omit, theseOmissions));
        }

        public override void Destroy()
        {
            if (IsPartial)
            {
                _dbInt.DestroyTable(GetOpenPartialOrderTableName());
                _dbInt.DestroyTable(GetReceivedPartialOrderTableName());
            }
            else
            {
                _dbInt.DestroyTable(GetOrderTableName());
            }
            base.Destroy();
        }
        #endregion
    }
}
