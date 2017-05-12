using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BuddhaBowls.Helpers;
using System.Linq;
using BuddhaBowls.Services;
using BuddhaBowls.Models;

namespace BuddhaBowls.Test
{
    /// <summary>
    /// Summary description for HelperTests
    /// </summary>
    [TestClass]
    public class HelperTests
    {
        public HelperTests()
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
        
        
        [TestInitialize()]
        public void MyTestInitialize()
        {
            BuddhaBowls.Properties.Settings.Default.DBLocation = Properties.Settings.Default.DBLocation;
        }

        [TestCleanup()]
        public void MyTestCleanup()
        {
            BuddhaBowls.Properties.Settings.Default.InventoryOrder = null;
        }

        #endregion

        [TestMethod]
        public void ListToCsvTest()
        {
            List<string[]> contents = new List<string[]>()
            {
                new string[] { "Hello", "World", "What!", "are..." },
                new string[] { "lkasdf", "Worllk;mLKMVd", "kasmdfsfafeiso!", "are..." },
                new string[] { "asdfn", "help", "What!", "are..." },
                new string[] { "Hello", "", "What!", "are..." },
                new string[] { "Hello", "World", "|||", "are..." },
                new string[] { "Hello", "", "What!", "are..." }
            };

            string output = MainHelper.ListToCsv(contents);

            string desired = "Hello,World,What!,are...\n" +
                            "lkasdf,Worllk;mLKMVd,kasmdfsfafeiso!,are...\n" +
                            "asdfn,help,What!,are...\n" +
                            "Hello,,What!,are...\n" +
                            "Hello,World,|||,are...\n" +
                            "Hello,,What!,are...";

            Assert.AreEqual(desired, output);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Contents cannot have a , in them")]
        public void ListToCsvErrorTest()
        {
            List<string[]> contents = new List<string[]>()
            {
                new string[] { "Hello", "World", "What!", "are..." },
                new string[] { "lkasdf", "Worllk;mLKMVd", "kasm,dfsfafeiso!", "are..." },
                new string[] { "asdfn", "help", "What!", "are..." },
                new string[] { "Hello", "", "What!", "are..." },
                new string[] { "Hello", "World", ",,,", "are..." },
                new string[] { "Hello", "", "What!", "are..." }
            };

            MainHelper.ListToCsv(contents);
        }

        [TestMethod]
        public void SortItemsNoOrderTest()
        {
            List<InventoryItem> unordered = new List<InventoryItem>()
            {
                MockObjects.GetInventoryItem("Avocado"),
                MockObjects.GetInventoryItem("Cinnamon"),
                MockObjects.GetInventoryItem("Bacon Bits"),
                MockObjects.GetInventoryItem("Cucumbers"),
                MockObjects.GetInventoryItem("Celery"),
                MockObjects.GetInventoryItem("Black Beans"),
                MockObjects.GetInventoryItem("Dried Basil")
            };

            BuddhaBowls.Properties.Settings.Default.InventoryOrder = null;
            List<InventoryItem> ordered = MainHelper.SortItems(unordered).ToList();

            List<InventoryItem> refOrdered = new List<InventoryItem>()
            {
                MockObjects.GetInventoryItem("Avocado"),
                MockObjects.GetInventoryItem("Bacon Bits"),
                MockObjects.GetInventoryItem("Black Beans"),
                MockObjects.GetInventoryItem("Celery"),
                MockObjects.GetInventoryItem("Cinnamon"),
                MockObjects.GetInventoryItem("Cucumbers"),
                MockObjects.GetInventoryItem("Dried Basil")
            };

            CollectionAssert.AreEqual(refOrdered.Select(x => x.Name).ToList(), ordered.Select(x => x.Name).ToList());
        }

        [TestMethod]
        public void SortItemsFullOrderTest()
        {
            BuddhaBowls.Properties.Settings.Default.InventoryOrder = new List<string>()
            {
                "Cinnamon",
                "Black Beans",
                "Cucumbers",
                "Avocado",
                "Dried Basil",
                "Bacon Bits",
                "Celery"
            };

            List<InventoryItem> unordered = new List<InventoryItem>()
            {
                MockObjects.GetInventoryItem("Avocado"),
                MockObjects.GetInventoryItem("Cinnamon"),
                MockObjects.GetInventoryItem("Bacon Bits"),
                MockObjects.GetInventoryItem("Cucumbers"),
                MockObjects.GetInventoryItem("Celery"),
                MockObjects.GetInventoryItem("Black Beans"),
                MockObjects.GetInventoryItem("Dried Basil")
            };

            List<InventoryItem> ordered = MainHelper.SortItems(unordered).ToList();

            List<InventoryItem> refOrdered = new List<InventoryItem>()
            {
                MockObjects.GetInventoryItem("Cinnamon"),
                MockObjects.GetInventoryItem("Black Beans"),
                MockObjects.GetInventoryItem("Cucumbers"),
                MockObjects.GetInventoryItem("Avocado"),
                MockObjects.GetInventoryItem("Dried Basil"),
                MockObjects.GetInventoryItem("Bacon Bits"),
                MockObjects.GetInventoryItem("Celery")
            };

            CollectionAssert.AreEqual(refOrdered.Select(x => x.Name).ToList(), ordered.Select(x => x.Name).ToList());
        }

        [TestMethod]
        public void SortItemsPartialOrderTest()
        {
            BuddhaBowls.Properties.Settings.Default.InventoryOrder = new List<string>()
            {
                "Cinnamon",
                "Black Beans",
                "Cucumbers"
            };

            List<InventoryItem> unordered = new List<InventoryItem>()
            {
                MockObjects.GetInventoryItem("Avocado"),
                MockObjects.GetInventoryItem("Cinnamon"),
                MockObjects.GetInventoryItem("Bacon Bits"),
                MockObjects.GetInventoryItem("Cucumbers"),
                MockObjects.GetInventoryItem("Celery"),
                MockObjects.GetInventoryItem("Black Beans"),
                MockObjects.GetInventoryItem("Dried Basil")
            };

            List<InventoryItem> ordered = MainHelper.SortItems(unordered).ToList();

            List<InventoryItem> refOrdered = new List<InventoryItem>()
            {
                MockObjects.GetInventoryItem("Cinnamon"),
                MockObjects.GetInventoryItem("Black Beans"),
                MockObjects.GetInventoryItem("Cucumbers"),
                MockObjects.GetInventoryItem("Avocado"),
                MockObjects.GetInventoryItem("Bacon Bits"),
                MockObjects.GetInventoryItem("Celery"),
                MockObjects.GetInventoryItem("Dried Basil")
            };

            CollectionAssert.AreEqual(refOrdered.Select(x => x.Name).ToList(), ordered.Select(x => x.Name).ToList());
        }

        [TestMethod]
        public void CategoryGroupingTest()
        {
            BuddhaBowls.Properties.Settings.Default.InventoryOrder = new List<string>()
            {
                "Cinnamon",
                "Black Beans",
                "Cucumbers",
                "Avocado",
                "Dried Basil",
                "Bacon Bits",
                "Celery"
            };

            List<InventoryItem> unordered = new List<InventoryItem>()
            {
                MockObjects.GetInventoryItem("Avocado"),
                MockObjects.GetInventoryItem("Cinnamon"),
                MockObjects.GetInventoryItem("Bacon Bits"),
                MockObjects.GetInventoryItem("Cucumbers"),
                MockObjects.GetInventoryItem("Celery"),
                MockObjects.GetInventoryItem("Black Beans"),
                MockObjects.GetInventoryItem("Dried Basil")
            };

            List<IGrouping<string, InventoryItem>> group = MainHelper.CategoryGrouping(unordered).ToList();

            List<string> refCategoryOrder = new List<string>()
            {
                "Herbs",
                "Dry Goods",
                "Produce",
                "Meats"
            };

            List<IGrouping<string, InventoryItem>> refGroup = new List<InventoryItem>()
            {
                // Herbs
                MockObjects.GetInventoryItem("Cinnamon"),
                MockObjects.GetInventoryItem("Dried Basil"),
                // Dry Goods
                MockObjects.GetInventoryItem("Black Beans"),
                // Produce
                MockObjects.GetInventoryItem("Cucumbers"),
                MockObjects.GetInventoryItem("Avocado"),
                MockObjects.GetInventoryItem("Celery"),
                // Meats
                MockObjects.GetInventoryItem("Bacon Bits")
            }.GroupBy(x => x.Category).ToList();

            CollectionAssert.AreEqual(refCategoryOrder, group.Select(x => x.Key).ToList());

            
            foreach (IGrouping<string, InventoryItem> refItem in refGroup)
            {
                foreach(IGrouping<string, InventoryItem> compItem in group)
                {
                    if (refItem.Key == compItem.Key)
                    {
                        CollectionAssert.AreEqual(refItem.Select(x => x.Name).ToList(), compItem.Select(x => x.Name).ToList());
                        break;
                    }
                }
            }
        }
    }
}
