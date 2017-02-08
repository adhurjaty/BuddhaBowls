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
        public string Measure { get; set; }
        public float Count { get; set; }
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

        public float GetCost()
        {
            return ItemList.Sum(x => x.GetCost() * x.Count);
        }

        public void Update(string recipeName)
        {
            _tableName = Path.Combine(Properties.Resources.RecipeFolder, recipeName);
            Update();
        }

        public override string[] GetPropertiesDB(string[] omit = null)
        {
            string[] theseOmissions = new string[] { "RecipeCost" };
            return base.GetPropertiesDB(ModelHelper.CombineArrays(omit, theseOmissions));
        }
    }
}
