using BuddhaBowls.Helpers;
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

        public List<InventoryItem> InventoryItems { get; set; }
        public List<Recipe> Recipes { get; set; }
        public List<PurchaseOrder> PurchaseOrders { get; set; }
        public List<Vendor> Vendors { get; set; }
        public List<Inventory> Inventories { get; set; }
        public List<PrepItem> PrepItems { get; set; }

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
            InventoryItems = ModelHelper.InstantiateList<InventoryItem>("InventoryItem") ?? new List<InventoryItem>();
            Recipes = ModelHelper.InstantiateList<Recipe>("Recipe") ?? new List<Recipe>();
            PurchaseOrders = ModelHelper.InstantiateList<PurchaseOrder>("PurchaseOrder") ?? new List<PurchaseOrder>();
            Vendors = ModelHelper.InstantiateList<Vendor>("Vendor") ?? new List<Vendor>();
            Inventories = ModelHelper.InstantiateList<Inventory>("Inventory") ?? new List<Inventory>();
            PrepItems = ModelHelper.InstantiateList<PrepItem>("PrepItem") ?? new List<PrepItem>();

            if (InventoryItems == null || Recipes == null)
                return;
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
            }
            else
            {
                item.Id = item.Insert();
                InventoryItems.Add(item);
            }
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
            Properties.Settings.Default.InventoryOrder.Remove(item.Name);
            item.Destroy();
            return true;
        }

        /// <summary>
        /// Adds or updates vendor in DB and model container
        /// </summary>
        /// <param name="vendor"></param>
        public void AddUpdateVendor(ref Vendor vendor, List<InventoryItem> vendorItems = null)
        {
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
        }

        /// <summary>
        /// Deletes vendor from DB and model container
        /// </summary>
        /// <param name="vendor"></param>
        public void DeleteVendor(Vendor vendor)
        {
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
        /// Converts case-sensitive hashset into case insensitive (will not store OZ-wt AND OZ-WT)
        /// </summary>
        /// <param name="set"></param>
        /// <returns></returns>
        private HashSet<string> EnsureCaseInsensitive(HashSet<string> set)
        {
            return set.Comparer == StringComparer.OrdinalIgnoreCase
                   ? set
                   : new HashSet<string>(set, StringComparer.OrdinalIgnoreCase);
        }

    }
}
