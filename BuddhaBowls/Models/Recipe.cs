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

        public List<IItem> ItemList;

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
                return ItemList.Sum(x => x.GetCost() * x.Count);
            }
            catch(Exception e)
            {
                return 0;
            }
        }

        public override void Update()
        {
            ModelHelper.CreateTable(GetInRecipeItems(), GetRecipeTableName());
            base.Update();
        }

        public override int Insert()
        {
            if(ItemList == null)
            {
                throw new Exception("Must set ItemsList to insert");
            }
            ModelHelper.CreateTable(GetInRecipeItems(), GetRecipeTableName());
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

        private List<RecipeItem> GetInRecipeItems()
        {
            List<RecipeItem> items = new List<RecipeItem>();
            int i = 0;
            foreach(IItem ingredient in ItemList)
            {
                items.Add(new RecipeItem() { Id = i, InventoryItemId = ingredient.Id, Name = ingredient.Name, Quantity = ingredient.Count });
                i++;
            }

            return items;
        }

        private string GetRecipeTableName()
        {
            return @"Recipes\" + Name;
        }

    }
}
