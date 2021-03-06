﻿using BuddhaBowls.Helpers;
using BuddhaBowls.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Services
{
    /// <summary>
    /// Class to store database values in memory
    /// </summary>
    public class DBCache
    {
        private Dictionary<string, string> _categoryColors;

        // Cache of bread info by week. Key is the start date of the week
        private Dictionary<DateTime, BreadWeekContainer> _breadWeekDict;

        public DailySalesContainer DSContainer { get; set; }
        public VendorInvItemsContainer VIContainer { get; private set; }
        public PurchaseOrdersContainer POContainer { get; private set; }
        public VendorsContainer VContainer { get; private set; }
        public InventoriesContainer InContainer { get; private set; }
        public RecipesContainer RContainer { get; private set; }
        public ExpenseItemsContainer EIContainer { get; private set; }
        public PrepItemsContainer PIContainer { get; set; }

        /// <summary>
        /// Constructor. Initialize containers, settings and ordering information
        /// </summary>
        public DBCache()
        {
            InitializeModels();
            InitializeItemsOrder();
            InitializeFoodCategories();
            SetCategoryColors();
        }

        /// <summary>
        /// Loads containers from database
        /// </summary>
        private void InitializeModels()
        {
            Logger.Info("Loading models");

            InventoryItemsContainer invItems = new InventoryItemsContainer(
                                                    MainHelper.SortItems(ModelHelper.InstantiateList<InventoryItem>("InventoryItem")).ToList(),
                                                    true);

            VContainer = new VendorsContainer(ModelHelper.InstantiateList<Vendor>("Vendor"), true);
            VIContainer = new VendorInvItemsContainer(invItems, VContainer, true);
            POContainer = new PurchaseOrdersContainer(ModelHelper.InstantiateList<PurchaseOrder>("PurchaseOrder"), VIContainer, true);
            InContainer = new InventoriesContainer(ModelHelper.InstantiateList<Inventory>("Inventory"), true);
            RContainer = new RecipesContainer(ModelHelper.InstantiateList<Recipe>("Recipe"), VIContainer, true);
            PIContainer = new PrepItemsContainer(ModelHelper.InstantiateList<PrepItem>("PrepItem"), VIContainer, RContainer, true);
            EIContainer = new ExpenseItemsContainer(ModelHelper.InstantiateList<ExpenseItem>("ExpenseItem"), true);
            DSContainer = new DailySalesContainer(ModelHelper.InstantiateList<DailySale>("DailySale"));
            //AddRecipeItems();

            _breadWeekDict = new Dictionary<DateTime, BreadWeekContainer>();
            InitBreadOrders();

            Logger.Info("Models loaded");
        }

        /// <summary>
        /// Initializes the recipe info in each recipe. Should change as I edit the recipe tab
        /// </summary>
        //private void AddRecipeItems()
        //{
        //    foreach (Recipe item in RContainer.Items)
        //    {
        //        item.GetRecipeItems();
        //    }
        //}

        /// <summary>
        /// Sets the desired ordering for inventory items by the inventory order file. Alphabetical otherwise
        /// </summary>
        private void InitializeItemsOrder()
        {
            string orderPath = Path.Combine(Properties.Settings.Default.DBLocation, "Settings", GlobalVar.INV_ORDER_FILE);
            if (File.Exists(orderPath))
                Properties.Settings.Default.InventoryOrder = new List<string>(File.ReadAllLines(orderPath));
            else
                Properties.Settings.Default.InventoryOrder = new List<string>(VIContainer.Items.Select(x => x.Name).OrderBy(x => x));
            orderPath = Path.Combine(Properties.Settings.Default.DBLocation, "Settings", GlobalVar.PREP_ORDER_FILE);
            if (File.Exists(orderPath))
                Properties.Settings.Default.PrepItemOrder = new List<string>(File.ReadAllLines(orderPath));
            else
                Properties.Settings.Default.PrepItemOrder = new List<string>(PIContainer.Items.Select(x => x.Name).OrderBy(x => x));
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Sets the setting for which categories are food categories
        /// </summary>
        private void InitializeFoodCategories()
        {
            string filepath = Path.Combine(Properties.Settings.Default.DBLocation, "Settings", GlobalVar.FOOD_CAT_FILE);
            if (File.Exists(filepath))
                Properties.Settings.Default.FoodCategories = new List<string>(File.ReadAllLines(filepath));
            else
            {
                Properties.Settings.Default.FoodCategories = new List<string>()
                {
                    "Bread",
                    "Dairy",
                    "Grocery",
                    "Herbs",
                    "Produce",
                    "Poultry",
                    "Meats"
                };
            }
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Gets the color (excel format) from the passed-in category (case insensitive)
        /// </summary>
        /// <remarks>Probably no need to test</remarks>
        public long GetColorFromCategory(string category)
        {
            string key = _categoryColors.Keys.FirstOrDefault(x => x.ToUpper() == category.ToUpper());
            if (!string.IsNullOrEmpty(key))
            {
                return MainHelper.ColorFromString(_categoryColors[key]);
            }

            return MainHelper.ColorFromString(GlobalVar.BLANK_COLOR);
        }

        /// <summary>
        /// Gets the hex representation of the color given the passed-in category (case insensitive)
        /// </summary>
        /// <remarks>Probably no need to test</remarks>
        public string GetCategoryColorHex(string category)
        {
            return "#" + (_categoryColors.Keys.Contains(category.ToUpper()) ?
                            _categoryColors[category.ToUpper()] : GlobalVar.BLANK_COLOR);
        }

        /// <summary>
        /// Method used to put categories in the inventory order
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public int SortCategory(string category)
        {
            List<string> categories = GetInventoryCategories();
            int position = categories.IndexOf(category);
            if (position == -1)
                return categories.Count;
            return position;
        }

        /// <summary>
        /// Get all of the count units that currently exist in inventory items
        /// </summary>
        /// <returns></returns>
        public List<string> GetCountUnits()
        {
            return MainHelper.EnsureCaseInsensitive(new HashSet<string>(VIContainer.Items.Select(x => x.CountUnit).Concat(PIContainer.Items.Select(x => x.CountUnit))
                                                                           .Where(x => !string.IsNullOrEmpty(x)))).ToList();
        }

        // may bring back prep concept. Do not remove yet
        //public List<string> GetPrepCountUnits()
        //{
        //    return EnsureCaseInsensitive(new HashSet<string>(PrepItems.Select(x => x.CountUnit)
        //                                                                   .Where(x => !string.IsNullOrEmpty(x)))).ToList();
        //}

        /// <summary>
        /// Get all of the recipe units that currently exist in inventory items
        /// </summary>
        /// <returns></returns>
        public List<string> GetRecipeUnits()
        {
            return MainHelper.EnsureCaseInsensitive(new HashSet<string>(GetAllIItems().Select(x => x.RecipeUnit)
                                                                            .Where(x => !string.IsNullOrEmpty(x)))).ToList();
        }

        /// <summary>
        /// Get all of the purchased units that currently exist in inventory items
        /// </summary>
        /// <returns></returns>
        public List<string> GetPurchasedUnits()
        {
            return MainHelper.EnsureCaseInsensitive(new HashSet<string>(VIContainer.Items.Select(x => x.PurchasedUnit)
                                                                           .Where(x => !string.IsNullOrEmpty(x)))).ToList();
        }

        /// <summary>
        /// Loads the inventory items as a container into the inventory and returns the inventory object
        /// </summary>
        /// <param name="inv"></param>
        /// <returns></returns>
        public void LoadInvContainer(Inventory inv)
        {
            List<InventoryItem> items = MainHelper.SortItems(inv.GetInvItems()).ToList();
            inv.SetInvItemsContainer(new InventoryItemsContainer(items));
        }

        /// <summary>
        /// Gets all IItems (Inventory items and batch recipe items)
        /// </summary>
        /// <returns></returns>
        public List<IItem> GetAllIItems()
        {
            //return VIContainer.Items.Select(x => (IItem)x.ToInventoryItem()).Concat(RContainer.Items.Where(x => x.IsBatch)).ToList();
            return VIContainer.Items.Select(x => (IItem)x).Concat(RContainer.Items.Where(x => x.IsBatch)).ToList();
        }

        /// <summary>
        /// Gets a list of the inventory categories
        /// </summary>
        /// <returns></returns>
        public List<string> GetInventoryCategories()
        {
            return VIContainer.Items.Select(x => x.Category).Distinct().ToList();
        }

        /// <summary>
        /// Constructs dictionary mapping categories to colors { CATEGORY : Hex Color }
        /// </summary>
        private void SetCategoryColors()
        {
            const string COLOR = "_COLOR";

            _categoryColors = new Dictionary<string, string>();

            FieldInfo[] fields = typeof(GlobalVar).GetFields().Where(x => x.Name.IndexOf(COLOR) != -1).ToArray();
            string[] fieldNames = fields.Select(x => x.Name).ToArray();
            foreach (string category in GetInventoryCategories())
            {
                int idx = Array.IndexOf(fieldNames, category.ToUpper().Replace(' ', '_') + COLOR);
                if (idx > -1)
                {
                    _categoryColors[category.ToUpper()] = (string)fields[idx].GetValue(null);
                }
            }
        }

        /// <summary>
        /// Initializes bread order container dictionary
        /// </summary>
        /// <returns></returns>
        public void InitBreadOrders()
        {
            List<BreadOrder> breadOrders = ModelHelper.InstantiateList<BreadOrder>("BreadOrder");
            BreadOrder[] breadWeek = new BreadOrder[8];
            Dictionary<string, float> parFactors = GetParFactors(breadOrders);
            int day = 0;
            DateTime weekStartDate = DateTime.Today;
            DateTime thisWeekStartDate = MainHelper.GetWeek(DateTime.Today).StartDate;

            foreach (BreadOrder bo in breadOrders)
            {
                if(breadWeek[0] == null)
                {
                    weekStartDate = MainHelper.GetWeek(bo.Date).StartDate;
                    if (bo.Date != weekStartDate)
                        continue;
                    breadWeek[day] = bo;
                }
                else
                {
                    breadWeek[day] = bo;
                    breadWeek[day - 1].NextBreadOrder = bo;
                }
                foreach (string breadType in parFactors.Keys)
                {
                    BreadDescriptor breadDesc = breadWeek[day].GetBreadDescriptor(breadType);
                    breadDesc.ParFactor = parFactors[breadType];
                }
                // Add the total column and push the week into _breadWeekDict
                if (breadWeek[6] != null)
                {
                    BreadOrder[] tempBreadWeek = new BreadOrder[7];
                    Array.Copy(breadWeek, tempBreadWeek, 7);
                    breadWeek[7] = new BreadOrderTotal(ref tempBreadWeek);
                    
                    _breadWeekDict[weekStartDate] = new BreadWeekContainer(breadWeek.ToList(), VIContainer.Items.Where(x => x.Category == "Bread").ToList());
                    day = 0;
                    Array.Clear(breadWeek, 0, breadWeek.Length);
                    continue;
                }

                day++;
            }

            //if (_breadWeekDict[thisWeekStartDate].Items[0].BreadDescDict == null)
            //    InitThisWeekBreadDesc();
        }

        /// <summary>
        /// Gets the bread order items for the given period
        /// </summary>
        /// <param name="period"></param>
        /// <returns></returns>
        public IEnumerable<InventoryItem> GetBreadPeriodOrders(PeriodMarker period)
        {
            return MainHelper.GetWeeksInPeriod(period).Where(x => x.StartDate < DateTime.Now).SelectMany(week => GetBreadWeek(week).GetWeekAsInvItems());
            //foreach (WeekMarker week in MainHelper.GetWeeksInPeriod(period).Where(x => x.StartDate < DateTime.Now))
            //{
            //    //foreach (KeyValuePair<string, BreadDescriptor> descKvp in GetBreadWeek(week).WeekNoTotal.Where(x => x.BreadDescDict != null)
            //    //                                                                            .SelectMany(x => x.BreadDescDict.ToList()))
            //    //{
            //    //    InventoryItem item = VIContainer.Items.First(x => x.Name == descKvp.Key).Copy<InventoryItem>();
            //    //    item.LastOrderAmount = descKvp.Value.Delivery;
            //    //    yield return item;
            //    //}
            //    foreach (KeyValuePair<string, BreadDescriptor> descKvp in GetBreadWeek(week).WeekNoTotal.Where(x => x.BreadDescDict != null)
            //                                                                                .SelectMany(x => x.BreadDescDict.ToList()))
            //    {
            //        InventoryItem item = VIContainer.Items.First(x => x.Name == descKvp.Key).Copy<InventoryItem>();
            //        item.LastOrderAmount = descKvp.Value.Delivery;
            //        yield return item;
            //    }
            //}
        }

        /// <summary>
        /// Gets a BreadWeekContainer by the specified date
        /// </summary>
        public BreadWeekContainer GetBreadWeek(DateTime date)
        {
            return GetBreadWeek(MainHelper.GetWeek(date));
        }

        /// <summary>
        /// Gets a BreadWeekContainer by the specified week
        /// </summary>
        public BreadWeekContainer GetBreadWeek(WeekMarker week)
        {
            if (_breadWeekDict.ContainsKey(week.StartDate))
                return _breadWeekDict[week.StartDate];

            BreadOrder[] breadWeek = new BreadOrder[8];

            Dictionary<string, float> parFactors = null;
            if (_breadWeekDict.Count > 0)
            {
                Dictionary<string, BreadDescriptor> latestDesc = _breadWeekDict[_breadWeekDict.Keys.Max()].Items[0].BreadDescDict;
                if(latestDesc != null)
                    parFactors = latestDesc.ToDictionary(x => x.Key, y => y.Value.ParFactor);
            }

            if(parFactors == null)
            {
                parFactors = new Dictionary<string, float>();
                foreach (string breadType in VIContainer.Items.Where(x => x.Category == "Bread").Select(x => x.Name))
                {
                    parFactors[breadType] = 0;
                }
            }

            for (int i = 0; i < 7; i++)
            {
                BreadOrder bo = new BreadOrder(week.StartDate.AddDays(i));

                breadWeek[i] = bo;
                if(i > 0 && breadWeek[i - 1] != null)
                    breadWeek[i - 1].NextBreadOrder = bo;

                if (parFactors != null)
                {
                    foreach (string breadType in parFactors.Keys)
                    {
                        if (breadWeek[i].GetBreadDescriptor(breadType) != null && breadWeek[i].BreadDescDict.ContainsKey(breadType))
                            breadWeek[i].BreadDescDict[breadType].ParFactor = parFactors[breadType];
                    }
                }
                bo.Insert();
            }

            BreadOrder[] tempBreadWeek = new BreadOrder[7];
            Array.Copy(breadWeek, tempBreadWeek, 7);
            breadWeek[7] = new BreadOrderTotal(ref tempBreadWeek);

            return new BreadWeekContainer(breadWeek.ToList(), VIContainer.Items.Where(x => x.Category == "Bread").ToList());
        }

        /// <summary>
        /// Gets the types of bread that exist
        /// </summary>
        /// <returns></returns>
        public List<string> GetBreadTypes()
        {
            return VIContainer.Items.Where(x => x.Category.ToUpper() == "BREAD").Select(x => x.Name).ToList();
        }

        /// <summary>
        /// Calculates par factors for bread ordering
        /// </summary>
        /// <param name="breadOrders"></param>
        /// <returns></returns>
        private Dictionary<string, float> GetParFactors(List<BreadOrder> breadOrders)
        {
            //TODO: throw away outliers
            // calculate par using data from a month back
            DateTime pastDate = DateTime.Today.AddDays(-30);
            Dictionary<string, float> salesDict = new Dictionary<string, float>();
            Dictionary<string, int> usageDict = new Dictionary<string, int>();

            BreadOrder[] boArray = breadOrders.Where(x => x.Date >= pastDate).OrderBy(x => x.Date).ToArray();
            for (int i = 0; i < boArray.Length - 1; i++)
            {
                BreadOrder bo = boArray[i];
                bo.NextBreadOrder = boArray[i + 1];
                if(bo.BreadDescDict != null && bo.GrossSales > 0)
                {
                    foreach (KeyValuePair<string, BreadDescriptor> kvp in bo.BreadDescDict)
                    {
                        if (kvp.Value.Usage > 0)
                        {
                            MainHelper.AddToDict(ref salesDict, kvp.Key, bo.GrossSales, (x, y) => x + y);
                            MainHelper.AddToDict(ref usageDict, kvp.Key, kvp.Value.Usage, (x, y) => x + y);
                        }
                    }
                }
            }

            return salesDict.ToDictionary(x => x.Key, x => usageDict[x.Key] > 0 ? x.Value / usageDict[x.Key] : 1);
        }

    }
}
