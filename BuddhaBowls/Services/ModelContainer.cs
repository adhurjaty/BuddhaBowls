﻿using BuddhaBowls.Helpers;
using BuddhaBowls.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Services
{
    public class ModelContainer
    {
        private Dictionary<string, string> _categoryColors;

        private List<InventoryItem> _inventoryItems;
        public List<InventoryItem> InventoryItems
        {
            get
            {
                return _inventoryItems;
            }
            set
            {
                _inventoryItems = value;

                if (_inventoryItems != null && VendorInvItems == null)
                {
                    VendorInvItems = _inventoryItems.Select(x => new VendorInventoryItem(GetVendorsFromItem(x), x)).ToList();
                }
            }
        }

        public List<Recipe> Recipes { get; set; }
        public List<PurchaseOrder> PurchaseOrders { get; set; }
        public List<Vendor> Vendors { get; set; }
        public List<Inventory> Inventories { get; set; }
        public List<PrepItem> PrepItems { get; set; }
        public List<VendorInventoryItem> VendorInvItems { get; private set; }

        public ModelContainer()
        {
            InitializeModels();
            InitializeInventoryOrder();
            if (InventoryItems != null)
            {
                SetCategoryColors();
            }
        }

        private void InitializeModels()
        {
            Vendors = ModelHelper.InstantiateList<Vendor>("Vendor") ?? new List<Vendor>();
            InventoryItems = MainHelper.SortItems(ModelHelper.InstantiateList<InventoryItem>("InventoryItem") ?? new List<InventoryItem>()).ToList();
            Recipes = ModelHelper.InstantiateList<Recipe>("Recipe") ?? new List<Recipe>();
            PurchaseOrders = ModelHelper.InstantiateList<PurchaseOrder>("PurchaseOrder") ?? new List<PurchaseOrder>();
            Inventories = ModelHelper.InstantiateList<Inventory>("Inventory") ?? new List<Inventory>();
            PrepItems = ModelHelper.InstantiateList<PrepItem>("PrepItem") ?? new List<PrepItem>();
        }

        private void InitializeInventoryOrder()
        {
            string orderPath = Path.Combine(Properties.Settings.Default.DBLocation, "Settings", GlobalVar.INV_ORDER_FILE);
            if (File.Exists(orderPath))
                Properties.Settings.Default.InventoryOrder = new List<string>(File.ReadAllLines(orderPath));
            else
                Properties.Settings.Default.InventoryOrder = new List<string>(InventoryItems.Select(x => x.Name).OrderBy(x => x));
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Get the total cost of the recipe's ingredients
        /// </summary>
        /// <param name="rec"></param>
        /// <returns>Cost</returns>
        public float GetBatchItemCost(Recipe rec)
        {
            return rec.GetCost();
        }

        /// <summary>
        /// Gets the color (excel format) from the passed-in category (case insensitive)
        /// </summary>
        /// <remarks>Probably no need to test</remarks>
        public long GetColorFromCategory(string category)
        {
            string key = _categoryColors.Keys.FirstOrDefault(x => x.ToUpper() == category.ToUpper());
            if (!string.IsNullOrEmpty(key))
            {
                return MainHelper.ColorFromString(_categoryColors[key]);
            }

            return MainHelper.ColorFromString(GlobalVar.BLANK_COLOR);
        }

        /// <summary>
        /// Gets the hex representation of the color given the passed-in category (case insensitive)
        /// </summary>
        /// <remarks>Probably no need to test</remarks>
        public string GetCategoryColorHex(string category)
        {
            return "#" + (_categoryColors.Keys.Contains(category.ToUpper()) ?
                            _categoryColors[category.ToUpper()] : GlobalVar.BLANK_COLOR);
        }

        /// <summary>
        /// Get all inventory items and batch recipe items
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IItem> GetIngredients()
        {
            return InventoryItems.Select(x => (IItem)x).Concat(Recipes.Where(x => x.IsBatch));
        }

        /// <summary>
        /// Get all of the count units that currently exist in inventory items
        /// </summary>
        /// <returns></returns>
        public List<string> GetCountUnits()
        {
            return EnsureCaseInsensitive(new HashSet<string>(GetAllIItems().Select(x => x.CountUnit)
                                                                           .Where(x => !string.IsNullOrEmpty(x)))).ToList();
        }

        public List<string> GetPrepCountUnits()
        {
            return EnsureCaseInsensitive(new HashSet<string>(PrepItems.Select(x => x.CountUnit)
                                                                           .Where(x => !string.IsNullOrEmpty(x)))).ToList();
        }

        /// <summary>
        /// Get all of the recipe units that currently exist in inventory items
        /// </summary>
        /// <returns></returns>
        public List<string> GetRecipeUnits()
        {
            return EnsureCaseInsensitive(new HashSet<string>(GetAllIItems().Select(x => x.RecipeUnit)
                                                                            .Where(x => !string.IsNullOrEmpty(x)))).ToList();
        }

        /// <summary>
        /// Get all of the purchased units that currently exist in inventory items
        /// </summary>
        /// <returns></returns>
        public List<string> GetPurchasedUnits()
        {
            return EnsureCaseInsensitive(new HashSet<string>(InventoryItems.Select(x => x.PurchasedUnit)
                                                                           .Where(x => !string.IsNullOrEmpty(x)))).ToList();
        }

        /// <summary>
        /// Add or update an inventory item - adds or updates db and the item within the model container
        /// </summary>
        /// <param name="item"></param>
        public void AddUpdateInventoryItem(ref InventoryItem item)
        {
            if(InventoryItems.Select(x => x.Id).Contains(item.Id))
            {
                item.Update();
                int itemId = item.Id;
                InventoryItems[InventoryItems.FindIndex(x => x.Id == itemId)] = item;
                VendorInvItems[VendorInvItems.FindIndex(x => x.Id == itemId)] = new VendorInventoryItem(GetVendorsFromItem(item), item);
            }
            else
            {
                item.Id = item.Insert();
                InventoryItems.Add(item);
                VendorInvItems.Add(new VendorInventoryItem(GetVendorsFromItem(item), item));
            }
            InventoryItems = MainHelper.SortItems(InventoryItems).ToList();
            VendorInvItems = MainHelper.SortItems(VendorInvItems).ToList();
        }

        /// <summary>
        /// Removes inventory item from db and from model container
        /// </summary>
        /// <param name="item"></param>
        /// <returns>Whether or not the item existed and was deleted</returns>
        public bool DeleteInventoryItem(InventoryItem item)
        {
            if(InventoryItems.First(x => x.Id == item.Id) == null)
                return false;
            InventoryItems.RemoveAll(x => x.Id == item.Id);
            VendorInvItems.RemoveAll(x => x.Id == item.Id);
            Properties.Settings.Default.InventoryOrder.Remove(item.Name);
            item.Destroy();
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public void ReOrderInvList()
        {
            InventoryItems = MainHelper.SortItems(InventoryItems).ToList();
            VendorInvItems = MainHelper.SortItems(VendorInvItems).ToList();
        }

        /// <summary>
        /// Adds or updates vendor in DB and model container
        /// </summary>
        /// <param name="vendor"></param>
        public void AddUpdateVendor(ref Vendor vendor, List<InventoryItem> vendorItems = null)
        {
            // remove reference of this vendor from old VendorInventoryItems
            List<InventoryItem> oldVendorItems = vendor.GetInventoryItems();
            if (oldVendorItems != null)
            {
                List<int> removedItemIds = oldVendorItems.Select(x => x.Id).Except(vendorItems.Select(x => x.Id)).ToList();
                if (removedItemIds.Count > 0)
                {
                    List<VendorInventoryItem> removedItems = removedItemIds.Select(x => VendorInvItems.First(y => y.Id == x)).ToList();
                    foreach (VendorInventoryItem item in removedItems)
                    {
                        item.DeleteVendor(vendor);
                    }
                }
            }

            int vendorId = vendor.Id;
            if(Vendors.FirstOrDefault(x => x.Id == vendorId) != null)
            {
                vendor.Update(vendorItems);
            }
            else
            {
                vendor.Id = vendor.Insert(vendorItems);
                Vendors.Add(vendor);
            }

            foreach (InventoryItem item in vendorItems)
            {
                VendorInventoryItem vInvItem = VendorInvItems.FirstOrDefault(x => x.Id == item.Id);
                if (vInvItem != null)
                {
                    vInvItem.AddVendor(vendor, item);
                }
            }
        }

        /// <summary>
        /// Deletes vendor from DB and model container
        /// </summary>
        /// <param name="vendor"></param>
        public void DeleteVendor(Vendor vendor)
        {
            foreach (VendorInventoryItem item in VendorInvItems.Where(x => x.Vendors.Contains(vendor)))
            {
                item.DeleteVendor(vendor);
            }

            Vendors.Remove(vendor);
            vendor.Destroy();
        }

        /// <summary>
        /// Gets a dictionary of vendors that offer the passed-in inventory item. The inventory item value is the vendor-specific inventory
        /// item associated with that vendor (not the one from the model container, which is passed in)
        /// </summary>
        public Dictionary<Vendor, InventoryItem> GetVendorsFromItem(InventoryItem item)
        {
            Dictionary<Vendor, InventoryItem> vendorDict = new Dictionary<Vendor, InventoryItem>();
            foreach(Vendor v in Vendors)
            {
                List<InventoryItem> items = v.GetInventoryItems();
                if (items != null)
                {
                    InventoryItem vendorItem = items.FirstOrDefault(x => x.Id == item.Id);
                    if (vendorItem != null)
                    {
                        vendorDict[v] = vendorItem;
                    }
                }
            }

            return vendorDict;
        }

        public void AddPurchaseOrder(PurchaseOrder order)
        {
            PurchaseOrders.Add(order);
        }

        public List<string> GetRecipeCategories()
        {
            HashSet<string> categories = new HashSet<string>();
            foreach (Recipe rec in Recipes)
            {
                if (!string.IsNullOrWhiteSpace(rec.Category))
                    categories.Add(rec.Category);
            }

            return categories.ToList();
        }

        public List<IItem> GetAllIItems()
        {
            return InventoryItems.Select(x => (IItem)x).Concat(Recipes).ToList();
        }

        public List<string> GetInventoryCategories()
        {
            HashSet<string> categories = new HashSet<string>();

            foreach (InventoryItem item in InventoryItems)
            {
                if(!string.IsNullOrWhiteSpace(item.Category))
                    categories.Add(item.Category);
            }

            return categories.ToList();
        }

        /// <summary>
        /// Constructs dictionary mapping categories to colors { CATEGORY : Hex Color }
        /// </summary>
        private void SetCategoryColors()
        {
            const string COLOR = "_COLOR";

            _categoryColors = new Dictionary<string, string>();

            FieldInfo[] fields = typeof(GlobalVar).GetFields().Where(x => x.Name.IndexOf(COLOR) != -1).ToArray();
            string[] fieldNames = fields.Select(x => x.Name).ToArray();
            foreach (string category in GetInventoryCategories())
            {
                int idx = Array.IndexOf(fieldNames, category.ToUpper().Replace(' ', '_') + COLOR);
                if (idx > -1)
                {
                    _categoryColors[category.ToUpper()] = (string)fields[idx].GetValue(null);
                }
            }
        }

        /// <summary>
        /// Gets the all category breakdown of the value of inventory (inventory and prep items)
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, float> GetCategoryValues()
        {
            Dictionary<string, float> costDict = new Dictionary<string, float>();
            foreach (InventoryItem item in InventoryItems)
            {
                if (!costDict.Keys.Contains(item.Category))
                    costDict[item.Category] = 0;
                costDict[item.Category] += item.PriceExtension;
            }

            foreach(KeyValuePair<string, float> kvp in GetPrepCatValues())
            {
                if (!costDict.Keys.Contains(kvp.Key))
                    costDict[kvp.Key] = 0;
                costDict[kvp.Key] += kvp.Value;
            }

            return costDict;
        }

        public Dictionary<string, float> GetPrepCatValues()
        {
            Dictionary<string, float> costDict = new Dictionary<string, float>();

            foreach (PrepItem item in PrepItems)
            {
                InventoryItem invItem = InventoryItems.FirstOrDefault(x => x.Name == item.Name);
                if (invItem != null)
                {
                    if (!costDict.Keys.Contains(invItem.Category))
                        costDict[invItem.Category] = 0;
                    costDict[invItem.Category] += item.Extension;
                    continue;
                }

                // separate prep item extension costs among the categories
                Recipe recipe = Recipes.FirstOrDefault(x => x.Name == item.Name);
                if (recipe != null)
                {
                    Dictionary<string, float> recipeCatProps = recipe.GetCatCostProportions();
                    if (recipeCatProps != null)
                    {
                        foreach (KeyValuePair<string, float> kvp in recipeCatProps)
                        {
                            if (!costDict.Keys.Contains(kvp.Key))
                                costDict[kvp.Key] = 0;
                            costDict[kvp.Key] += kvp.Value * item.Extension;
                        }
                    }
                }
            }

            return costDict;
        }

        public void SaveInvOrder()
        {
            string dir = Path.Combine(Properties.Settings.Default.DBLocation, "Settings");
            Directory.CreateDirectory(dir);
            File.WriteAllLines(Path.Combine(dir, GlobalVar.INV_ORDER_FILE), Properties.Settings.Default.InventoryOrder);
        }

        /// <summary>
        /// Converts case-sensitive hashset into case insensitive (will not store OZ-wt AND OZ-WT)
        /// </summary>
        /// <param name="set"></param>
        /// <returns></returns>
        private HashSet<string> EnsureCaseInsensitive(HashSet<string> set)
        {
            return set.Comparer == StringComparer.OrdinalIgnoreCase
                   ? set : new HashSet<string>(set, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Alters VendorInvItems property to have all same items in list
        /// </summary>
        private void SyncVendorItems()
        {
            List<int> vItemsIds = VendorInvItems.Select(x => x.Id).ToList();
            for (int i = 0; i < InventoryItems.Count; i++)
            {
                int invId = InventoryItems[i].Id;
                if(i >= VendorInvItems.Count)
                {
                    VendorInvItems.Add(new VendorInventoryItem(GetVendorsFromItem(InventoryItems[i]), InventoryItems[i]));
                    continue;
                }
                if (invId != VendorInvItems[i].Id)
                {
                    VendorInventoryItem vItem;
                    if (vItemsIds.Contains(invId))
                    {
                        vItem = VendorInvItems.First(x => x.Id == invId);
                        VendorInvItems.Remove(vItem);
                        VendorInvItems.Insert(i, vItem);
                    }
                    else
                    {
                        vItem = new VendorInventoryItem(GetVendorsFromItem(InventoryItems[i]), InventoryItems[i]);
                    }
                    VendorInvItems.Insert(i, vItem);
                }
            }
        }
    }
}
