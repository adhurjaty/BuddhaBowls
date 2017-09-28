using BuddhaBowls.Helpers;
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
    public class DBCache
    {
        private Dictionary<string, string> _categoryColors;
        private Dictionary<DateTime, BreadWeekContainer> _breadWeekDict;

        public List<Recipe> Recipes { get; set; }
        public BreadOrder[] BreadWeek { get; set; }
        public List<DailySale> DailySales { get; set; }
        public List<ExpenseItem> ExpenseItems { get; set; }
        public VendorInvItemsContainer VIContainer { get; private set; }
        public PurchaseOrdersContainer POContainer { get; private set; }
        public VendorsContainer VContainer { get; private set; }
        public InventoriesContainer InContainer { get; private set; }

        public DBCache()
        {
            InitializeModels();
            InitializeInventoryOrder();
            InitializeFoodCategories();
            //if (InventoryItems != null)
            //{
            SetCategoryColors();
            //}
        }

        private void InitializeModels()
        {
            Recipes = ModelHelper.InstantiateList<Recipe>("Recipe") ?? new List<Recipe>();
            AddRecipeItems();
            DailySales = ModelHelper.InstantiateList<DailySale>("DailySale") ?? new List<DailySale>();
            ExpenseItems = ModelHelper.InstantiateList<ExpenseItem>("ExpenseItem") ?? new List<ExpenseItem>();

            List<InventoryItem> invItems = MainHelper.SortItems(ModelHelper.InstantiateList<InventoryItem>("InventoryItem") ??
                                           new List<InventoryItem>()).ToList();

            VContainer = new VendorsContainer(ModelHelper.InstantiateList<Vendor>("Vendor") ?? new List<Vendor>());
            VIContainer = new VendorInvItemsContainer(invItems.Select(x => new VendorInventoryItem(x, GetVendorsFromItem(x))).ToList(),
                                                       VContainer);
            POContainer = new PurchaseOrdersContainer(ModelHelper.InstantiateList<PurchaseOrder>("PurchaseOrder") ?? new List<PurchaseOrder>());
            InContainer = new InventoriesContainer(ModelHelper.InstantiateList<Inventory>("Inventory") ?? new List<Inventory>());
            _breadWeekDict = new Dictionary<DateTime, BreadWeekContainer>();
            InitBreadOrders();
        }

        private void AddRecipeItems()
        {
            foreach (Recipe item in Recipes)
            {
                item.GetRecipeItems();
            }
        }

        private void InitializeInventoryOrder()
        {
            string orderPath = Path.Combine(Properties.Settings.Default.DBLocation, "Settings", GlobalVar.INV_ORDER_FILE);
            if (File.Exists(orderPath))
                Properties.Settings.Default.InventoryOrder = new List<string>(File.ReadAllLines(orderPath));
            else
                Properties.Settings.Default.InventoryOrder = new List<string>(VIContainer.Items.Select(x => x.Name).OrderBy(x => x));
            Properties.Settings.Default.Save();
        }

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
        /// Converts a list of InventoryItems to VendorInventoryItems. Used for instantiating an old inventory 
        /// </summary>
        /// <param name="invItems"></param>
        /// <returns></returns>
        public List<VendorInventoryItem> InvToVendorInvList(List<InventoryItem> invItems)
        {
            return invItems.Select(x => new VendorInventoryItem(x, VContainer.Items.First(y => y.Id == x.LastVendorId))).ToList();
        }

        /// <summary>
        /// Get the total cost of the recipe's ingredients
        /// </summary>
        /// <param name="rec"></param>
        /// <returns>Cost</returns>
        public float GetBatchItemCost(Recipe rec)
        {
            return rec.GetCost();
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
        /// Get all of the count units that currently exist in inventory items
        /// </summary>
        /// <returns></returns>
        public List<string> GetCountUnits()
        {
            return MainHelper.EnsureCaseInsensitive(new HashSet<string>(VIContainer.Items.Select(x => x.CountUnit)
                                                                           .Where(x => !string.IsNullOrEmpty(x)))).ToList();
        }

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

        public IEnumerable<InventoryItem> GetBreadPeriodOrders(PeriodMarker period)
        {
            foreach (WeekMarker week in MainHelper.GetWeeksInPeriod(period).Where(x => x.StartDate < DateTime.Now))
            {
                foreach (KeyValuePair<string, BreadDescriptor> descKvp in GetBreadWeek(week).WeekNoTotal.Where(x => x.BreadDescDict != null)
                                                                                            .SelectMany(x => x.BreadDescDict.ToList()))
                {
                    InventoryItem item = VIContainer.Items.First(x => x.Name == descKvp.Key).Copy<InventoryItem>();
                    item.LastOrderAmount = descKvp.Value.Delivery;
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Loads the inventory items as a container into the inventory and returns the inventory object
        /// </summary>
        /// <param name="inv"></param>
        /// <returns></returns>
        public void LoadInvContainer(Inventory inv)
        {
            List<InventoryItem> items = inv.GetInvItems();
            inv.SetInvItemsContainer(new VendorInvItemsContainer(items.Select(x => new VendorInventoryItem(x, GetVendorsFromItem(x)))
                                                                                .ToList(),
                                                                 VContainer));
        }

        /// <summary>
        /// Gets a dictionary of vendors that offer the passed-in inventory item. The inventory item value is the vendor-specific inventory
        /// item associated with that vendor (not the one from the model container, which is passed in). Really should be in VendorInvItemsContainer
        /// but cannot put it in due to build error problems
        /// </summary>
        public Dictionary<Vendor, InventoryItem> GetVendorsFromItem(InventoryItem item)
        {
            Dictionary<Vendor, InventoryItem> vendorDict = new Dictionary<Vendor, InventoryItem>();
            foreach(Vendor v in VContainer.Items)
            {
                InventoryItem vendorItem = v.ItemList.FirstOrDefault(x => x.Id == item.Id);
                if (vendorItem != null)
                    vendorDict[v] = vendorItem;
            }

            return vendorDict;
        }

        public List<string> GetRecipeCategories()
        {
            HashSet<string> categories = new HashSet<string>();
            foreach (Recipe rec in Recipes)
            {
                if (!string.IsNullOrWhiteSpace(rec.Category))
                    categories.Add(rec.Category);
            }

            return categories.ToList();
        }

        public List<IItem> GetAllIItems()
        {
            return VIContainer.Items.Select(x => (IItem)x.ToInventoryItem()).Concat(Recipes).ToList();
        }

        public List<string> GetInventoryCategories()
        {
            HashSet<string> categories = new HashSet<string>();

            foreach (VendorInventoryItem item in VIContainer.Items)
            {
                if(!string.IsNullOrWhiteSpace(item.Category))
                    categories.Add(item.Category);
            }

            return categories.ToList();
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
        /// Gets the all category breakdown of the value of inventory (inventory and prep items)
        /// </summary>
        /// <returns></returns>
        //public Dictionary<string, float> GetCategoryValues()
        //{
        //    Dictionary<string, float> costDict = new Dictionary<string, float>();
        //    foreach (VendorInventoryItem item in VIContainer.Items)
        //    {
        //        if (!costDict.Keys.Contains(item.Category))
        //            costDict[item.Category] = 0;
        //        costDict[item.Category] += item.PriceExtension;
        //    }

        //    foreach(KeyValuePair<string, float> kvp in GetPrepCatValues())
        //    {
        //        if (!costDict.Keys.Contains(kvp.Key))
        //            costDict[kvp.Key] = 0;
        //        costDict[kvp.Key] += kvp.Value;
        //    }

        //    return costDict;
        //}

        //public Dictionary<string, float> GetPrepCatValues()
        //{
        //    Dictionary<string, float> costDict = new Dictionary<string, float>();

        //    foreach (PrepItem item in PrepItems)
        //    {
        //        InventoryItem invItem = VIContainer.Items.FirstOrDefault(x => x.Name == item.Name);
        //        if (invItem != null)
        //        {
        //            if (!costDict.Keys.Contains(invItem.Category))
        //                costDict[invItem.Category] = 0;
        //            costDict[invItem.Category] += item.Extension;
        //            continue;
        //        }

        //        // separate prep item extension costs among the categories
        //        Recipe recipe = Recipes.FirstOrDefault(x => x.Name == item.Name);
        //        if (recipe != null)
        //        {
        //            Dictionary<string, float> recipeCatProps = recipe.GetCatCostProportions();
        //            if (recipeCatProps != null)
        //            {
        //                foreach (KeyValuePair<string, float> kvp in recipeCatProps)
        //                {
        //                    if (!costDict.Keys.Contains(kvp.Key))
        //                        costDict[kvp.Key] = 0;
        //                    costDict[kvp.Key] += kvp.Value * item.Extension;
        //                }
        //            }
        //        }
        //    }

        //    return costDict;
        //}

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
            DateTime firstDay = DateTime.Today;

            foreach (BreadOrder bo in breadOrders)
            {
                if(breadWeek[0] == null)
                {
                    firstDay = MainHelper.GetWeek(bo.Date).StartDate;
                    if (bo.Date != firstDay)
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
                if(breadWeek[6] != null)
                {
                    BreadOrder[] tempBreadWeek = new BreadOrder[7];
                    Array.Copy(breadWeek, tempBreadWeek, 7);
                    breadWeek[7] = new BreadOrderTotal(ref tempBreadWeek);
                    _breadWeekDict[firstDay] = new BreadWeekContainer(breadWeek.ToList());
                    day = 0;
                    Array.Clear(breadWeek, 0, breadWeek.Length);
                    continue;
                }

                day++;
            }
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
                parFactors = latestDesc.ToDictionary(x => x.Key, y => y.Value.ParFactor);
            }

            for (int i = 0; i < 7; i++)
            {
                BreadOrder bo = new BreadOrder(week.StartDate.AddDays(i));
                bo.Insert();

                breadWeek[i] = bo;
                if(i > 0 && breadWeek[i - 1] != null)
                    breadWeek[i - 1].NextBreadOrder = bo;

                if (parFactors != null)
                {
                    foreach (string breadType in parFactors.Keys)
                    {
                        if (breadWeek[i].BreadDescDict != null && breadWeek[i].BreadDescDict.ContainsKey(breadType))
                            breadWeek[i].BreadDescDict[breadType].ParFactor = parFactors[breadType];
                    }
                }
            }

            BreadOrder[] tempBreadWeek = new BreadOrder[7];
            Array.Copy(breadWeek, tempBreadWeek, 7);
            breadWeek[7] = new BreadOrderTotal(ref tempBreadWeek);

            return new BreadWeekContainer(breadWeek.ToList());
        }

        //public BreadOrder[] GetBreadWeekNoTotal(WeekMarker week)
        //{
        //    return GetBreadWeek(week).Take(7).ToArray();
        //}

        public List<string> GetBreadTypes()
        {
            return VIContainer.Items.Where(x => x.Category.ToUpper() == "BREAD").Select(x => x.Name).ToList();
        }

        //private void SetBreadWeek()
        //{
        //    BreadWeek = GetBreadWeek(MainHelper.GetThisWeek());
        //}

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
                        if (!salesDict.ContainsKey(kvp.Key))
                        {
                            salesDict[kvp.Key] = 0;
                            usageDict[kvp.Key] = 0;
                        }

                        if (kvp.Value.Useage > 0)
                        {
                            salesDict[kvp.Key] += bo.GrossSales;
                            usageDict[kvp.Key] += kvp.Value.Useage;
                        }
                    }
                }
            }

            return salesDict.ToDictionary(x => x.Key, x => usageDict[x.Key] > 0 ? x.Value / usageDict[x.Key] : 1);
        }
    }

    /// <summary>
    /// Class to hold the inventory items associated with which vendors sell them
    /// </summary>
    public class VendorInvItemsContainer : ModelContainer<VendorInventoryItem>
    {
        // tracks vendors, necessary for each item to reference
        private VendorsContainer _vendorsContainer;

        /// <summary>
        /// Instantiate the container. Should only be called in the DBCache class
        /// </summary>
        /// <param name="items">List of vendor inventory items to contain</param>
        /// <param name="vContainer">Vendor container</param>
        public VendorInvItemsContainer(List<VendorInventoryItem> items, VendorsContainer vContainer) : base(items)
        {
            _vendorsContainer = vContainer;
        }

        /// <summary>
        /// Adds a new item with a list of which vendors sell the item
        /// </summary>
        /// <param name="item">New item</param>
        /// <param name="vendors">List of vendors that sell it</param>
        public void AddItem(InventoryItem item, List<VendorInfo> vendors)
        {
            int idx = Items.FindIndex(x => x.Id == item.Id);
            if (idx != -1)
            {
                _items[idx].Update(vendors);
                _items[idx].InvItem = item;
                PushChange();
            }
            else
            {
                VendorInventoryItem vItem = new VendorInventoryItem(item);

                vItem.Id = item.Insert();
                vItem.SetVendorDict(vendors);

                int insertIdx = Properties.Settings.Default.InventoryOrder.IndexOf(item.Name);
                if (insertIdx == -1)
                    _items.Add(vItem);
                else
                    _items.Insert(insertIdx, vItem);

                PushChange();
            }
        }

        /// <summary>
        /// Removes item from current inventory list and all vendor lists
        /// </summary>
        /// <param name="item"></param>
        public override void RemoveItem(VendorInventoryItem item)
        {
            _vendorsContainer.RemoveItemFromVendors(item);

            base.RemoveItem(item);
        }

        /// <summary>
        /// Updates the items in the list. Does not remove any items from the master list
        /// </summary>
        /// <param name="items"></param>
        public void Update(List<VendorInventoryItem> items)
        {
            foreach (VendorInventoryItem item in items)
            {
                int idx = Items.FindIndex(x => x.Id == item.Id);
                Items[idx] = item;
            }
            PushChange();
        }

        /// <summary>
        /// Copy the container
        /// </summary>
        /// <returns></returns>
        public VendorInvItemsContainer Copy()
        {
            return new VendorInvItemsContainer(_items, _vendorsContainer);
        }

        /// <summary>
        /// Adds vendor to vendor container and associates items with new vendor
        /// </summary>
        /// <param name="vend"></param>
        public void AddVendor(Vendor vend, List<InventoryItem> invItems)
        {
            _vendorsContainer.AddItem(vend);
            vend.SetItemList(invItems);
            foreach (InventoryItem item in invItems)
            {
                Items.First(x => x.Id == item.Id).AddVendor(vend, item);
            }
            PushChange();
        }

        /// <summary>
        /// Removes a vendor from vendor container and all associations with inv items
        /// </summary>
        /// <param name="vend"></param>
        public void RemoveVendor(Vendor vend)
        {
            _vendorsContainer.RemoveItem(vend);
            foreach (InventoryItem item in vend.ItemList)
            {
                Items.First(x => x.Id == item.Id).DeleteVendor(vend);
            }
            PushChange();
        }

        /// <summary>
        /// Save the display order of the inventory items
        /// </summary>
        public void SaveOrder()
        {
            string dir = Path.Combine(Properties.Settings.Default.DBLocation, "Settings");
            Directory.CreateDirectory(dir);
            File.WriteAllLines(Path.Combine(dir, GlobalVar.INV_ORDER_FILE), Properties.Settings.Default.InventoryOrder);
            _items = MainHelper.SortItems(_items).ToList();
        }

        /// <summary>
        /// Get the PriceExtension value of each category of items
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, float> GetCategoryValues()
        {
            Dictionary<string, float> costDict = new Dictionary<string, float>();

            foreach (VendorInventoryItem item in _items)
            {
                if (!costDict.Keys.Contains(item.Category))
                    costDict[item.Category] = 0;
                costDict[item.Category] += item.PriceExtension;
            }

            return costDict;
        }

        /// <summary>
        /// Update (push to DB) all of the items in the container
        /// </summary>
        public void UpdateContainer()
        {
            foreach (VendorInventoryItem item in Items)
            {
                item.Update();
            }
        }

        /// <summary>
        /// Associates item with vendor and updates vendor
        /// </summary>
        /// <param name="item"></param>
        /// <param name="vend"></param>
        public void UpdateItem(InventoryItem item, Vendor vend)
        {
            vend.AddInvItem(item);
            _vendorsContainer.Update(vend);
            PushChange();
        }

        /// <summary>
        /// Associates vendor with all of items in invItems, removes association with vendor and items not in invItems
        /// </summary>
        /// <param name="vendor"></param>
        /// <param name="list"></param>
        public void UpdateVendorItems(Vendor vendor, List<InventoryItem> invItems)
        {
            RemoveVendor(vendor);
            AddVendor(vendor, invItems);
        }
    }

    /// <summary>
    /// Container for holding inventories
    /// </summary>
    public class InventoriesContainer : ModelContainer<Inventory>
    {
        /// <summary>
        /// Instantiate the container
        /// </summary>
        /// <param name="items"></param>
        public InventoriesContainer(List<Inventory> items) : base(items)
        {

        }

        /// <summary>
        /// Adds or overwrites inventory based on date
        /// </summary>
        /// <param name="inv"></param>
        public override void AddItem(Inventory inv)
        {
            int idx = Items.FindIndex(x => x.Date.Date == inv.Date);

            if (idx != -1)
            {
                Items[idx].Id = inv.Id;
                Items[idx] = inv;
                PushChange();
            }
            else
            {
                base.AddItem(inv);
            }
        }

        /// <summary>
        /// Updates the inventory
        /// </summary>
        /// <param name="inv"></param>
        public override void Update(Inventory inv)
        {
            int idx = _items.FindIndex(x => x.Id == inv.Id);
            _items[idx].Date = inv.Date;
            base.Update(inv);
        }
    }

    /// <summary>
    /// Container for vendors
    /// </summary>
    public class VendorsContainer : ModelContainer<Vendor>
    {
        /// <summary>
        /// Instantiate container
        /// </summary>
        /// <param name="items"></param>
        public VendorsContainer(List<Vendor> items) : base(items)
        {
            //foreach (Vendor vend in items)
            //{
            //    vend.InitItems();
            //}
        }

        /// <summary>
        /// Check to see if the vendor name already exists (case and space insensitive)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public override bool Contains(Vendor item)
        {
            return Items.Select(x => x.Name.ToUpper().Replace(" ", "")).Contains(item.Name.ToUpper().Replace(" ", ""));
        }

        /// <summary>
        /// Adds or updates vendor and sets the items sold to the invItems parameter
        /// </summary>
        /// <param name="vend"></param>
        /// <param name="invItems"></param>
        public void AddItem(Vendor vend, List<InventoryItem> invItems)
        {

        }

        /// <summary>
        /// Removes the inventory item from all the vendors' lists that contain it
        /// </summary>
        /// <param name="item"></param>
        public void RemoveItemFromVendors(VendorInventoryItem item)
        {
            foreach (Vendor v in item.Vendors)
            {
                v.RemoveInvItem(item);
            }
        }
    }

    public class PurchaseOrdersContainer : ModelContainer<PurchaseOrder>
    {
        public PurchaseOrdersContainer(List<PurchaseOrder> items) : base(items)
        {

        }
    }

    public class BreadWeekContainer : ModelContainer<BreadOrder>
    {
        public BreadOrder[] Week
        {
            get
            {
                return Items.ToArray();
            }
        }

        public BreadOrder[] WeekNoTotal
        {
            get
            {
                return Week.Take(7).ToArray();
            }
        }

        public BreadWeekContainer(List<BreadOrder> items) : base(items)
        {

        }
    }
}
