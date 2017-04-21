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
        public string CountUnit { get; set; }
        public float? Price { get; set; }
        public bool IsBatch { get; set; }

        public float RecipeCost
        {
            get
            {
                return GetCost();
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

            }
        }

        public float GetCost()
        {
            try
            {
                return GetIItems().Sum(x => x.GetCost());
            }
            catch(Exception e)
            {
                return 0;
            }
        }

        public Dictionary<string, float> GetCategoryCosts()
        {
            Dictionary<string, float> costDict = new Dictionary<string, float>();

            foreach (IItem item in GetIItems())
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

        public void Update(List<IItem> items)
        {
            ModelHelper.CreateTable(ConvToRecipeItems(items), GetRecipeTableName());
            base.Update();
        }

        public int Insert(List<IItem> items)
        {
            ModelHelper.CreateTable(ConvToRecipeItems(items), GetRecipeTableName());
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

        public List<IItem> GetIItems()
        {
            return GetRecipeItems().Select(x => x.GetIItem()).ToList();
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
    }
}
