﻿using BuddhaBowls.Helpers;
using BuddhaBowls.Services;
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
        private InventoryItemsContainer _invItemsContainer;

        private string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                // HACK: set _invItemsContainer as soon as program can get name of table
                if (!string.IsNullOrEmpty(_name) && _invItemsContainer == null)
                    SetItemsContainer();
                NotifyPropertyChanged("Name");
            }
        }

        private string _email;
        public string Email
        {
            get
            {
                return _email;
            }
            set
            {
                _email = value;
                NotifyPropertyChanged("Email");
            }
        }

        private string _phoneNumber;
        public string PhoneNumber
        {
            get
            {
                return _phoneNumber;
            }
            set
            {
                _phoneNumber = value;
                NotifyPropertyChanged("PhoneNumber");
            }
        }

        private string _contact;
        public string Contact
        {
            get
            {
                return _contact;
            }
            set
            {
                _contact = value;
                NotifyPropertyChanged("Contact");
            }
        }

        private float _shippingCost;
        public float ShippingCost
        {
            get
            {
                return _shippingCost;
            }
            set
            {
                _shippingCost = value;
                NotifyPropertyChanged("ShippingCost");
            }
        }

        //private List<InventoryItem> _itemList;
        public List<InventoryItem> ItemList
        {
            get
            {
                if (_invItemsContainer == null)
                    SetItemsContainer();
                return _invItemsContainer.Items;
            }
        }

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
                //InitItems();
            }
            SetItemsContainer();
        }

        /// <summary>
        /// Insert the vendor and create a price table if there exist any items
        /// </summary>
        /// <returns></returns>
        public override int Insert()
        {
            if(ItemList != null && ItemList.Count > 0)
                ModelHelper.CreateTable(ItemList, GetPriceTableName());
            return base.Insert();
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
            _invItemsContainer = new InventoryItemsContainer(items);
            NotifyPropertyChanged("ItemList");

            return base.Insert();
        }

        public override void Update()
        {
            if(ItemList != null && ItemList.Count > 0)
                ModelHelper.CreateTable(ItemList, GetPriceTableName());
            base.Update();
        }

        public void Update(InventoryItem item)
        {
            _invItemsContainer.AddItem(item);
            NotifyPropertyChanged("ItemList");
            Update();
        }

        /// <summary>
        /// Update the vendor record and only update the items in the vendor price table
        /// </summary>
        /// <param name="items"></param>
        public void Update(List<InventoryItem> items)
        {
            _invItemsContainer.UpdateMultiple(items);
            NotifyPropertyChanged("ItemList");
            Update();
        }

        /// <summary>
        /// Update the vendor record and update the price table to match the supplied inventory items.
        /// </summary>
        /// <remarks>Creates a new price table if one does not already exist</remarks>
        /// <param name="items"></param>
        //public void ClearAndUpdate(List<InventoryItem> items)
        //{
        //    ModelHelper.CreateTable(items, GetPriceTableName());
        //    ItemList = items;

        //    base.Update();
        //}

        //public void Update(InventoryItem item)
        //{
        //    if (!File.Exists(_dbInt.FilePath(GetPriceTableName())))
        //        ModelHelper.CreateTable(new List<InventoryItem>() { item }, GetPriceTableName());
        //    else
        //        AddInvItem(item);

        //    base.Update();
        //}

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
            _invItemsContainer = null;
            NotifyPropertyChanged("ItemList");
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

        public Vendor Copy()
        {
            Vendor v = base.Copy<Vendor>();
            v.SetItemsContainer(v.CopyItems());
            return v;
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
            _invItemsContainer.RemoveItem(item);
            NotifyPropertyChanged("ItemList");
        }

        /// <summary>
        /// Gets the inventory items that the vendor offers for sale
        /// </summary>
        public List<InventoryItem> GetInventoryItems()
        {
            if (_dbInt.TableExists(GetPriceTableName()))
                return MainHelper.SortItems(ModelHelper.InstantiateList<InventoryItem>(GetPriceTableName(), false)).ToList();
            return new List<InventoryItem>();
        }

        private void SetItemsContainer()
        {
            if (_dbInt.TableExists(GetPriceTableName()))
                SetItemsContainer(new InventoryItemsContainer(MainHelper.SortItems(ModelHelper.InstantiateList<InventoryItem>(GetPriceTableName(), false)).ToList()));
            else
                SetItemsContainer(new InventoryItemsContainer(new List<InventoryItem>()));
        }

        public void SetItemsContainer(InventoryItemsContainer cont)
        {
            _invItemsContainer = cont;
            NotifyPropertyChanged("ItemList");
        }

        public InventoryItemsContainer CopyItems()
        {
            return _invItemsContainer.Copy();
        }

        /// <summary>
        /// Adds or updates inventory item on vendor items sold list
        /// </summary>
        /// <param name="item"></param>
        public void AddInvItem(InventoryItem item)
        {
            _invItemsContainer.AddItem(item);
            NotifyPropertyChanged("ItemList");
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
