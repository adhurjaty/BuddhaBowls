using BuddhaBowls.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Models
{
    public class Recipe : Model, IItem
    {
        public string Name { get; set; }
        public string RecipeUnit { get; set; }
        public float? RecipeUnitConversion { get; set; }
        public string Category { get; set; }
        public float Count { get; set; }
        public bool IsBatch { get; set; }

        public float RecipeCost
        {
            get
            {
                try
                {
                    return CostPerRU * Count;
                }
                catch (Exception e)
                {
                    return 0;
                }
            }
        }

        public float CostPerRU
        {
            get
            {
                if (RecipeUnitConversion == null || RecipeUnitConversion == 0)
                    return 0;
                return (float)(TotalCost / RecipeUnitConversion);
            }
        }

        public float TotalCost
        {
            get
            {
                return GetItems().Sum(x => x.RecipeCost);
            }
        }

        //public List<IItem> ItemList;

        public Recipe() : base()
        {
            _tableName = "Recipe";
        }

        public Recipe(Dictionary<string, string> searchParams) : this()
        {
            string[] record = _dbInt.GetRecord(_tableName, searchParams);

            if (record != null)
            {
                InitializeObject(record);
                GetRecipeItems();
            }
        }

        public float GetCost()
        {
            return RecipeCost * Count;
        }

        public Dictionary<string, float> GetCategoryCosts()
        {
            Dictionary<string, float> costDict = new Dictionary<string, float>();

            foreach (IItem item in GetItems())
            {
                if (item.GetType() == typeof(Recipe))
                {
                    Dictionary<string, float> subCostDict = ((Recipe)item).GetCategoryCosts();
                    foreach (KeyValuePair<string, float> kvp in subCostDict)
                    {
                        if (!costDict.Keys.Contains(kvp.Key))
                            costDict[kvp.Key] = 0;
                        costDict[kvp.Key] += kvp.Value;
                    }
                }
                else
                {
                    if (!costDict.Keys.Contains(item.Category))
                        costDict[item.Category] = 0;
                    costDict[item.Category] += item.GetCost();
                }
            }

            return costDict;
        }

        public void Update(List<RecipeItem> items)
        {
            if (items != null && items.Count > 0)
                ModelHelper.CreateTable(items, GetRecipeTableName());
            base.Update();
        }

        public int Insert(List<RecipeItem> items)
        {
            if (items != null && items.Count > 0)
                ModelHelper.CreateTable(items, GetRecipeTableName());
            return base.Insert();
        }

        public override void Destroy()
        {
            _dbInt.DestroyTable(GetRecipeTableName());
            base.Destroy();
        }

        public override string[] GetPropertiesDB(string[] omit = null)
        {
            string[] theseOmissions = new string[] { "RecipeCost" };
            return base.GetPropertiesDB(ModelHelper.CombineArrays(omit, theseOmissions));
        }

        public IItem Copy()
        {
            return Copy<Recipe>();
        }

        public List<RecipeItem> GetRecipeItems()
        {
            return ModelHelper.InstantiateList<RecipeItem>(GetRecipeTableName(), false);
        }

        public List<IItem> GetItems()
        {
            List<RecipeItem> items = GetRecipeItems();
            if (items != null)
                return items.Select(x => x.GetIItem()).ToList();
            return null;
        }

        private List<RecipeItem> ConvToRecipeItems(List<IItem> items)
        {
            List<RecipeItem> recItems = new List<RecipeItem>();
            int i = 0;
            foreach(IItem ingredient in items)
            {
                int? invId = null;
                if (ingredient.GetType() == typeof(InventoryItem))
                    invId = ingredient.Id;
                recItems.Add(new RecipeItem() { Id = i, InventoryItemId = invId, Name = ingredient.Name, Quantity = ingredient.Count });
                i++;
            }

            return recItems;
        }

        private string GetRecipeTableName()
        {
            return @"Recipes\" + Name;
        }

        public string GetRecipeTablePath()
        {
            return _dbInt.FilePath(GetRecipeTableName());
        }

        public Dictionary<string, float> GetCatCostProportions()
        {
            Dictionary<string, float> propDict = GetCategoryCosts();
            float totalCost = GetCost();

            if (totalCost == 0)
                return null;

            foreach (KeyValuePair<string, float> kvp in propDict)
            {
                propDict[kvp.Key] /= totalCost;
            }

            return propDict;
        }
    }
}
