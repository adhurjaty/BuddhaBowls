﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BuddhaBowls.Test.TestData;
using System.Collections.Generic;
using System.IO;
using BuddhaBowls.Models;
using System.Linq;

namespace BuddhaBowls.Test
{
    [TestClass]
    public class ModelTests
    {
        private const string TEST_OBJECT_PATH = @"C:\Users\Developer\Documents\Visual Studio 2015\Projects\BuddhaBowls\BuddhaBowls.Test\TestData\Test.csv";
        private string _tableCopy;

        public ModelTests()
        {

        }

        [TestInitialize]
        public void InitializeTests()
        {
            BuddhaBowls.Properties.Settings.Default.DBLocation = Properties.Settings.Default.DBLocation;
            _tableCopy = Util.CopyTable(Path.Combine(Properties.Settings.Default.DBLocation, "Test.csv"));
        }

        [TestCleanup]
        public void CleanupTests()
        {
            File.Delete(_tableCopy);
        }

        #region Model Tests

        [TestMethod]
        public void GetModelTest()
        {
            TestModel tModel = new TestModel(new Dictionary<string, string> { { "Id", "1" } });

            Assert.AreEqual(1, tModel.Id);
            Assert.AreEqual("lksdf", tModel.Col3);
        }

        [TestMethod]
        public void UpdateModelTest()
        {
            TestModel tModel = new TestModel(new Dictionary<string, string> { { "Id", "1" } });
            tModel.Col3 = "new Col3 value";
            tModel.Update();

            TestModel newModel = new TestModel(new Dictionary<string, string> { { "Id", "1" } });
            Assert.AreEqual(tModel.Col3, newModel.Col3);
        }

        [TestMethod]
        public void DeleteModelTest()
        {
            TestModel tModel = new TestModel(new Dictionary<string, string> { { "Id", "1" } });
            tModel.Destroy();

            TestModel newModel = new TestModel(new Dictionary<string, string> { { "Id", "1" } });
            Assert.AreEqual(-1, newModel.Id);
        }

        [TestMethod]
        public void InsertModelTest()
        {
            TestModel tModel = new TestModel() { Col1 = "something", Col2 = "wicked", Col3 = "this", AnotherCol = "way" };
            tModel.Insert();

            TestModel refModel = new TestModel(new Dictionary<string, string>() { { "Col2", "wicked" } });
            Assert.AreEqual(4, refModel.Id);
            Assert.AreEqual("this", refModel.Col3);
        }

        #endregion

        #region Inventory Tests

        [TestMethod]
        public void GetInventoryHistoryTest()
        {
            Inventory inv = new Inventory(new DateTime(2017, 3, 1));

            List<InventoryItem> history = inv.GetInventoryHistory();

            List<string> refHistory = new List<string>()
            {
                "Mozzarella",
                "Cheddar",
                "Pepper Jack",
                "Feta",
                "Butter",
                "Sour Cream"
            };

            CollectionAssert.AreEquivalent(refHistory, history.Select(x => x.Name).ToList());
        }

        [TestMethod]
        public void InsertInventoryTest()
        {
            DateTime nowDate = DateTime.Now;
            Inventory inv = new Inventory(nowDate);

            List<InventoryItem> refList = new List<InventoryItem>()
            {
                MockObjects.GetInventoryItem("Butter"),
                MockObjects.GetInventoryItem("Cumin"),
                MockObjects.GetInventoryItem("Eggs"),
                MockObjects.GetInventoryItem("Flour"),
                MockObjects.GetInventoryItem("Feta"),
            };

            inv.Insert(refList);

            try
            {
                Assert.IsTrue(File.Exists(inv.GetHistoryTablePath()));

                List<InventoryItem> invList = inv.GetInventoryHistory();

                foreach (InventoryItem item in invList)
                {
                    InventoryItem refItem = refList.First(x => x.Id == item.Id);
                    Dictionary<string, string> comp = item.Compare(refItem);
                    Assert.AreEqual(0, comp.Keys.Count);
                }
            }
            catch(Exception e)
            {
                Assert.IsFalse(true);
            }
            finally
            {
                inv.Destroy();
            }

            Assert.IsFalse(File.Exists(inv.GetHistoryTablePath()));
        }


        [TestMethod]
        public void UpdateInventoryTest()
        {
            Inventory inv = new Inventory(new DateTime(2017, 3, 20));
            List<InventoryItem> oldList = inv.GetInventoryHistory();

            float tempCount = oldList[0].Count;
            oldList[0].Count = 111;
            string tempCountUnit = oldList[1].CountUnit;
            oldList[1].CountUnit = "NewUnit";

            inv.Update(oldList);

            List<InventoryItem> newList = inv.GetInventoryHistory();

            Assert.AreEqual(111, newList[0].Count);
            Assert.AreEqual("NewUnit", newList[1].CountUnit);

            newList[0].Count = tempCount;
            newList[1].CountUnit = tempCountUnit;
            inv.Update(newList);
        }

        #endregion

        #region InventoryItem Tests

        [TestMethod]
        public void GetCostTest()
        {
            InventoryItem inv = new InventoryItem()
            {
                Name = "TestItem",
                LastPurchasedPrice = 20,
                Conversion = 2,
                RecipeUnitConversion = 10,
                Yield = 1
            };

            Assert.AreEqual(1, inv.GetCost());
        }

        [TestMethod]
        public void GetLastCountTest()
        {
            InventoryItem inv = new InventoryItem(new Dictionary<string, string>() { { "Name", "Mozzarella" } });
            float refCount = inv.Count;

            inv.Count = 123;

            Assert.AreEqual(refCount, inv.GetLastCount());
        }

        [TestMethod]
        public void GetPrevOrderAmountTest()
        {
            InventoryItem inv = new InventoryItem(new Dictionary<string, string>() { { "Name", "Mozzarella" } });
            float refOrderAmt = inv.LastOrderAmount;

            inv.LastOrderAmount = 123;

            Assert.AreEqual(refOrderAmt, inv.GetPrevOrderAmount());
        }

        #endregion

    }
}
