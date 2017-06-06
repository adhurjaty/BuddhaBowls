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
                Count = 2,
                Yield = 1
            };

            Assert.AreEqual(2, inv.GetCost());
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

                List<InventoryItem> newItemList = vend.ItemList;

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
            Assert.IsNull(vend.ItemList);
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
                vend.ClearAndUpdate(itemList);
                List<InventoryItem> newList = vend.ItemList;

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
                List<InventoryItem> newList = vend.ItemList;

                Assert.IsNotNull(newList.First(x => x.Name == "Chopped Clams"));

                vend.RemoveInvItem(newItem);
                newList = vend.ItemList;
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
            VendorInventoryItem vi = new VendorInventoryItem(item);
            List<Vendor> vendors = vi.Vendors;

            vi.SelectedVendor = vendors[0];
            float tempPrice0 = vi.LastPurchasedPrice;
            vi.LastPurchasedPrice = 69.69f;

            try
            {
                vi.NotifyAllChanges();
                vi.Update();

                InventoryItem testItem = vendors[0].ItemList.First(x => x.Name == "Feta");
                Assert.AreEqual(69.69f, testItem.LastPurchasedPrice);

                vi.SelectedVendor = vendors[1];
                Assert.AreNotEqual(69.69f, vi.LastPurchasedPrice);
            }
            finally
            {
                vi.SelectedVendor = vendors[0];
                vi.LastPurchasedPrice = tempPrice0;
                vi.NotifyAllChanges();
            }
        }

        [TestMethod]
        public void ToInventoryItemTest()
        {
            ModelContainer models = new ModelContainer();
            InventoryItem item = models.InventoryItems.First(x => x.Name == "Feta");
            VendorInventoryItem vi = new VendorInventoryItem(item);

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
                List<InventoryItem> berryManItems = berryMan.ItemList;
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
                Assert.AreEqual(DateTime.Today, ((DateTime)po.ReceivedDate).Date);
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
                Assert.AreEqual(DateTime.Today, ((DateTime)po.ReceivedDate).Date);
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
            List<IItem> recipeItems = new List<IItem>()
            {
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Avocado" } }),
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Black Beans" } }),
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Cinnamon" } }),
                new Recipe(new Dictionary<string, string>() { { "Name", "Chopped Clams" } })
            };

            try
            {
                recipe.Insert(recipeItems);
                Recipe dbRecipe = new Recipe(new Dictionary<string, string>() { { "Name", name } });

                Assert.AreEqual(name, dbRecipe.Name);
                Assert.AreEqual(category, dbRecipe.Category);
                CollectionAssert.AreEquivalent(recipeItems.Select(x => x.Name).ToList(), recipe.ItemList.Select(x => x.Name).ToList());
            }
            finally
            {
                recipe.Destroy();
            }

            Recipe emptyRecipe = new Recipe(new Dictionary<string, string>() { { "Name", name } });
            Assert.IsNull(emptyRecipe.Name);
            Assert.IsFalse(File.Exists(emptyRecipe.GetRecipeTablePath()));
        }

        [TestMethod]
        public void InsertAndDeleteEmptyRecipeTest()
        {
            string name = "Test Recipe";
            string category = "Test Rec Category";
            Recipe recipe = new Recipe() { Name = name, Category = category, RecipeUnitConversion = 1 };

            try
            {
                recipe.Insert();
                Recipe dbRecipe = new Recipe(new Dictionary<string, string>() { { "Name", name } });
                // make sure no error occurs when trying to find items

                Assert.AreEqual(name, dbRecipe.Name);
                Assert.AreEqual(category, dbRecipe.Category);

                Assert.IsFalse(File.Exists(dbRecipe.GetRecipeTablePath()));
            }
            finally
            {
                recipe.Destroy();
            }

            Recipe emptyRecipe = new Recipe(new Dictionary<string, string>() { { "Name", name } });
            Assert.IsNull(emptyRecipe.Name);
        }

        [TestMethod]
        public void UpdateRecipeTest()
        {
            string name = "Test Recipe";
            string category = "Test Rec Category";
            Recipe recipe = new Recipe() { Name = name, Category = category, RecipeUnitConversion = 1 };
            List<IItem> recipeItems = new List<IItem>()
            {
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Avocado" } }),
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Black Beans" } }),
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Cinnamon" } }),
                new Recipe(new Dictionary<string, string>() { { "Name", "Chopped Clams" } })
            };

            recipe.Insert(recipeItems);

            recipeItems = new List<IItem>()
            {
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Eggs" } }),
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Feta" } }),
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Flour" } }),
                new Recipe(new Dictionary<string, string>() { { "Name", "Granola" } })
            };

            string newCategory = "New Category";
            recipe.Category = newCategory;

            try
            {
                recipe.Update(recipeItems);

                Recipe dbRecipe = new Recipe(new Dictionary<string, string>() { { "Name", name } });

                Assert.AreEqual(newCategory, dbRecipe.Category);
                CollectionAssert.AreEquivalent(recipeItems.Select(x => x.Name).ToList(), recipe.ItemList.Select(x => x.Name).ToList());
            }
            finally
            {
                recipe.Destroy();
            }
        }

        [TestMethod]
        public void UpdateRecipeEmptyTest()
        {
            string name = "Test Recipe";
            string category = "Test Rec Category";
            Recipe recipe = new Recipe() { Name = name, Category = category, RecipeUnitConversion = 1 };
            List<IItem> recipeItems = new List<IItem>()
            {
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Avocado" } }),
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Black Beans" } }),
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Cinnamon" } }),
                new Recipe(new Dictionary<string, string>() { { "Name", "Chopped Clams" } })
            };

            recipe.Insert(recipeItems);

            string newCategory = "New Category";
            recipe.Category = newCategory;

            try
            {
                recipe.Update();

                Recipe dbRecipe = new Recipe(new Dictionary<string, string>() { { "Name", name } });

                Assert.AreEqual(newCategory, dbRecipe.Category);
                CollectionAssert.AreEquivalent(recipeItems.Select(x => x.Name).ToList(), recipe.ItemList.Select(x => x.Name).ToList());
            }
            finally
            {
                recipe.Destroy();
            }
        }

        [TestMethod]
        public void GetRecipeTest()
        {
            Recipe recipe = new Recipe(new Dictionary<string, string>() { { "Name", "New England Clam Chowder" } });

            Dictionary<string, float> desired = new Dictionary<string, float>()
            {
                { "Butter", 16 },
                { "Heavy Cream", 8 },
                { "Chopped Clams", 1.5f },
                { "Clam Juice", 2 },
                { "Flour", 6 },
                { "Thyme", 3 },
                { "Pepper", 3 },
                { "Salt", 6 },
                { "Old Bay Seasoning", 1.5f },
                { "Yellow Onions", 100 },
                { "Potatoes", 120 },
                { "Celery", 2 }
            };

            foreach (IItem item in recipe.ItemList)
            {
                Assert.AreEqual(desired[item.Name], item.Count);
            }
        }

        [TestMethod]
        public void GetBatchItemCostTest()
        {
            Recipe clam = new Recipe(new Dictionary<string, string>() { { "Name", "New England Clam Chowder" } });

            float cost = clam.RecipeCost;

            Assert.AreEqual(9.37f, Math.Round(cost, 2));
        }

        [TestMethod]
        public void GetCategoryCostTest()
        {
            Recipe clam = new Recipe(new Dictionary<string, string>() { { "Name", "New England Clam Chowder" } });

            Dictionary<string, float> catCost = clam.GetCategoryCosts();

            Dictionary<string, float> refCosts = new Dictionary<string, float>()
            {
                { "Dairy", 2.49f },
                { "Dry Goods", 0.32f },
                { "Herbs", 0.15f },
                { "Produce", 6.41f }
            };

            foreach (string key in catCost.Keys)
            {
                float refCost = refCosts[key];
                float testCost = (float)Math.Round(catCost[key], 2);
                Assert.AreEqual(refCost, testCost);
            }

            float totalCost = (float)Math.Round(new List<float>(catCost.Values).Sum(), 2);
            Assert.AreEqual(9.37f, totalCost);
        }

        [TestMethod]
        public void GetComplexRecipeCostTest()
        {
            Recipe recipe = new Recipe() { Name = "Test Recipe", IsBatch = true };
            List<IItem> desired = CreateComplexRecipe();

            try
            {
                recipe.Insert(desired);
                Assert.AreEqual(16.06, Math.Round(recipe.RecipeCost, 2));
            }
            finally
            {
                recipe.Destroy();
            }
        }

        [TestMethod]
        public void GetComplexRecipeCategoryCostTest()
        {
            Recipe recipe = new Recipe() { Name = "Test Recipe", IsBatch = true };
            List<IItem> desired = CreateComplexRecipe();

            Dictionary<string, float> refCosts = new Dictionary<string, float>()
            {
                { "Dairy", 2.17f },
                { "Dry Goods", 3.74f },
                { "Herbs", 0.03f },
                { "Produce", 8.18f }
            };

            try
            {
                recipe.Insert(desired);
                Dictionary<string, float> catCost = recipe.GetCategoryCosts();
                foreach (string key in catCost.Keys)
                {
                    float refCost = refCosts[key];
                    float testCost = (float)Math.Round(catCost[key], 2);
                    Assert.AreEqual(refCost, testCost);
                }
            }
            finally
            {
                recipe.Destroy();
            }
        }
        #endregion

        #region BreadOrder Tests

        [TestMethod]
        public void BreadOrderGetDictTest()
        {
            string breadDescStr = "Id=0;Name=test;BeginInventory=9;Delivery=8|Id=1;Name=other test;BeginInventory=1;Delivery=4";
            BreadOrder bo = new BreadOrder() { Date = DateTime.Today, BreadDescDBString = breadDescStr };

            Dictionary<string, BreadDescriptor> refBdList = new Dictionary<string, BreadDescriptor>()
            {
                { "test", new BreadDescriptor(bo) { Id=0, Name = "test", BeginInventory = 9, Delivery = 8 } },
                { "other test", new BreadDescriptor(bo) { Id=0, Name = "other test", BeginInventory = 1, Delivery = 4 } }
            };

            Assert.AreEqual(refBdList.Count, bo.BreadDescDict.Count);

            foreach (KeyValuePair<string, BreadDescriptor> kvp in refBdList)
            {
                foreach (string prop in kvp.Value.GetProperties())
                {
                    Assert.AreEqual(kvp.Value.GetPropertyValue(prop), bo.BreadDescDict[kvp.Key].GetPropertyValue(prop));
                }
            }
        }

        [TestMethod]
        public void BreadOrderSetDictStrTest()
        {
            string breadDescStr = "Name=test;BeginInventory=9;Delivery=8;Id=0|Name=other test;BeginInventory=1;Delivery=4;Id=1";
            BreadOrder bo = new BreadOrder() { Date = DateTime.Today, BreadDescDBString = breadDescStr };

            Assert.AreEqual(breadDescStr, bo.BreadDescToStr());
        }

        #endregion

        private List<IItem> CreateComplexRecipe()
        {
            return new List<IItem>()
            {
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Butter" } }) { Count = 16 },
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Heavy Cream" } }) { Count = 8 },
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Chopped Clams" } }) { Count = 1.5f },
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Clam Juice" } }) { Count = 2 },
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Flour" } }) { Count = 6 },
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Thyme" } }) { Count = 3 },
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Pepper" } }) { Count = 3 },
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Salt" } }) { Count = 6 },
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Old Bay Seasoning" } }) { Count = 1.5f },
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Yellow Onions" } }) { Count = 100 },
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Potatoes" } }) { Count = 120 },
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Celery" } }) { Count = 2 },
                new Recipe(new Dictionary<string, string>() { { "Name", "New England Clam Chowder" } }) { Count = 1 },
            };
        }
    }
}
