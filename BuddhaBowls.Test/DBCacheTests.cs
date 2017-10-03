using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BuddhaBowls.Models;
using BuddhaBowls.Helpers;
using BuddhaBowls.Services;
using System.Linq;
using System.IO;

namespace BuddhaBowls.Test
{
    /// <summary>
    /// Summary description for ModelContainerTests
    /// </summary>
    [TestClass]
    public class DBCacheTests
    {
        private DBCache _models;

        public DBCacheTests()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void MyTestInitialize()
        {
            BuddhaBowls.Properties.Settings.Default.DBLocation = Properties.Settings.Default.DBLocation;
            _models = new DBCache();
        }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void GetIngredientsTest()
        {
            _models.VIContainer.SetItems(_models.VIContainer.Items.Take(5).ToList());
            _models.Recipes = _models.Recipes.Take(4).ToList();

            List<IItem> allItems = _models.GetAllIItems();

            List<IItem> refList = new List<IItem>()
            {
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Mozzarella" } }),
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Cheddar" } }),
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Feta" } }),
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Butter" } }),
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Sour Cream" } }),
                new Recipe(new Dictionary<string, string>() { { "Name", "Mac & Cheese" } }),
                new Recipe(new Dictionary<string, string>() { { "Name", "Balsamic Vinegar" } }),
                new Recipe(new Dictionary<string, string>() { { "Name", "Chicken Marinade" } }),
                new Recipe(new Dictionary<string, string>() { { "Name", "Chili" } })
            };

            CollectionAssert.AreEqual(refList.Select(x => x.Name).ToList(), allItems.Select(x => x.Name).ToList());
        }

        [TestMethod]
        public void GetCountUnitsTest()
        {
            List<string> countUnits = _models.GetCountUnits();

            List<string> refCountUnits = new List<string>()
            {
                "5lb BAG",
                "1lb",
                "5lb BUCKET",
                "1/2 GALLON",
                "TRAY",
                "GALLON",
                "QUART",
                "BUCKET",
                "CAN",
                "JUG",
                "BAG",
                "6\" PAN",
                "JAR",
                "BOTTLE",
                "EACH",
                "LB",
                "GAL",
                "OZ-wt",
                "Container",
                "1/2 CASE",
                "FLAT",
                "CASE",
                "BASKET",
                "LOG"
            };

            CollectionAssert.AreEquivalent(refCountUnits, countUnits);
        }

        [TestMethod]
        public void GetRecipeUnitsTest()
        {
            List<string> recUnits = _models.GetRecipeUnits();

            List<string> refrecUnits = new List<string>()
            {
                "OZ-wt",
                "CUP",
                "EA",
                "OZ-fl",
                "TBL",
                "BUNCH"
            };

            CollectionAssert.AreEquivalent(refrecUnits, recUnits);
        }

        [TestMethod]
        public void AddInventoryItemTest()
        {
            string name = "New Test Item";
            InventoryItem testItem = new InventoryItem() { Name = name };
            int origSize = _models.VIContainer.Items.Count;

            InventoryItem dbItem;
            try
            {
                _models.AddUpdateInventoryItem(ref testItem);

                CollectionAssert.Contains(_models.InventoryItems, testItem);
                Assert.AreEqual(origSize, testItem.Id);
                dbItem = ModelHelper.InstantiateList<InventoryItem>("InventoryItem").First(x => x.Name == name);
                Assert.AreEqual(origSize, dbItem.Id);
            }
            finally
            {
                _models.DeleteInventoryItem(testItem);
            }

            CollectionAssert.DoesNotContain(_models.InventoryItems, testItem);
            dbItem = new InventoryItem(new Dictionary<string, string>() { { "Name", name } });
            Assert.IsNull(dbItem.Name);
            CollectionAssert.DoesNotContain(BuddhaBowls.Properties.Settings.Default.InventoryOrder, name);

        }

        [TestMethod]
        public void UpdateInventoryItemTest()
        {
            InventoryItem testItem = _models.InventoryItems.First();
            float tempOrderAmt = testItem.LastOrderAmount;
            int refId = testItem.Id;
            int refCount = _models.InventoryItems.Count;
            testItem.LastOrderAmount = 77;

            try
            {
                _models.AddUpdateInventoryItem(ref testItem);

                Assert.AreSame(_models.InventoryItems.First(), testItem);
                Assert.AreEqual(77, testItem.LastOrderAmount);
                Assert.AreEqual(refId, testItem.Id);
                Assert.AreEqual(refCount, _models.InventoryItems.Count);
                InventoryItem dbItem = ModelHelper.InstantiateList<InventoryItem>("InventoryItem").FirstOrDefault(x => x.Name == testItem.Name);
                Assert.AreEqual(77, dbItem.LastOrderAmount);
            }
            finally
            {
                testItem.LastOrderAmount = tempOrderAmt;
                _models.AddUpdateInventoryItem(ref testItem);
            }
        }

        [TestMethod]
        public void AddVendorTest()
        {
            string name = "New Test Vendor";
            Vendor testVendor = new Vendor() { Name = name };

            try
            {
                _models.AddUpdateVendor(ref testVendor);

                CollectionAssert.Contains(_models.Vendors, testVendor);
                Assert.AreEqual(5, testVendor.Id);
                Vendor dbItem = ModelHelper.InstantiateList<Vendor>("Vendor").First(x => x.Name == name);
                Assert.AreEqual(5, dbItem.Id);
            }
            finally
            {
                _models.DeleteVendor(testVendor);
            }
            CollectionAssert.DoesNotContain(_models.InventoryItems, testVendor);
            Vendor newDbItem = ModelHelper.InstantiateList<Vendor>("InventoryItem").FirstOrDefault(x => x.Name == name);
            Assert.IsNull(newDbItem);
        }

        [TestMethod]
        public void UpdateVendorTest()
        {
            Vendor testVendor = _models.Vendors.First();
            string tempPhone = testVendor.PhoneNumber;
            int refId = testVendor.Id;
            int refCount = _models.Vendors.Count;
            string phone = "8887777";
            testVendor.PhoneNumber = phone;

            _models.AddUpdateVendor(ref testVendor);

            Assert.AreSame(_models.Vendors.First(), testVendor);
            Assert.AreEqual(phone, testVendor.PhoneNumber);
            Assert.AreEqual(refId, testVendor.Id);
            Assert.AreEqual(refCount, _models.Vendors.Count);
            Vendor dbItem = ModelHelper.InstantiateList<Vendor>("Vendor").FirstOrDefault(x => x.Name == testVendor.Name);
            Assert.AreEqual(phone, dbItem.PhoneNumber);

            testVendor.PhoneNumber = tempPhone;
            _models.AddUpdateVendor(ref testVendor);
        }

        [TestMethod]
        public void GetVendorsFromItemTest()
        {
            InventoryItem testItem = _models.InventoryItems.First(x => x.Name == "Cheddar");

            Dictionary<Vendor, InventoryItem> testDict = _models.GetVendorsFromItem(testItem);
            List<Vendor> keyList = new List<Vendor>(testDict.Keys);

            Assert.AreEqual(2, keyList.Count);
            Assert.AreEqual(7, testDict[keyList.First(x => x.Name == "Another guy")].LastOrderAmount);
            Assert.AreEqual(3, testDict[keyList.First(x => x.Name == "Sysco")].LastOrderAmount);
        }

        [TestMethod]
        public void GetEmptyVendorsFromItemTest()
        {
            InventoryItem testItem = _models.InventoryItems.First(x => x.Name == "Cookie Dough");

            Dictionary<Vendor, InventoryItem> testDict = _models.GetVendorsFromItem(testItem);
            List<Vendor> keyList = new List<Vendor>(testDict.Keys);

            Assert.AreEqual(0, keyList.Count);
        }

        [TestMethod]
        public void GetCategoryValuesTest()
        {
            _models.InventoryItems = new List<InventoryItem>()
            {
                new InventoryItem() { Name = "Sourdough", Category = "Bread", LastPurchasedPrice = 5f, Conversion = 1, Count = 10, Yield = 1 },
                new InventoryItem() { Name = "Wheat", Category = "Bread", LastPurchasedPrice = 6f, Conversion = 1, Count = 10, Yield = 1 },
                new InventoryItem() { Name = "Milk", Category = "Dairy", LastPurchasedPrice = 7f, Conversion = 1, Count = 10, Yield = 1 },
                new InventoryItem() { Name = "Cheese", Category = "Dairy", LastPurchasedPrice = 8f, Conversion = 1, Count = 10, Yield = 1 },
                new InventoryItem() { Name = "Celery", Category = "Produce", LastPurchasedPrice = 9f, Conversion = 1, Count = 10, Yield = 1 }
            };

            _models.PrepItems = new List<PrepItem>()
            {
                new PrepItem() { Name = "Sourdough",Cost = 5f, LineCount = 1 },
                new PrepItem() { Name = "Wheat", Cost = 6f, WalkInCount = 1 },
                new PrepItem() { Name = "Milk", Cost = 1f, WalkInCount = 5, LineCount = 10 },
                new PrepItem() { Name = "Cheese", Cost = 1f, LineCount = 10, WalkInCount = 10 },
            };

            Dictionary<string, float> refCostDict = new Dictionary<string, float>()
            {
                { "Bread", 55f + 66f  },
                { "Dairy", 185f  },
                { "Produce", 90f  },
            };

            Dictionary<string, float> costDict = _models.GetCategoryValues();

            CollectionAssert.AreEquivalent(refCostDict, costDict);
        }

        [TestMethod]
        public void CreateTestObjects()
        {
            //List<InventoryItem> items = ModelHelper.InstantiateList<InventoryItem>("InventoryItem");
            //foreach (InventoryItem item in items)
            //{
            //    MockObjects.SaveInventoryItem(item);
            //}

            //List<Recipe> recipes = ModelHelper.InstantiateList<Recipe>("Recipe");
            //foreach (Recipe rec in recipes)
            //{
            //    MockObjects.SaveRecipe(rec);
            //}
        }

        [TestMethod]
        public void WeekLabelsTest()
        {
            List<string> labels = _models.GetWeekLabels(13).Select(x => x.ToString()).ToList();

            List<string> refLabels = new List<string>()
            {
                "WK1 12/4-12/10",
                "WK2 12/11-12/17",
                "WK3 12/18-12/24",
                "WK4 12/25-12/31"
            };

            CollectionAssert.AreEqual(refLabels, labels);
        }

        [TestMethod]
        public void WeekLabelsGet0Test()
        {
            List<string> labels = _models.GetWeekLabels(0).Select(x => x.ToString()).ToList();

            List<string> refLabels = new List<string>()
            {
                "WK0 1/1-1/1"
            };

            CollectionAssert.AreEqual(refLabels, labels);
        }

        [TestMethod]
        public void PeriodLabelsTest()
        {
            List<string> labels = _models.GetPeriodLabels().Select(x => x.ToString()).ToList();

            List<string> refLabels = new List<string>()
            {
                "P0 1/1-1/1",
                "P1 1/2-1/29",
                "P2 1/30-2/26",
                "P3 2/27-3/26",
                "P4 3/27-4/23",
                "P5 4/24-5/21",
                "P6 5/22-6/18",
                "P7 6/19-7/16",
                "P8 7/17-8/13",
                "P9 8/14-9/10",
                "P10 9/11-10/8",
                "P11 10/9-11/5",
                "P12 11/6-12/3",
                "P13 12/4-12/31"
            };

            CollectionAssert.AreEqual(refLabels, labels);
        }
    }
}
