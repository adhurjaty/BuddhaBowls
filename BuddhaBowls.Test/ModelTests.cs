using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BuddhaBowls.Test.TestData;
using System.Collections.Generic;
using System.IO;
using BuddhaBowls.Models;
using System.Linq;
using BuddhaBowls.Services;

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

        #region Vendor Tests

        [TestMethod]
        public void InsertVendorTest()
        {
            Vendor vend = new Vendor()
            {
                Name = "TestVendor",
                Email = "testvendor@test.com"
            };

            List<InventoryItem> itemList = new List<InventoryItem>()
            {
                MockObjects.GetInventoryItem("Butter"),
                MockObjects.GetInventoryItem("Cumin"),
                MockObjects.GetInventoryItem("Eggs"),
                MockObjects.GetInventoryItem("Flour"),
                MockObjects.GetInventoryItem("Feta"),
            };

            try
            {
                vend.Insert(itemList);

                Vendor dbVendor = new Vendor(new Dictionary<string, string>() { { "Name", "TestVendor" } });
                Assert.AreEqual("testvendor@test.com", dbVendor.Email);

                List<InventoryItem> newItemList = vend.GetInventoryItems();

                foreach (InventoryItem newItem in newItemList)
                {
                    InventoryItem refItem = itemList.First(x => x.Id == newItem.Id);
                    Assert.AreEqual(refItem.LastPurchasedPrice, newItem.LastPurchasedPrice);
                    Assert.AreEqual(refItem.Count, newItem.Count);
                }
            }
            finally
            {
                vend.Destroy();
            }
            Vendor newDbVendor = new Vendor(new Dictionary<string, string>() { { "Name", "TestVendor" } });
            Assert.IsNull(newDbVendor.Name);
            Assert.IsNull(vend.GetInventoryItems());
        }

        [TestMethod]
        public void UpdateVendorTest()
        {
            Vendor vend = new Vendor()
            {
                Name = "TestVendor",
                Email = "testvendor@test.com"
            };

            List<InventoryItem> itemList = new List<InventoryItem>()
            {
                MockObjects.GetInventoryItem("Butter"),
                MockObjects.GetInventoryItem("Cumin"),
                MockObjects.GetInventoryItem("Eggs"),
                MockObjects.GetInventoryItem("Flour"),
                MockObjects.GetInventoryItem("Feta"),
            };

            try
            {
                vend.Insert(itemList);

                itemList[0].Count = 999;
                itemList[1].LastPurchasedPrice = 22.22f;

                vend.PhoneNumber = "5559999";
                vend.Update(itemList);
                List<InventoryItem> newList = vend.GetInventoryItems();

                Vendor newVend = new Vendor(new Dictionary<string, string>() { { "Name", vend.Name } });

                Assert.AreEqual(999, newList.First(x => x.Name == "Butter").Count);
                Assert.AreEqual(22.22f, newList.First(x => x.Name == "Cumin").LastPurchasedPrice);
                Assert.AreEqual("5559999", newVend.PhoneNumber);
            }
            finally
            {
                vend.Destroy();
            }
        }

        [TestMethod]
        public void ResetVendorTest()
        {
            Vendor vend = new Vendor()
            {
                Name = "TestVendor",
                Email = "testvendor@test.com"
            };

            List<InventoryItem> itemList = new List<InventoryItem>()
            {
                MockObjects.GetInventoryItem("Butter"),
                MockObjects.GetInventoryItem("Cumin"),
                MockObjects.GetInventoryItem("Eggs"),
                MockObjects.GetInventoryItem("Flour"),
                MockObjects.GetInventoryItem("Feta"),
            };

            try
            {
                vend.Insert(itemList);

                vend.Email = "fakeEmail@example.com";
                vend.PhoneNumber = "6789";

                vend.Reset();

                Assert.AreEqual("testvendor@test.com", vend.Email);
                Assert.IsNull(vend.PhoneNumber);
            }
            finally
            {
                vend.Destroy();
            }
        }

        [TestMethod]
        public void DeleteVendorInvItemTest()
        {
            Vendor vend = new Vendor()
            {
                Name = "TestVendor",
                Email = "testvendor@test.com"
            };

            List<InventoryItem> itemList = new List<InventoryItem>()
            {
                MockObjects.GetInventoryItem("Butter"),
                MockObjects.GetInventoryItem("Cumin"),
                MockObjects.GetInventoryItem("Eggs"),
                MockObjects.GetInventoryItem("Flour"),
                MockObjects.GetInventoryItem("Feta")
            };

            InventoryItem newItem = MockObjects.GetInventoryItem("Chopped Clams");

            try
            {
                vend.Insert(itemList);
                vend.Update(newItem);
                List<InventoryItem> newList = vend.GetInventoryItems();

                Assert.IsNotNull(newList.First(x => x.Name == "Chopped Clams"));

                vend.RemoveInvItem(newItem);
                newList = vend.GetInventoryItems();
                Assert.IsNull(newList.FirstOrDefault(x => x.Name == "Chopped Clams"));
            }
            finally
            {
                vend.Destroy();
            }
        }

        #endregion

        #region VendorInventoryItem Tests

        [TestMethod]
        public void UpdateVendorPriceTest()
        {
            ModelContainer models = new ModelContainer();
            InventoryItem item = models.InventoryItems.First(x => x.Name == "Feta");
            Dictionary<Vendor, InventoryItem> vendDict = models.GetVendorsFromItem(item);
            VendorInventoryItem vi = new VendorInventoryItem(vendDict, item);
            List<Vendor> vendors = new List<Vendor>(vendDict.Keys);

            vi.SelectedVendor = vendors[0];
            float tempPrice0 = vi.LastPurchasedPrice;
            vi.LastPurchasedPrice = 69.69f;

            try
            {
                vi.UpdateVendorProps();

                InventoryItem testItem = vendors[0].GetInventoryItems().First(x => x.Name == "Feta");
                Assert.AreEqual(69.69f, testItem.LastPurchasedPrice);

                vi.SelectedVendor = vendors[1];
                Assert.AreNotEqual(69.69f, vi.LastPurchasedPrice);
            }
            finally
            {
                vi.SelectedVendor = vendors[0];
                vi.LastPurchasedPrice = tempPrice0;
                vi.UpdateVendorProps();
            }
        }

        [TestMethod]
        public void ToInventoryItemTest()
        {
            ModelContainer models = new ModelContainer();
            InventoryItem item = models.InventoryItems.First(x => x.Name == "Feta");
            Dictionary<Vendor, InventoryItem> vendDict = models.GetVendorsFromItem(item);
            VendorInventoryItem vi = new VendorInventoryItem(vendDict, item);

            string[] refProperties = (new InventoryItem()).GetPropertiesDB();
            CollectionAssert.AreEqual(refProperties, vi.ToInventoryItem().GetPropertiesDB());
            CollectionAssert.AreEqual(refProperties, vi.GetPropertiesDB());
        }

        #endregion

        #region PurchaseOrder Tests

        [TestMethod]
        public void ConstructPurchaseOrderTest()
        {
            Vendor berryMan = new Vendor(new Dictionary<string, string>() { { "Name", "Berry Man" } });
            Dictionary<string, float> updateOrderDict = new Dictionary<string, float>()
            {
                { "Artichoke Hearts", 44f },
                { "Avocado", 22f },
                { "Vanilla", 11f }
            };

            List<InventoryItem> orderItems = new List<InventoryItem>();
            foreach (KeyValuePair<string, float> kvp in updateOrderDict)
            {
                orderItems.Add(new InventoryItem(new Dictionary<string, string>() { { "Name", kvp.Key } }) { LastOrderAmount = kvp.Value });
            }

            PurchaseOrder po = new PurchaseOrder(berryMan, orderItems, DateTime.Today);

            try
            {
                List<InventoryItem> berryManItems = berryMan.GetInventoryItems();
                List<InventoryItem> poItems = po.GetOpenPOItems();

                foreach (KeyValuePair<string, float> kvp in updateOrderDict)
                {
                    InventoryItem bItem = berryManItems.First(x => x.Name == kvp.Key);
                    Assert.AreEqual(kvp.Value, bItem.LastOrderAmount);

                    InventoryItem oItem = poItems.First(x => x.Name == kvp.Key);
                    Assert.AreEqual(kvp.Value, oItem.LastOrderAmount);
                    Assert.AreEqual(DateTime.Today, oItem.LastPurchasedDate);
                }
            }
            finally
            {
                po.Destroy();
            }

            Assert.IsFalse(File.Exists(po.GetOrderPath()));
        }

        [TestMethod]
        public void ReceiveOrderTest()
        {
            Vendor berryMan = new Vendor(new Dictionary<string, string>() { { "Name", "Berry Man" } });
            Dictionary<string, float> updateOrderDict = new Dictionary<string, float>()
            {
                { "Artichoke Hearts", 44f },
                { "Avocado", 22f },
                { "Vanilla", 11f }
            };

            List<InventoryItem> orderItems = new List<InventoryItem>();
            foreach (KeyValuePair<string, float> kvp in updateOrderDict)
            {
                orderItems.Add(new InventoryItem(new Dictionary<string, string>() { { "Name", kvp.Key } }) { LastOrderAmount = kvp.Value });
            }

            PurchaseOrder po = new PurchaseOrder(berryMan, orderItems, DateTime.Today);

            try
            {
                po.Receive();
                List<InventoryItem> receivedItems = po.GetPOItems()[1];
                CollectionAssert.AreEquivalent(updateOrderDict.Keys, receivedItems.Select(x => x.Name).ToList());
                Assert.AreEqual(DateTime.Today, po.ReceivedDate);
            }
            finally
            {
                po.Destroy();
            }
        }

        [TestMethod]
        public void ReopenOrderTest()
        {
            Vendor berryMan = new Vendor(new Dictionary<string, string>() { { "Name", "Berry Man" } });
            Dictionary<string, float> updateOrderDict = new Dictionary<string, float>()
            {
                { "Artichoke Hearts", 44f },
                { "Avocado", 22f },
                { "Vanilla", 11f }
            };

            List<InventoryItem> orderItems = new List<InventoryItem>();
            foreach (KeyValuePair<string, float> kvp in updateOrderDict)
            {
                orderItems.Add(new InventoryItem(new Dictionary<string, string>() { { "Name", kvp.Key } }) { LastOrderAmount = kvp.Value });
            }

            PurchaseOrder po = new PurchaseOrder(berryMan, orderItems, DateTime.Today);

            try
            {
                po.Receive();
                po.ReOpen();
                List<InventoryItem> reopened = po.GetPOItems()[0];
                CollectionAssert.AreEquivalent(updateOrderDict.Keys, reopened.Select(x => x.Name).ToList());
                Assert.IsNull(po.ReceivedDate);
            }
            finally
            {
                po.Destroy();
            }
        }

        [TestMethod]
        public void SplitPurchaseOrderTest()
        {
            Vendor berryMan = new Vendor(new Dictionary<string, string>() { { "Name", "Berry Man" } });
            Dictionary<string, float> updateOrderDict = new Dictionary<string, float>()
            {
                { "Artichoke Hearts", 44f },
                { "Avocado", 22f },
                { "Vanilla", 11f }
            };

            List<InventoryItem> orderItems = new List<InventoryItem>();
            foreach (KeyValuePair<string, float> kvp in updateOrderDict)
            {
                orderItems.Add(new InventoryItem(new Dictionary<string, string>() { { "Name", kvp.Key } }) { LastOrderAmount = kvp.Value });
            }

            PurchaseOrder po = new PurchaseOrder(berryMan, orderItems, DateTime.Today);

            try
            {
                po.SplitToPartials(orderItems.Take(2).ToList(), orderItems.Skip(2).ToList());

                Assert.IsTrue(po.IsPartial);
                List<InventoryItem>[] openReceivedItems = po.GetPOItems();

                CollectionAssert.AreEqual(new string[] { "Artichoke Hearts", "Avocado" }, openReceivedItems[0].Select(x => x.Name).ToArray());
                CollectionAssert.AreEqual(new string[] { "Vanilla" }, openReceivedItems[1].Select(x => x.Name).ToArray());
                Assert.IsFalse(File.Exists(po.GetOrderPath()));

                string[] paths = po.GetPartialOrderPaths();
                foreach (string path in paths)
                {
                    Assert.IsTrue(File.Exists(path));
                }
            }
            finally
            {
                po.Destroy();
            }

            string[] missingPaths = po.GetPartialOrderPaths();
            foreach (string path in missingPaths)
            {
                Assert.IsFalse(File.Exists(path));
            }
        }

        [TestMethod]
        public void DeleteOpenPartialOrderTest()
        {
            Vendor berryMan = new Vendor(new Dictionary<string, string>() { { "Name", "Berry Man" } });
            Dictionary<string, float> updateOrderDict = new Dictionary<string, float>()
            {
                { "Artichoke Hearts", 44f },
                { "Avocado", 22f },
                { "Vanilla", 11f }
            };

            List<InventoryItem> orderItems = new List<InventoryItem>();
            foreach (KeyValuePair<string, float> kvp in updateOrderDict)
            {
                orderItems.Add(new InventoryItem(new Dictionary<string, string>() { { "Name", kvp.Key } }) { LastOrderAmount = kvp.Value });
            }

            PurchaseOrder po = new PurchaseOrder(berryMan, orderItems, DateTime.Today);

            try
            {
                po.SplitToPartials(orderItems.Take(2).ToList(), orderItems.Skip(2).ToList());
                po.DeleteOpenPartial();

                Assert.IsTrue(File.Exists(po.GetOrderPath()));
                Assert.IsFalse(po.GetPartialOrderPaths().Select(x => File.Exists(x)).Any(x => x));
                CollectionAssert.AreEqual(orderItems.Skip(2).Select(x => x.Name).ToList(), po.GetPOItems()[1].Select(x => x.Name).ToList());
            }
            finally
            {
                po.Destroy();
            }
        }

        [TestMethod]
        public void ReceivePartialOrderTest()
        {
            Vendor berryMan = new Vendor(new Dictionary<string, string>() { { "Name", "Berry Man" } });
            Dictionary<string, float> updateOrderDict = new Dictionary<string, float>()
            {
                { "Artichoke Hearts", 44f },
                { "Avocado", 22f },
                { "Vanilla", 11f }
            };

            List<InventoryItem> orderItems = new List<InventoryItem>();
            foreach (KeyValuePair<string, float> kvp in updateOrderDict)
            {
                orderItems.Add(new InventoryItem(new Dictionary<string, string>() { { "Name", kvp.Key } }) { LastOrderAmount = kvp.Value });
            }

            PurchaseOrder po = new PurchaseOrder(berryMan, orderItems, DateTime.Today);

            try
            {
                po.SplitToPartials(orderItems.Take(2).ToList(), orderItems.Skip(2).ToList());
                po.Receive();

                Assert.IsFalse(po.IsPartial);
                List<InventoryItem> receivedItems = po.GetPOItems()[1];

                CollectionAssert.AreEquivalent(updateOrderDict.Keys, receivedItems.Select(x => x.Name).ToList());
                Assert.AreEqual(DateTime.Today, po.ReceivedDate);
            }
            finally
            {
                po.Destroy();
            }
        }

        [TestMethod]
        public void ReopenPartialOrderTest()
        {
            Vendor berryMan = new Vendor(new Dictionary<string, string>() { { "Name", "Berry Man" } });
            Dictionary<string, float> updateOrderDict = new Dictionary<string, float>()
            {
                { "Artichoke Hearts", 44f },
                { "Avocado", 22f },
                { "Vanilla", 11f }
            };

            List<InventoryItem> orderItems = new List<InventoryItem>();
            foreach (KeyValuePair<string, float> kvp in updateOrderDict)
            {
                orderItems.Add(new InventoryItem(new Dictionary<string, string>() { { "Name", kvp.Key } }) { LastOrderAmount = kvp.Value });
            }

            PurchaseOrder po = new PurchaseOrder(berryMan, orderItems, DateTime.Today);

            try
            {
                po.SplitToPartials(orderItems.Take(2).ToList(), orderItems.Skip(2).ToList());
                po.ReOpen();

                Assert.IsFalse(po.IsPartial);
                List<InventoryItem> reopened = po.GetPOItems()[0];

                CollectionAssert.AreEquivalent(updateOrderDict.Keys, reopened.Select(x => x.Name).ToList());
                Assert.IsNull(po.ReceivedDate);
            }
            finally
            {
                po.Destroy();
            }
        }

        #endregion

        #region Recipe Tests

        [TestMethod]
        public void InsertAndDeleteRecipeTest()
        {
            string name = "Test Recipe";
            string category = "Test Rec Category";
            Recipe recipe = new Recipe() { Name = name, Category = category, RecipeUnitConversion = 1 };
            recipe.ItemList = new List<IItem>()
            {
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Avocado" } }),
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Black Beans" } }),
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Cinnamon" } }),
                new Recipe(new Dictionary<string, string>() { { "Name", "Chopped Clams" } })
            };

            try
            {
                recipe.Insert();
                Recipe dbRecipe = new Recipe(new Dictionary<string, string>() { { "Name", name } });

                Assert.AreEqual(name, dbRecipe.Name);
                Assert.AreEqual(category, dbRecipe.Category);
                List<IItem> items = dbRecipe.
            }
            finally
            {
                recipe.Destroy();
            }
        }

        #endregion
    }
}
