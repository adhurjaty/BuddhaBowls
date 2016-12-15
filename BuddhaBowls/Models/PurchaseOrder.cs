﻿using BuddhaBowls.Helpers;
using System;
using System.Collections.Generic;

namespace BuddhaBowls.Models
{
    public class PurchaseOrder : Model
    {
        public string Company { get; set; }
        public DateTime OrderDate { get; set; }

        private DateTime? _receivedDate;
        public DateTime? ReceivedDate
        {
            get
            {
                return _receivedDate;
            }
            set
            {
                if(ReceivedDate != null)
                    _received = true;
                _receivedDate = value;
            }
        }

        private bool _received;
        public bool Received
        {
            get
            {
                return _received;
            }
            set
            {
                _received = value;
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

            ModelHelper.CreateTable(inventoryItems, GetOrderTableName());
        }

        public override string[] GetPropertiesDB(string[] omit = null)
        {
            return base.GetPropertiesDB(new string[] { "Received" });
        }

        public List<InventoryItem> GetPOItems()
        {
            return ModelHelper.InstantiateList<InventoryItem>(GetOrderTableName(), false);
        }

        public override void Destroy()
        {
            _dbInt.DestroyTable(GetOrderTableName());
            base.Destroy();
        }

        private string GetOrderTableName()
        {
            return @"Orders\" + Company + "_" + Id.ToString();
        }
    }
}
