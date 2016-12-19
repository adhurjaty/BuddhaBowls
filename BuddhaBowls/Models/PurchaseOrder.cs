using BuddhaBowls.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BuddhaBowls.Models
{
    public class PurchaseOrder : Model
    {
        public string Company { get; set; }
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

        public PurchaseOrder(string vendor, List<InventoryItem> inventoryItems) : this()
        {
            Company = vendor;
            OrderDate = DateTime.Now;

            Insert();

            foreach (InventoryItem item in inventoryItems)
            {
                item.LastPurchasedDate = DateTime.Now;
            }
            ModelHelper.CreateTable(inventoryItems, GetOrderTableName());
        }

        public void Receive()
        {
            CombinePartial();
            ReceivedDate = DateTime.Now;
            Update();
        }

        public void ReOpen()
        {
            CombinePartial();
            ReceivedDate = null;
            Update();
        }

        public List<InventoryItem>[] GetPOItems()
        {
            List<InventoryItem> openItems = null;
            List<InventoryItem> receivedItems = null;

            if(IsPartial)
            {
                openItems = ModelHelper.InstantiateList<InventoryItem>(GetOpenPartialOrderTableName(), fileExists: false);
                receivedItems = ModelHelper.InstantiateList<InventoryItem>(GetReceivedPartialOrderTableName(), fileExists: false);
            }
            else
            {
                if(Received)
                    receivedItems = ModelHelper.InstantiateList<InventoryItem>(GetOrderTableName(), fileExists: false);
                else
                    openItems = ModelHelper.InstantiateList<InventoryItem>(GetOrderTableName(), fileExists: false);
            }
            return new List<InventoryItem>[] { openItems, receivedItems };
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

        private string GetOrderTableName()
        {
            return @"Orders\" + Company + "_" + Id.ToString();
        }

        private string GetReceivedPartialOrderTableName()
        {
            return @"Orders\Partial_Received_" + Company + "_" + Id.ToString();
        }

        private string GetOpenPartialOrderTableName()
        {
            return @"Orders\Partial_Open_" + Company + "_" + Id.ToString();
        }

        private void CombinePartial()
        {
            if (IsPartial)
            {
                List<InventoryItem>[] bothItems = GetPOItems();
                List<InventoryItem> items = bothItems[0].Concat(bothItems[1]).ToList();
                _dbInt.DestroyTable(GetOpenPartialOrderTableName());
                _dbInt.DestroyTable(GetReceivedPartialOrderTableName());
                ModelHelper.CreateTable(items, GetOrderTableName());
                IsPartial = false;
            }
        }

        #region Overrides
        public override string[] GetPropertiesDB(string[] omit = null)
        {
            return base.GetPropertiesDB(new string[] { "Received", "ReceivedCheck" });
        }

        public override void Destroy()
        {
            _dbInt.DestroyTable(GetOrderTableName());
            base.Destroy();
        }
        #endregion
    }
}
