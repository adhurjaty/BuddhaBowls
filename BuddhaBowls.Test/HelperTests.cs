﻿using System;
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

        [TestMethod]
        public void WeekLabelsTest()
        {
            List<string> labels = MainHelper.GetWeekLabels(13, 2017).Select(x => x.ToString()).ToList();

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
            List<string> labels = MainHelper.GetWeekLabels(0, 2017).Select(x => x.ToString()).ToList();

            List<string> refLabels = new List<string>()
            {
                "WK0 1/1-1/1"
            };

            CollectionAssert.AreEqual(refLabels, labels);
        }

        [TestMethod]
        public void PeriodLabelsTest()
        {
            List<string> labels = MainHelper.GetPeriodLabels(2017).Select(x => x.ToString()).ToList();

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

        [TestMethod]
        public void MergeFloatDictsTest()
        {
            Dictionary<string, float> dict1 = new Dictionary<string, float>()
            {
                { "a", 3 },
                { "b", 5 },
                { "c", 8 }
            };

            Dictionary<string, float> dict2 = new Dictionary<string, float>()
            {
                { "b", 5 },
                { "c", 8 },
                { "d", 2 }
            };

            Dictionary<string, float> refDict = new Dictionary<string, float>()
            {
                { "a", 3 },
                { "b", 10 },
                { "c", 16 },
                { "d", 2 }
            };

            Dictionary<string, float> outDict = MainHelper.MergeDicts(dict1, dict2, (x, y) => x + y);

            CollectionAssert.AreEquivalent(refDict.Keys, outDict.Keys);
            foreach (string key in refDict.Keys)
            {
                Assert.AreEqual(refDict[key], outDict[key]);
            }
        }

        [TestMethod]
        public void MergeListDictsTest()
        {
            Dictionary<string, List<int>> dict1 = new Dictionary<string, List<int>>()
            {
                { "a", new List<int>() { 3, 5 } },
                { "b", new List<int>() { 5, 1 } },
                { "c", new List<int>() { 8 } }
            };

            Dictionary<string, List<int>> dict2 = new Dictionary<string, List<int>>()
            {
                { "b", new List<int>() { 5, 8, 7 } },
                { "c", new List<int>() { 8, 1, 1, 1, 1 } },
                { "d", new List<int>() { 2 } }
            };

            Dictionary<string, List<int>> refDict = new Dictionary<string, List<int>>()
            {
                { "a", new List<int>() { 3, 5 } },
                { "b", new List<int>() { 5, 1, 5, 8, 7 } },
                { "c", new List<int>() { 8, 8, 1, 1, 1, 1 } },
                { "d", new List<int>() { 2 } }
            };

            Dictionary<string, List<int>> outDict = MainHelper.MergeDicts(dict1, dict2, (x, y) => x.Concat(y).ToList());

            CollectionAssert.AreEquivalent(refDict.Keys, outDict.Keys);
            foreach (string key in refDict.Keys)
            {
                CollectionAssert.AreEqual(refDict[key], outDict[key]);
            }
        }

        [TestMethod]
        public void AddToDictTest()
        {
            Dictionary<string, int> outDict = new Dictionary<string, int>();
            List<string> keys = new List<string>() { "a", "b", "a", "c", "b", "a" };
            List<int> vals = new List<int>() { 1, 4, 6, 5, 1, 9 };

            Dictionary<string, int> refDict = new Dictionary<string, int>()
            {
                { "a", 16 },
                { "b", 5 },
                { "c", 5 }
            };

            for (int i = 0; i < keys.Count; i++)
            {
                MainHelper.AddToDict(ref outDict, keys[i], vals[i], (x, y) => x + y);
            }

            CollectionAssert.AreEquivalent(refDict.Keys, outDict.Keys);
            foreach (string key in refDict.Keys)
            {
                Assert.AreEqual(refDict[key], outDict[key]);
            }
        }
    }
}
