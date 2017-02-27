using BuddhaBowls.Helpers;
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
            if(_dbInt.TableExists(GetPriceTableName()))
                return ModelHelper.InstantiateList<InventoryItem>(GetPriceTableName(), false);
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

        public void UpdatePrices(List<InventoryItem> inventoryItems)
        {
            string tableName = GetPriceTableName();
            if (_dbInt.TableExists(tableName))
            {
                foreach (InventoryItem item in inventoryItems)
                {
                    AddInvItem(item);
                }
            }
            else
            {
                File.Copy(_dbInt.FilePath("InventoryItem"), _dbInt.FilePath(tableName));
                List<InventoryItem> allItems = ModelHelper.InstantiateList<InventoryItem>(tableName, false);
                foreach (InventoryItem item in allItems.Where(x => !inventoryItems.Select(y => y.Id).Contains(x.Id)))
                {
                    _dbInt.DeleteRecord(tableName, new Dictionary<string, string>() { { "Id", item.Id.ToString() } });
                }
            }
        }

        public string GetItemsListPath()
        {
            return Path.Combine(Properties.Settings.Default.DBLocation, "Order Sheets", Name + ".xlsx");
        }

        public void AddInvItem(InventoryItem item)
        {
            if (!_dbInt.UpdateRecord(GetPriceTableName(), item.FieldsToDict(), item.Id))
            {
                _dbInt.WriteRecord(GetPriceTableName(), item.FieldsToDict());
            }
        }

        private string GetOrderFile()
        {
            return Path.Combine(Properties.Settings.Default.DBLocation, "Vendors", Name + "_order.txt");
        }

        private string GetPriceTableName()
        {
            return @"Vendors\" + Name + "_Prices";
        }

    }
}
