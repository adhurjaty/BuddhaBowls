﻿using BuddhaBowls.Models;
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
        public static IEnumerable<T> SortItems<T>(IEnumerable<T> items) where T : ISortable
        {
            if (Properties.Settings.Default.InventoryOrder == null)
                return items.OrderBy(x => x.Name);
            return items.OrderBy(x => SortValue(x, Properties.Settings.Default.InventoryOrder)).ThenBy(x => x.Name);
        }

        public static IEnumerable<T> SortItems<T>(IEnumerable<T> items, List<string> itemOrder) where T : IItem
        {
            if (itemOrder == null)
                return items.OrderBy(x => x.Name);
            return items.OrderBy(x => SortValue(x, itemOrder))
                        .ThenBy(x => SortValue(x, Properties.Settings.Default.InventoryOrder))
                        .ThenBy(x => x.Name);
        }

        public static List<PrepItem> SortPrepItems(IEnumerable<PrepItem> items)
        {
            return items.OrderBy(x => SortValue(x, Properties.Settings.Default.PrepItemOrder)).ThenBy(x => x.Name).ToList();
        }

        private static int SortValue<T>(T item, List<string> itemOrder) where T : ISortable
        {
            int value = itemOrder.IndexOf(item.Name);
            if (value != -1)
                return value;
            return 1000;
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
        public static ObservableCollection<T> FilterInventoryItems<T>(string filterStr, IEnumerable<T> items) where T : ISortable
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

            if (idx > 0 && up)
            {
                orderedList.RemoveAt(idx);
                orderedList.Insert(idx - 1, item);
            }
            if (idx < orderedList.Count - 1 && !up)
            {
                orderedList.RemoveAt(idx);
                orderedList.Insert(idx + 1, item);
            }

            return orderedList;
        }

        public static int Mod(int x, int m)
        {
            return (x % m + m) % m;
        }

        public static IEnumerable<WeekMarker> GetWeekLabels(PeriodMarker period)
        {
            return GetWeekLabels(period.Period, period.StartDate.Year);
        }

        public static IEnumerable<WeekMarker> GetWeekLabels(int period, int year)
        {
            DateTime theFirst = new DateTime(year, 1, 1);
            DateTime firstMonday = theFirst.AddDays(Mod(8 - (int)theFirst.DayOfWeek, 7));

            if (period == 0 && firstMonday != theFirst)
            {
                yield return new WeekMarker(theFirst, 0, (int)firstMonday.Subtract(theFirst).TotalDays);
            }
            else
            {
                DateTime startDate = firstMonday.AddDays(28 * (period - 1));
                for (int i = 0; i < 4; i++)
                {
                    yield return new WeekMarker(startDate.AddDays(7 * i), (i + 1));
                }
            }
        }

        public static IEnumerable<WeekMarker> GetWeeksInPeriod(PeriodMarker period)
        {
            if (period.GetType() == typeof(WeekMarker))
                return new List<WeekMarker>() { (WeekMarker)period };
            else
                return GetWeekLabels(period);
        }

        public static IEnumerable<PeriodMarker> GetPeriodLabels(int year)
        {
            DateTime theFirst = new DateTime(year, 1, 1);
            int startDayDiff = Mod(8 - (int)theFirst.DayOfWeek, 7);
            DateTime firstMonday = theFirst.AddDays(startDayDiff);

            if(startDayDiff == 0)
            {
                for (int i = 0; i < 13; i++)
                {
                    yield return new PeriodMarker(theFirst.AddDays(28 * i), i + 1);
                }
            }
            else
            {
                for (int i = 0; i < 14; i++)
                {
                    if(i == 0)
                    {
                        yield return new PeriodMarker(theFirst, i, startDayDiff);
                    }
                    else
                    {
                        yield return new PeriodMarker(firstMonday.AddDays(28 * (i - 1)), i);
                    }
                }
            }
        }

        public static WeekMarker GetThisWeek()
        {
            List<PeriodMarker> periods = GetPeriodLabels(DateTime.Today.Year).ToList();
            if (periods != null && periods.Count > 0)
            {
                return GetWeekLabels(periods.First(x => x.StartDate <= DateTime.Now && DateTime.Now <= x.EndDate))
                                            .First(x => x.StartDate <= DateTime.Now && DateTime.Now <= x.EndDate);
            }
            return null;
        }

        public static WeekMarker GetWeek(DateTime date)
        {
            List<PeriodMarker> periods = GetPeriodLabels(date.Year).ToList();
            if (periods != null && periods.Count > 0)
            {
                return GetWeekLabels(periods.First(x => x.StartDate <= date && date <= x.EndDate))
                                            .First(x => x.StartDate <= date && date <= x.EndDate);
            }
            return null;
        }

        /// <summary>
        /// Converts case-sensitive hashset into case insensitive (will not store OZ-wt AND OZ-WT)
        /// </summary>
        /// <param name="set"></param>
        /// <returns></returns>
        public static HashSet<string> EnsureCaseInsensitive(HashSet<string> set)
        {
            return set.Comparer == StringComparer.OrdinalIgnoreCase
                   ? set : new HashSet<string>(set, StringComparer.OrdinalIgnoreCase);
        }

        public static bool CompareStrings(string a, string b)
        {
            return a.ToUpper().Replace(" ", "") == b.ToUpper().Replace(" ", "");
        }

        public static Dictionary<T, V> MergeDicts<T, V>(Dictionary<T, V> dict1, Dictionary<T, V> dict2, Func<V, V, V> onDuplicate)
        {
            foreach (T key in dict2.Keys)
            {
                if (dict1.ContainsKey(key))
                    dict1[key] = onDuplicate(dict1[key], dict2[key]);
                else
                    dict1[key] = dict2[key];
            }

            return dict1;
        }

        public static void AddToDict<T, V>(ref Dictionary<T, V> dict, T key, V val, Func<V, V, V> onDuplicate)
        {
            if (dict.ContainsKey(key))
                dict[key] = onDuplicate(dict[key], val);
            else
                dict[key] = val;
        }
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
