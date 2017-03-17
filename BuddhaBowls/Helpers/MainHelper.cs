using BuddhaBowls.Models;
using BuddhaBowls.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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
                    addItem = models.Recipes.FirstOrDefault(x => x.Name == item.Name);
                    if(addItem != null)
                        ((Recipe)addItem).ItemList = GetRecipe(addItem.Name, models);
                }
                else
                {
                    addItem = models.InventoryItems.FirstOrDefault(x => x.Id == item.InventoryItemId);
                }

                if (addItem != null)
                {
                    addItem = addItem.Copy();
                    addItem.Count = item.Quantity;
                    recipeList.Add(addItem);
                }
            }

            return recipeList;
        }

        public static IEnumerable<T> SortItems<T>(IEnumerable<T> items) where T : IItem
        {
            if (Properties.Settings.Default.InventoryOrder == null)
                return items.OrderBy(x => x.Name);

            return items.Where(x => Properties.Settings.Default.InventoryOrder.Contains(x.Name))
                        .OrderBy(x => Properties.Settings.Default.InventoryOrder.IndexOf(x.Name))
                        .Concat(items.Where(x => !Properties.Settings.Default.InventoryOrder.Contains(x.Name))
                                     .OrderBy(x => x.Name));
        }

        public static List<IItem> CategoryOrder(List<IItem> items)
        {
            return items.OrderBy(x => OrderValue(x)).ToList();
        }

        public static IEnumerable<IGrouping<string, T>> CategoryGrouping<T>(List<T> items) where T : IItem
        {
            return SortItems(items).GroupBy(x => x.Category).OrderBy(x => x.Key);
        }

        private static int OrderValue(IItem item)
        {
            return Properties.Settings.Default.CategoryOrder.IndexOf(item.Category) * 1000 +
                    Properties.Settings.Default.InventoryOrder.IndexOf(item.Name);
        }

        //public static List<VendorItem> GetVendorPrices(string vendorName)
        //{
        //    string tableName = Path.Combine(Properties.Resources.VendorFolder, vendorName);

        //    return ModelHelper.InstantiateList<VendorItem>(tableName, false);
        //}
    }

    public class BindingProxy : Freezable
    {
        public static readonly DependencyProperty DataProperty = DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy),
                                                                                             new UIPropertyMetadata(null));
        public object Data
        {
            get
            {
                return GetValue(DataProperty);
            }
            set
            {
                SetValue(DataProperty, value);
            }
        }
        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }
    }
}
