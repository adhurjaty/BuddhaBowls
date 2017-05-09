using BuddhaBowls.Models;
using BuddhaBowls.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BuddhaBowls.Helpers
{
    public static class MainHelper
    {
        /// <summary>
        /// Converts a list of string arrays into a csv string
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        public static string ListToCsv(List<string[]> rows)
        {
            string contents = "";
            foreach (string[] row in rows)
            {
                string cell = row.FirstOrDefault(x => x.Contains(","));
                if (!string.IsNullOrEmpty(cell))
                    throw new ArgumentException("Contents cannot have a , in them");
                contents += string.Join(",", row) + "\n";
            }

            return contents.TrimEnd('\n');
        }

        /// <summary>
        /// Convert a hex string for a color into the excel format color long format
        /// </summary>
        /// <remarks>I don't think I need to test this one - it works and there is no need to change it</remarks>
        /// <param name="color">Hex code for color</param>
        public static long ColorFromString(string color)
        {
            int[] rgb = ChunkString(color, 2).Select(x => int.Parse(x, System.Globalization.NumberStyles.HexNumber)).ToArray();
            int r = rgb[0];
            int g = rgb[1];
            int b = rgb[2];

            return (long)(Math.Pow(2, 16) * b + Math.Pow(2, 8) * g + r);
        }

        /// <summary>
        /// Breaks a string up into |size| sized chunks, the last element cannot have a smaller size - so it is omitted
        /// </summary>
        /// <remarks>Only used for ColorFromString</remarks>
        /// <returns></returns>
        private static IEnumerable<string> ChunkString(string str, int size)
        {
            return Enumerable.Range(0, str.Length / size).Select(i => str.Substring(i * size, size));
        }

        /// <summary>
        /// Sorts inventory items based on the stored order if it exists, otherwise sort alphabetically by name
        /// </summary>
        /// <typeparam name="T">Return type in IEnumerable - must be an IItem</typeparam>
        /// <param name="items">Items to sort</param>
        /// <returns>Sorted IEnumerable</returns>
        public static IEnumerable<T> SortItems<T>(IEnumerable<T> items) where T : IItem
        {
            if (Properties.Settings.Default.InventoryOrder == null)
                return items.OrderBy(x => x.Name);
            return items.Where(x => Properties.Settings.Default.InventoryOrder.Contains(x.Name))
                        .OrderBy(x => Properties.Settings.Default.InventoryOrder.IndexOf(x.Name))
                        .Concat(items.Where(x => !Properties.Settings.Default.InventoryOrder.Contains(x.Name))
                                     .OrderBy(x => x.Name));
        }

        /// <summary>
        /// Unused for now - not sure if it is needed in the future
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static List<IItem> CategoryOrder(List<IItem> items)
        {
            return items.OrderBy(x => OrderValue(x)).ToList();
        }

        /// <summary>
        /// Create a grouping by category of items that are in the order of InventoryOrder within their category group
        /// </summary>
        /// <typeparam name="T">IItem type</typeparam>
        public static IEnumerable<IGrouping<string, T>> CategoryGrouping<T>(List<T> items) where T : IItem
        {
            return SortItems(items).GroupBy(x => x.Category).OrderBy(x => Properties.Settings.Default.InventoryOrder.IndexOf(x.First().Name));
        }

        /// <summary>
        /// Helper for CategoryGrouping - orders first by category then by InventoryOrder
        /// </summary>
        /// <returns>Int representation corresponding with the order</returns>
        private static int OrderValue(IItem item)
        {
            return Properties.Settings.Default.CategoryOrder.IndexOf(item.Category) * 1000 +
                    Properties.Settings.Default.InventoryOrder.IndexOf(item.Name);
        }

        /// <summary>
        /// Filter list of inventory items based on the string in the filter box above datagrids
        /// </summary>
        /// <param name="filterStr"></param>
        public static ObservableCollection<T> FilterInventoryItems<T>(string filterStr, IEnumerable<T> items) where T : IItem
        {
            if (string.IsNullOrWhiteSpace(filterStr))
                return new ObservableCollection<T>(SortItems(items));
            else
                return new ObservableCollection<T>(items.Where(x => x.Name.ToUpper().Contains(filterStr.ToUpper()))
                                                        .OrderBy(x => x.Name.ToUpper().IndexOf(filterStr.ToUpper())));
        }

        public static List<T> MoveInList<T>(T item, bool up, List<T> orderedList)
        {
            int idx = orderedList.IndexOf(item);
            orderedList.RemoveAt(idx);

            if (idx > 0 && up)
                orderedList.Insert(idx - 1, item);
            if (idx < orderedList.Count - 1 && !up)
                orderedList.Insert(idx + 1, item);

            return orderedList;
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
