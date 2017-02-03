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
        private Dictionary<string, string> _categoryColors;

        public List<InventoryItem> InventoryItems { get; set; }
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

        public void InitializeModels()
        {
            InventoryItems = ModelHelper.InstantiateList<InventoryItem>("InventoryItem");
            Recipes = ModelHelper.InstantiateList<Recipe>("Recipe");
            PurchaseOrders = ModelHelper.InstantiateList<PurchaseOrder>("PurchaseOrder");
            Vendors = ModelHelper.InstantiateList<Vendor>("Vendor");
            Inventories = ModelHelper.InstantiateList<Inventory>("Inventory");

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
            if(_categoryColors.Keys.Contains(category.ToUpper()))
            {
                return MainHelper.ColorFromString(_categoryColors[category.ToUpper()]);
            }

            return MainHelper.ColorFromString(GlobalVar.BLANK_COLOR);
        }

        public string GetCategoryColorHex(string category)
        {
            return "#" + (_categoryColors.Keys.Contains(category.ToUpper()) ?
                            _categoryColors[category.ToUpper()] : GlobalVar.BLANK_COLOR);
        }

        private void SetInventoryCategories()
        {
            ItemCategories = new HashSet<string>();

            foreach (InventoryItem item in InventoryItems)
            {
                ItemCategories.Add(item.Category.ToUpper());
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
                int idx = Array.IndexOf(fieldNames, category.Replace(' ', '_') + COLOR);
                if (idx > -1)
                {
                    _categoryColors[category.ToUpper()] = (string)fields[idx].GetValue(null);
                }
            }
        }
    }
}
