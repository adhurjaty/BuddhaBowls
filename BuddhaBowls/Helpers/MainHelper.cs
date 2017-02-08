using BuddhaBowls.Models;
using BuddhaBowls.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Helpers
{
    public static class MainHelper
    {

        public static string ListToCsv(List<string[]> rows)
        {
            return string.Join("\n", rows.Select(x => string.Join(",", x)));
        }

        public static long ColorFromString(string color)
        {
            int[] rgb = ChunkString(color, 2).Select(x => int.Parse(x, System.Globalization.NumberStyles.HexNumber)).ToArray();
            int r = rgb[0];
            int g = rgb[1];
            int b = rgb[2];

            return (long)(Math.Pow(2, 16) * b + Math.Pow(2, 8) * g + r);
        }

        public static IEnumerable<string> ChunkString(string str, int size)
        {
            return Enumerable.Range(0, str.Length / size).Select(i => str.Substring(i * size, size));
        }

        public static List<IItem> GetRecipe(string recipeName, ModelContainer models)
        {
            string tableName = Path.Combine(Properties.Resources.RecipeFolder, recipeName);

            List<RecipeItem> items = ModelHelper.InstantiateList<RecipeItem>(tableName, false);

            List<IItem> recipeList = new List<IItem>();
            foreach(RecipeItem item in items)
            {
                IItem addItem;
                if (item.InventoryItemId == null)
                {
                    addItem = models.Recipes.First(x => x.Name == item.Name);
                    ((Recipe)addItem).ItemList = GetRecipe(addItem.Name, models);
                }
                else
                {
                    addItem = models.InventoryItems.First(x => x.Id == item.InventoryItemId);
                }

                addItem.Count = item.Quantity;
                recipeList.Add(addItem);
            }

            return recipeList;
        }

        //public static List<VendorItem> GetVendorPrices(string vendorName)
        //{
        //    string tableName = Path.Combine(Properties.Resources.VendorFolder, vendorName);

        //    return ModelHelper.InstantiateList<VendorItem>(tableName, false);
        //}
    }
}
