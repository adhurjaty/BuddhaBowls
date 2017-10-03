﻿using BuddhaBowls.Helpers;
using BuddhaBowls.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Services
{
    /// <summary>
    /// Class to hold the inventory items associated with which vendors sell them
    /// </summary>
    public class VendorInvItemsContainer : ModelContainer<VendorInventoryItem>
    {
        // tracks vendors, necessary for each item to reference
        private VendorsContainer _vendorsContainer;

        /// <summary>
        /// Instantiate the container. Should only be called in the DBCache class
        /// </summary>
        /// <param name="items">List of vendor inventory items to contain</param>
        /// <param name="vContainer">Vendor container</param>
        public VendorInvItemsContainer(List<VendorInventoryItem> items, VendorsContainer vContainer) : base(items)
        {
            _vendorsContainer = vContainer;
        }

        /// <summary>
        /// Adds a new item with a list of which vendors sell the item
        /// </summary>
        /// <param name="item">New item</param>
        /// <param name="vendors">List of vendors that sell it</param>
        public void AddItem(InventoryItem item, List<VendorInfo> vendors)
        {
            int idx = Items.FindIndex(x => x.Id == item.Id);
            if (idx != -1)
            {
                _items[idx].Update(vendors);
                _items[idx].InvItem = item;
                PushChange();
            }
            else
            {
                VendorInventoryItem vItem = new VendorInventoryItem(item);

                vItem.Id = item.Insert();
                vItem.SetVendorDict(vendors);

                int insertIdx = Properties.Settings.Default.InventoryOrder.IndexOf(item.Name);
                if (insertIdx == -1)
                    _items.Add(vItem);
                else
                    _items.Insert(insertIdx, vItem);

                PushChange();
            }
        }

        /// <summary>
        /// Removes item from current inventory list and all vendor lists
        /// </summary>
        /// <param name="item"></param>
        public override void RemoveItem(VendorInventoryItem item)
        {
            _vendorsContainer.RemoveItemFromVendors(item);

            base.RemoveItem(item);
        }

        /// <summary>
        /// Updates the items in the list. Does not remove any items from the master list
        /// </summary>
        /// <param name="items"></param>
        public void Update(List<VendorInventoryItem> items)
        {
            foreach (VendorInventoryItem item in items)
            {
                int idx = Items.FindIndex(x => x.Id == item.Id);
                Items[idx] = item;
            }
            PushChange();
        }

        /// <summary>
        /// Copy the container
        /// </summary>
        /// <returns></returns>
        public VendorInvItemsContainer Copy()
        {
            return new VendorInvItemsContainer(_items, _vendorsContainer);
        }

        /// <summary>
        /// Adds vendor to vendor container and associates items with new vendor
        /// </summary>
        /// <param name="vend"></param>
        public void AddVendor(Vendor vend, List<InventoryItem> invItems)
        {
            _vendorsContainer.AddItem(vend);
            vend.SetItemList(invItems);
            foreach (InventoryItem item in invItems)
            {
                Items.First(x => x.Id == item.Id).AddVendor(vend, item);
            }
            PushChange();
        }

        /// <summary>
        /// Removes a vendor from vendor container and all associations with inv items
        /// </summary>
        /// <param name="vend"></param>
        public void RemoveVendor(Vendor vend)
        {
            _vendorsContainer.RemoveItem(vend);
            foreach (InventoryItem item in vend.ItemList)
            {
                Items.First(x => x.Id == item.Id).DeleteVendor(vend);
            }
            PushChange();
        }

        /// <summary>
        /// Save the display order of the inventory items
        /// </summary>
        public void SaveOrder()
        {
            string dir = Path.Combine(Properties.Settings.Default.DBLocation, "Settings");
            Directory.CreateDirectory(dir);
            File.WriteAllLines(Path.Combine(dir, GlobalVar.INV_ORDER_FILE), Properties.Settings.Default.InventoryOrder);
            _items = MainHelper.SortItems(_items).ToList();
        }

        /// <summary>
        /// Get the PriceExtension value of each category of items
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, float> GetCategoryValues()
        {
            Dictionary<string, float> costDict = new Dictionary<string, float>();

            foreach (VendorInventoryItem item in _items)
            {
                if (!costDict.Keys.Contains(item.Category))
                    costDict[item.Category] = 0;
                costDict[item.Category] += item.PriceExtension;
            }

            return costDict;
        }

        /// <summary>
        /// Update (push to DB) all of the items in the container
        /// </summary>
        public void UpdateContainer()
        {
            foreach (VendorInventoryItem item in Items)
            {
                item.Update();
            }
        }

        /// <summary>
        /// Associates item with vendor and updates vendor
        /// </summary>
        /// <param name="item"></param>
        /// <param name="vend"></param>
        public void UpdateItem(InventoryItem item, Vendor vend)
        {
            vend.AddInvItem(item);
            _vendorsContainer.Update(vend);
            PushChange();
        }

        /// <summary>
        /// Associates vendor with all of items in invItems, removes association with vendor and items not in invItems
        /// </summary>
        /// <param name="vendor"></param>
        /// <param name="list"></param>
        public void UpdateVendorItems(Vendor vendor, List<InventoryItem> invItems)
        {
            RemoveVendor(vendor);
            AddVendor(vendor, invItems);
        }
    }
}
