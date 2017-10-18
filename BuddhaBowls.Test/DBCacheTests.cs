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
        public void GetBreadPeriodOrdersTest()
        {
            PeriodMarker period = MainHelper.GetWeek(new DateTime(2017, 7, 17));

            InventoryItem sourdough = new InventoryItem(new Dictionary<string, string>() { { "Name", "Sourdough" } });
            sourdough.LastOrderAmount = 1;
            InventoryItem wheat = new InventoryItem(new Dictionary<string, string>() { { "Name", "Wheat" } });
            wheat.LastOrderAmount = 2;

            List<InventoryItem> refItems = new List<InventoryItem>();

            for (int i = 0; i < 7; i++)
            {
                refItems.Add(sourdough);
                refItems.Add(wheat);
            }

            List<InventoryItem> breadItems = _models.GetBreadPeriodOrders(period).ToList();

            CollectionAssert.AreEquivalent(refItems.Select(x => x.Name).ToList(), breadItems.Select(x => x.Name).ToList());
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
    }
}
