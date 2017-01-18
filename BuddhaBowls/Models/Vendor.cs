﻿using BuddhaBowls.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Models
{
    public class Vendor : Model
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Contact { get; set; }

        public Vendor() : base()
        {
            _tableName = "Vendor";
        }

        public Vendor(Dictionary<string, string> searchParams) : this()
        {
            string[] record = _dbInt.GetRecord(_tableName, searchParams);

            if (record != null)
            {
                InitializeObject(record);
            }
        }

        public void Reset()
        {
            string[] record = _dbInt.GetRecord(_tableName, new Dictionary<string, string>() { { "Id", Id.ToString() } });

            if (record != null)
            {
                InitializeObject(record);
            }
        }

        public List<InventoryItem> GetFromPriceList()
        {
            string tableName = @"Vendors\" + Name + "_Prices";
            if(_dbInt.TableExists(tableName))
                return ModelHelper.InstantiateList<InventoryItem>(tableName, false);
            return null;
        }

        public void SaveItemOrder(IEnumerable<string> listOrder)
        {
            File.WriteAllLines(GetOrderFile(), listOrder);
        }

        public List<string> GetRecListOrder()
        {
            if(File.Exists(GetOrderFile()))
            {
                return File.ReadAllLines(GetOrderFile()).ToList();
            }

            return Properties.Settings.Default.InventoryOrder;
        }

        private string GetOrderFile()
        {
            return Path.Combine(Properties.Settings.Default.DBLocation, "Vendors", Name + "_order.txt");
        }
    }
}
