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
        public List<BatchItem> BatchItems { get; set; }
        public HashSet<string> ItemCategories { get; set; }

        public ModelContainer()
        {
            InitializeModels();
            SetInventoryCategories();
            SetCategoryColors();
        }

        public void InitializeModels()
        {
            InventoryItems = ModelHelper.InstantiateList<InventoryItem>("InventoryItem");
            BatchItems = ModelHelper.InstantiateList<BatchItem>("BatchItem");

            if (InventoryItems == null || BatchItems == null)
                return;

            ClearCountUpdated();

            foreach(BatchItem bi in BatchItems)
            {
                bi.recipe = MainHelper.GetRecipe(bi.Name);
            }
        }

        public float GetBatchItemCost(BatchItem item)
        {
            float cost = 0;
            foreach(RecipeItem ri in item.recipe)
            {
                InventoryItem invItem = InventoryItems[(int)ri.InventoryItemId];
                cost += invItem.GetCost() * ri.Quantity;
            }

            return cost;
        }

        public Dictionary<string, float> GetCategoryCosts(BatchItem item)
        {
            Dictionary<string, float> costDict = new Dictionary<string, float>();

            foreach(RecipeItem ri in item.recipe)
            {
                InventoryItem invItem = InventoryItems[(int)ri.InventoryItemId];
                if(!costDict.Keys.Contains(invItem.Category))
                    costDict[invItem.Category] = 0;
                costDict[invItem.Category] += invItem.GetCost() * ri.Quantity;
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

        private void ClearCountUpdated()
        {
            foreach(InventoryItem item in InventoryItems)
            {
                item.countUpdated = false;
            }
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
