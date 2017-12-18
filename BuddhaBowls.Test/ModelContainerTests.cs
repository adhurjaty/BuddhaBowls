using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BuddhaBowls.Models;
using BuddhaBowls.Services;
using BuddhaBowls.Helpers;
using System.Linq;

namespace BuddhaBowls.Test
{
    /// <summary>
    /// Summary description for ModelContainerTests
    /// </summary>
    [TestClass]
    public class ModelContainerTests
    {
        private DBCache _models;

        public ModelContainerTests()
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
        

        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void TestMethod1()
        {
            //
            // TODO: Add test logic here
            //
        }

        [TestMethod]
        public void AddVendorInvItemTest()
        {
            string name = "New Test Item";
            int origSize = _models.VIContainer.Items.Count;
            VendorInventoryItem testItem = null;

            InventoryItem dbItem;
            try
            {
                Vendor v = new Vendor(new Dictionary<string, string>() { { "Name", "Sysco" } });
                InventoryItem item = new InventoryItem() { Name = name };
                testItem = _models.VIContainer.AddItem(item, new List<VendorInfo>() { new VendorInfo()
                                                                            { Vend = v, Conversion = 1, Price = 12.5f, PurchasedUnit = "EACH" } });

                Assert.AreEqual(origSize, testItem.Id);
                CollectionAssert.Contains(_models.VIContainer.Items, testItem);
                dbItem = ModelHelper.InstantiateList<InventoryItem>("InventoryItem").First(x => x.Name == name);
                Assert.AreEqual(origSize, dbItem.Id);

                List<InventoryItem> syscoItems = _models.VContainer.Items.First(x => x.Name == "Sysco").GetInventoryItems();
                CollectionAssert.Contains(syscoItems.Select(x => x.Name).ToList(), testItem.Name);
            }
            finally
            {
                _models.VIContainer.RemoveItem(testItem);
                testItem.Destroy();
            }

            CollectionAssert.DoesNotContain(_models.VIContainer.Items, testItem);
            dbItem = ModelHelper.InstantiateList<InventoryItem>("InventoryItem").FirstOrDefault(x => x.Name == name);
            Assert.IsNull(dbItem);
            CollectionAssert.DoesNotContain(BuddhaBowls.Properties.Settings.Default.InventoryOrder, name);
        }

        [TestMethod]
        public void UpdateInventoryItemTest()
        {
            //VendorInventoryItem testItem = _models.VIContainer.Items.First();
            //float tempOrderAmt = testItem.LastOrderAmount;
            //int refId = testItem.Id;
            //int refCount = _models.VIContainer.Items.Count;
            //testItem.LastOrderAmount = 77;

            //UpdateBinding updateBinding = delegate () { testItem.Update(); };
            //_models.VIContainer.AddUpdateBinding(updateBinding);

            //try
            //{
            //    Vendor v = new Vendor(new Dictionary<string, string>() { { "Name", "Sysco" } });
            //    _models.VIContainer.AddItem(testItem, new List<VendorInfo>() { new VendorInfo() { Vend = v, Conversion = 1, Price = 12.5f,
            //                                                                                      PurchasedUnit = "EACH" } });

            //    Assert.AreEqual(refCount, _models.VIContainer.Items.Count);
            //    InventoryItem dbItem = ModelHelper.InstantiateList<InventoryItem>("InventoryItem").FirstOrDefault(x => x.Name == testItem.Name);
            //    Assert.AreEqual(77, dbItem.LastOrderAmount);
            //    Assert.AreEqual(refId, dbItem.Id);

            //    List<InventoryItem> syscoItems = _models.VContainer.Items.First(x => x.Name == "Sysco").GetInventoryItems();
            //    CollectionAssert.Contains(syscoItems.Select(x => x.Name).ToList(), testItem.Name);
            //}
            //finally
            //{
            //    testItem.LastOrderAmount = tempOrderAmt;
            //    testItem.Update();
            //}
        }

        [TestMethod]
        public void AddVendorTest()
        {
            string name = "New Test Vendor";
            Vendor testVendor = new Vendor() { Name = name };

            try
            {
                _models.VContainer.AddItem(testVendor);

                CollectionAssert.Contains(_models.VContainer.Items.Select(x => x.Name).ToList(), testVendor.Name);
                Assert.AreEqual(5, testVendor.Id);
                Vendor dbItem = ModelHelper.InstantiateList<Vendor>("Vendor").First(x => x.Name == name);
                Assert.AreEqual(5, dbItem.Id);
            }
            finally
            {
                _models.VContainer.RemoveItem(testVendor);
            }
            CollectionAssert.DoesNotContain(_models.VContainer.Items.Select(x => x.Name).ToList(), testVendor.Name);
            Vendor newDbItem = ModelHelper.InstantiateList<Vendor>("InventoryItem").FirstOrDefault(x => x.Name == name);
            Assert.IsNull(newDbItem);
        }

        [TestMethod]
        public void UpdateVendorTest()
        {
            Vendor testVendor = _models.VContainer.Items.First().Copy<Vendor>();
            int refId = testVendor.Id;
            int refCount = _models.VContainer.Items.Count;
            string phone = "8887777";
            testVendor.PhoneNumber = phone;

            _models.VContainer.Update(testVendor);

            Assert.AreSame(_models.VContainer.Items.First(), testVendor);
            Assert.AreEqual(phone, testVendor.PhoneNumber);
            Assert.AreEqual(refId, testVendor.Id);
            Assert.AreEqual(refCount, _models.VContainer.Items.Count);
        }

        //[TestMethod]
        //public void GetVendorsFromItemTest()
        //{
        //    InventoryItem testItem = _models.VIContainer.Items.First(x => x.Name == "Cheddar");

        //    Dictionary<Vendor, InventoryItem> testDict = _models.GetVendorsFromItem(testItem);
        //    List<Vendor> keyList = new List<Vendor>(testDict.Keys);

        //    Assert.AreEqual(2, keyList.Count);
        //    Assert.AreEqual(7, testDict[keyList.First(x => x.Name == "Another guy")].LastOrderAmount);
        //    Assert.AreEqual(3, testDict[keyList.First(x => x.Name == "Sysco")].LastOrderAmount);
        //}

        //[TestMethod]
        //public void GetEmptyVendorsFromItemTest()
        //{
        //    InventoryItem testItem = _models.VIContainer.Items.First(x => x.Name == "Cookie Dough");

        //    Dictionary<Vendor, InventoryItem> testDict = _models.GetVendorsFromItem(testItem);
        //    List<Vendor> keyList = new List<Vendor>(testDict.Keys);

        //    Assert.AreEqual(0, keyList.Count);
        //}

        [TestMethod]
        public void GetCategoryValuesTest()
        {
            Dictionary<string, float> refCostDict = new Dictionary<string, float>()
            {
                { "Bread", 55f + 66f  },
                { "Dairy", 185f  },
                { "Produce", 90f  },
            };

            Dictionary<string, float> costDict = _models.VIContainer.GetCategoryValues();

            CollectionAssert.AreEquivalent(refCostDict, costDict);
        }
    }
}
