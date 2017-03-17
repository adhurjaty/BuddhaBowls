using BuddhaBowls.Helpers;
using BuddhaBowls.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Services
{
    public class ModelContainer
    {
        //private static ModelContainer cont;
        //public static ModelContainer _container
        //{
        //    get
        //    {
        //        return cont;
        //    }
        //    set
        //    {
        //        cont = value;
        //    }
        //}
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
            }
        }
        public List<Recipe> Recipes { get; set; }
        public HashSet<string> ItemCategories { get; set; }
        public List<PurchaseOrder> PurchaseOrders { get; set; }
        public List<Vendor> Vendors { get; set; }
        public List<Inventory> Inventories { get; set; }

        public ModelContainer()
        {
            InitializeModels();
            if (InventoryItems != null)
            {
                SetInventoryCategories();
                SetCategoryColors();
            }
        }

        //public static ModelContainer Instance()
        //{
        //    if (_container == null)
        //        _container = new ModelContainer();
        //    return _container;
        //}

        //public static void ChangeContainer(ModelContainer container)
        //{
        //    _container = container;
        //}

        public void InitializeModels()
        {
            InventoryItems = ModelHelper.InstantiateList<InventoryItem>("InventoryItem") ?? new List<InventoryItem>();
            Recipes = ModelHelper.InstantiateList<Recipe>("Recipe") ?? new List<Recipe>();
            PurchaseOrders = ModelHelper.InstantiateList<PurchaseOrder>("PurchaseOrder") ?? new List<PurchaseOrder>();
            Vendors = ModelHelper.InstantiateList<Vendor>("Vendor") ?? new List<Vendor>();
            Inventories = ModelHelper.InstantiateList<Inventory>("Inventory") ?? new List<Inventory>();

            if (InventoryItems == null || Recipes == null)
                return;

            foreach(Recipe rec in Recipes)
            {
                rec.ItemList = MainHelper.GetRecipe(rec.Name, this);
            }
        }

        public float GetBatchItemCost(Recipe rec)
        {
            float cost = 0;
            foreach(IItem item in rec.ItemList)
            {
                cost += item.GetCost() * item.Count;
            }

            return cost;
        }

        public Dictionary<string, float> GetCategoryCosts(Recipe rec)
        {
            Dictionary<string, float> costDict = new Dictionary<string, float>();

            foreach(IItem item in rec.ItemList)
            {
                if(!costDict.Keys.Contains(item.Category))
                    costDict[item.Category] = 0;
                costDict[item.Category] += item.GetCost() * item.Count;
            }

            return costDict;
        }

        public long GetColorFromCategory(string category)
        {
            string key = _categoryColors.Keys.FirstOrDefault(x => x.ToUpper() == category.ToUpper());
            if (!string.IsNullOrEmpty(key))
            {
                return MainHelper.ColorFromString(_categoryColors[key]);
            }

            return MainHelper.ColorFromString(GlobalVar.BLANK_COLOR);
        }

        public string GetCategoryColorHex(string category)
        {
            return "#" + (_categoryColors.Keys.Contains(category.ToUpper()) ?
                            _categoryColors[category.ToUpper()] : GlobalVar.BLANK_COLOR);
        }

        public IEnumerable<IItem> GetIngredients()
        {
            return InventoryItems.Select(x => (IItem)x).Concat(Recipes.Where(x => x.IsBatch));
        }

        public List<string> GetCountUnits()
        {
            return (new HashSet<string>(InventoryItems.Select(x => x.CountUnit))).ToList();
        }

        public List<string> GetRecipeUnits()
        {
            return (new HashSet<string>(InventoryItems.Select(x => x.RecipeUnit))).ToList();
        }

        public List<string> GetPurchasedUnits()
        {
            return (new HashSet<string>(InventoryItems.Select(x => x.PurchasedUnit))).ToList();
        }

        public void AddUpdateInventoryItem(ref InventoryItem item)
        {
            if(InventoryItems.Select(x => x.Id).Contains(item.Id))
            {
                item.Update();
            }
            else
            {
                Properties.Settings.Default.InventoryOrder.Add(item.Name);
                Properties.Settings.Default.Save();

                item.Id = item.Insert();
                InventoryItems.Add(item);
            }
        }

        public bool DeleteInventoryItem(InventoryItem item)
        {
            if(InventoryItems.First(x => x.Id == item.Id) == null)
                return false;
            InventoryItems.RemoveAll(x => x.Id == item.Id);
            Properties.Settings.Default.InventoryOrder.Remove(item.Name);
            item.Destroy();
            return true;
        }

        public void AddUpdateVendor(Vendor vendor)
        {
            if(Vendors.FirstOrDefault(x => x.Id == vendor.Id) != null)
            {
                vendor.Update();
            }
            else
            {
                vendor.Id = vendor.Insert();
                Vendors.Add(vendor);
            }
        }

        public void RemoveVendor(Vendor vendor)
        {
            Vendors.Remove(vendor);
            vendor.Destroy();
        }

        public Dictionary<Vendor, InventoryItem> GetVendorsFromItem(InventoryItem item)
        {
            Dictionary<Vendor, InventoryItem> vendorDict = new Dictionary<Vendor, InventoryItem>();
            foreach(Vendor v in Vendors)
            {
                List<InventoryItem> items = v.GetFromPriceList();
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

        private void SetInventoryCategories()
        {
            ItemCategories = new HashSet<string>();

            foreach (InventoryItem item in InventoryItems)
            {
                // may need to add back in the .ToUpper()
                ItemCategories.Add(item.Category);
            }
        }

        private void SetCategoryColors()
        {
            const string COLOR = "_COLOR";

            _categoryColors = new Dictionary<string, string>();

            FieldInfo[] fields = typeof(GlobalVar).GetFields().Where(x => x.Name.IndexOf(COLOR) != -1).ToArray();
            string[] fieldNames = fields.Select(x => x.Name).ToArray();
            foreach (string category in ItemCategories)
            {
                int idx = Array.IndexOf(fieldNames, category.ToUpper().Replace(' ', '_') + COLOR);
                if (idx > -1)
                {
                    _categoryColors[category.ToUpper()] = (string)fields[idx].GetValue(null);
                }
            }
        }
    }
}
