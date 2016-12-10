using BuddhaBowls.Helpers;
using System;
using System.Collections.Generic;

namespace BuddhaBowls.Models
{
    public class PurchaseOrder : Model
    {
        public string Company { get; set; }
        public DateTime Timestamp { get; set; }

        public PurchaseOrder() : base()
        {
            _tableName = "PurchaseOrder";
        }

        public PurchaseOrder(string vendor, List<InventoryItem> inventoryItems) : this()
        {
            Company = vendor;
            Timestamp = DateTime.Now;

            Insert();

            ModelHelper.CreateTable(inventoryItems, @"Orders\" + vendor + "_" + Id.ToString());
        }
    }
}
