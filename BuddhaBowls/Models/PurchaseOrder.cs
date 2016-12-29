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

        public PurchaseOrder(string vendor, List<InventoryItem> inventoryItems) : this()
        {
            VendorName = vendor;
            OrderDate = DateTime.Now;

            Insert();

            foreach (InventoryItem item in inventoryItems)
            {
                item.LastPurchasedDate = DateTime.Now;
            }
            ModelHelper.CreateTable(inventoryItems, GetOrderTableName());
            UpdatePrices(inventoryItems);
        }

        private void UpdatePrices(List<InventoryItem> inventoryItems)
        {
            if(_dbInt.TableExists(GetPriceTableName()))
            {
                foreach (InventoryItem item in inventoryItems)
                {
                    if (!_dbInt.UpdateRecord(GetPriceTableName(), item.FieldsToDict(), item.Id))
                    {
                        _dbInt.WriteRecord(GetPriceTableName(), item.FieldsToDict());
                    }
                }
            }
            else
            {
                File.Copy(_dbInt.FilePath("InventoryItem"), _dbInt.FilePath(GetPriceTableName()));
            }
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

        public List<InventoryItem> GetOpenPOItems()
        {
            return GetPOItems()[0];
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

        private string GetPriceTableName()
        {
            return @"Vendor Prices\" + VendorName + "_Prices";
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
            string[] theseOmissions = new string[] { "Received", "ReceivedCheck" };
            return base.GetPropertiesDB(ModelHelper.CombineArrays(omit, theseOmissions));
        }

        public override void Destroy()
        {
            _dbInt.DestroyTable(GetOrderTableName());
            base.Destroy();
        }
        #endregion
    }
}
