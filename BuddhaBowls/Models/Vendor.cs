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
        public float ShippingCost { get; set; }

        public List<InventoryItem> ItemList { get; set; }

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
                InitItems();
            }
        }

        public void InitItems()
        {
            ItemList = GetInventoryItems();
        }

        /// <summary>
        /// Create new Vendor record and list of items that vendor sells
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public int Insert(List<InventoryItem> items)
        {
            if(items != null && items.Count > 0)
                ModelHelper.CreateTable(items, GetPriceTableName());
            ItemList = items;

            return base.Insert();
        }

        /// <summary>
        /// Update the vendor record and only update the items in the vendor price table
        /// </summary>
        /// <param name="items"></param>
        public void Update(List<InventoryItem> items)
        {
            foreach (InventoryItem item in items)
            {
                ItemList[ItemList.FindIndex(x => x.Id == item.Id)] = item;
            }

            ClearAndUpdate(ItemList);
        }

        /// <summary>
        /// Update the vendor record and update the price table to match the supplied inventory items.
        /// </summary>
        /// <remarks>Creates a new price table if one does not already exist</remarks>
        /// <param name="items"></param>
        public void ClearAndUpdate(List<InventoryItem> items)
        {
            ModelHelper.CreateTable(items, GetPriceTableName());
            ItemList = items;

            base.Update();
        }

        public void Update(InventoryItem item)
        {
            if (!File.Exists(_dbInt.FilePath(GetPriceTableName())))
                ModelHelper.CreateTable(new List<InventoryItem>() { item }, GetPriceTableName());
            else
                AddInvItem(item);

            base.Update();
        }

        /// <summary>
        /// Remove the vendor record and price table
        /// </summary>
        public override void Destroy()
        {
            string priceListPath = _dbInt.FilePath(GetPriceTableName());
            if (File.Exists(priceListPath))
            {
                File.Delete(priceListPath);
            }
            ItemList = null;
            base.Destroy();
        }

        public override string[] GetPropertiesDB(string[] omit = null)
        {
            string[] vendOmit = new string[] { "ItemList" };
            return base.GetPropertiesDB(ModelHelper.CombineArrays(omit, vendOmit));
        }

        public override int GetHashCode()
        {
            return Id;
        }

        /// <summary>
        /// Reset vendor back to when it was last saved in the DB
        /// </summary>
        public void Reset()
        {
            string[] record = _dbInt.GetRecord(_tableName, new Dictionary<string, string>() { { "Id", Id.ToString() } });

            if (record != null)
            {
                InitializeObject(record);
            }
        }

        /// <summary>
        /// Saves a text document for displaying the vendor-sold items for a receiving list
        /// </summary>
        /// <remarks>should match the order that the vendor displays their items on an invoice</remarks>
        /// <param name="listOrder"></param>
        public void SaveItemOrder(IEnumerable<string> listOrder)
        {
            File.WriteAllLines(GetOrderFile(), listOrder);
        }

        /// <summary>
        /// Gets the order of inventory items if one exists, otherwise returns the default order 
        /// </summary>
        /// <returns></returns>
        public List<string> GetRecListOrder()
        {
            if(File.Exists(GetOrderFile()))
            {
                return File.ReadAllLines(GetOrderFile()).ToList();
            }

            return Properties.Settings.Default.InventoryOrder;
        }

        public List<InventoryItem> GetItemsRecListOrder()
        {
            return MainHelper.SortItems(ItemList, GetRecListOrder()).ToList();
        }

        /// <summary>
        /// Gets the path to the order sheet for printing and filling out orders manually
        /// </summary>
        /// <returns></returns>
        public string GetOrderSheetPath()
        {
            return Path.Combine(Properties.Settings.Default.DBLocation, "Order Sheets", Name + ".xlsx");
        }

        /// <summary>
        /// Removes inventory item from the vendor-sold inventory item table
        /// </summary>
        /// <param name="item"></param>
        public void RemoveInvItem(InventoryItem item)
        {
            _dbInt.DeleteRecord(GetPriceTableName(), new Dictionary<string, string>() { { "Id", item.Id.ToString() } });
            ItemList.RemoveAll(x => x.Id == item.Id);
        }

        /// <summary>
        /// Gets the inventory items that the vendor offers for sale
        /// </summary>
        public List<InventoryItem> GetInventoryItems()
        {
            if (_dbInt.TableExists(GetPriceTableName()))
                return ModelHelper.InstantiateList<InventoryItem>(GetPriceTableName(), false);
            return null;
        }

        /// <summary>
        /// Adds or updates inventory item on vendor items sold list
        /// </summary>
        /// <param name="item"></param>
        private void AddInvItem(InventoryItem item)
        {
            if (!_dbInt.UpdateRecord(GetPriceTableName(), item.FieldsToDict(), item.Id))
            {
                _dbInt.WriteRecord(GetPriceTableName(), item.FieldsToDict(), item.Id);
            }

            InventoryItem existingItem = ItemList.FirstOrDefault(x => x.Id == item.Id);
            if(existingItem == null)
            {
                ItemList.Add(item);
            }
            else
            {
                ItemList[ItemList.IndexOf(existingItem)] = item;
            }
        }

        /// <summary>
        /// Gets the path to the file that specifies the vendor-specific order of inventory items
        /// </summary>
        /// <returns></returns>
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
