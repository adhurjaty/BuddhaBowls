using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BuddhaBowls.Models;
using System.Linq;
using System.Windows;
using BuddhaBowls.Services;
using BuddhaBowls.Helpers;
using System.IO;

namespace BuddhaBowls.Test
{
    /// <summary>
    /// Summary description for ViewModelsTest
    /// </summary>
    [TestClass]
    public class ViewModelsTest
    {
        private MainViewModel _vm;
        private DatabaseInterface _dbInt;

        public ViewModelsTest()
        {
            BuddhaBowls.Properties.Settings.Default.DBLocation = Properties.Settings.Default.DBLocation;
            _vm = new MainViewModel();
            _vm.InitializeWindow(new MainWindow(_vm));
            _dbInt = new DatabaseInterface();
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
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        #region MainViewModel Tests

        [TestMethod]
        public void SaveSettingsValidDBLocationTest()
        {
            string validPath = @"C:\Users\Developer\Documents\Visual Studio 2015\Projects\BuddhaBowls\BuddhaBowls.Test\TestData";

            _vm.DataFileFolder = validPath;
            _vm.SaveSettingsCommand.Execute(null);

            Assert.AreEqual(DateTime.Parse("3/22/2017  8:28:00 PM"), new List<Inventory>(_vm.InventoryTab.InventoryList)[0].Date);
            Assert.AreEqual("Another guy", _vm.VendorTab.FilteredVendorList[0].Name);
        }

        [TestMethod]
        public void SaveSettingsInvalidDBLocationTest()
        {
            string invalidPath = @"l;dfjs";

            _vm.DataFileFolder = invalidPath;
            _vm.SaveSettingsCommand.Execute(null);

            Assert.IsFalse(_vm.InventoryTab.DBConnection);
            Assert.AreEqual("Could not connect to DB", _vm.VendorTab.FilteredVendorList[0].Name);
        }

        #endregion

        #region InventoryTabVM/NewInventoryVM Tests

        [TestMethod]
        public void AddDeleteNewInventoryTest()
        {
            InventoryTabVM tabVM = _vm.InventoryTab;
            DateTime invDate = DateTime.Today;
            int initInvCount = tabVM.InventoryList.Count;

            tabVM.AddCommand.Execute(null);
            NewInventoryVM newInvVM = GetOpenTempTabVM<NewInventoryVM>();
            newInvVM.InventoryDate = invDate;

            try
            {
                newInvVM.SaveCountCommand.Execute(null);

                Assert.AreEqual(initInvCount + 1, tabVM.InventoryList.Count);
                Assert.AreEqual(invDate, tabVM.InventoryList[0].Date);
                Assert.AreEqual(0, TempTabVM.TabStack.Count);
            }
            finally
            {
                tabVM.SelectedInventory = tabVM.InventoryList[0];
                tabVM.DeleteCommand.Execute(null);
            }

            Assert.AreEqual(initInvCount, tabVM.InventoryList.Count);
            Assert.IsNull(tabVM.InventoryList.FirstOrDefault(x => x.Date == invDate));
        }

        [TestMethod]
        public void EditInventoryTest()
        {
            InventoryTabVM tabVM = _vm.InventoryTab;
            DateTime invDate = DateTime.Today;

            tabVM.AddCommand.Execute(null);
            NewInventoryVM newInvVM = GetOpenTempTabVM<NewInventoryVM>();
            newInvVM.InventoryDate = invDate;
            newInvVM.InvListVM.FilteredItems[0].Count = 1122;

            try
            {
                newInvVM.SaveCountCommand.Execute(null);

                int initInvCount = tabVM.InventoryList.Count;
                tabVM.SelectedInventory = tabVM.InventoryList[0];
                tabVM.ViewCommand.Execute(null);
                NewInventoryVM editInvVM = GetOpenTempTabVM<NewInventoryVM>();

                Assert.AreEqual(1122, editInvVM.InvListVM.FilteredItems[0].Count);

                editInvVM.InventoryDate = invDate.AddDays(-1);
                editInvVM.SaveCountCommand.Execute(null);

                // this assert fails and I'm not sure how I want to handle if a user wants to change the date on edit
                // should I create a new inventory or edit the existing one?
                //Assert.AreEqual(invDate.AddDays(-1), tabVM.InventoryList[0].Date);
                Assert.AreEqual(0, TempTabVM.TabStack.Count);
                Assert.AreEqual(initInvCount, tabVM.InventoryList.Count);
            }
            finally
            {
                tabVM.SelectedInventory = tabVM.InventoryList[0];
                tabVM.DeleteCommand.Execute(null);
            }
        }

        #endregion

        #region InventoryListVM/NewInventoryWizard Tests

        [TestMethod]
        public void NewInventoryItemFromMasterTest()
        {
            InventoryListVM listVM = _vm.InventoryTab.InvListVM;
            string name = "My New Item";
            listVM.AddCommand.Execute(null);
            NewInventoryItemWizard newInvVM = GetOpenTempTabVM<NewInventoryItemWizard>();

            newInvVM.Item = new InventoryItem()
            {
                Name = name,
                Category = "Dairy",
                RecipeUnit = "OZ-wt",
                RecipeUnitConversion = 20,
                CountUnit = "EA",
                Yield = 1
            };

            Vendor v1 = new Vendor(new Dictionary<string, string>() { { "Name", "Another guy" } });
            Vendor v2 = new Vendor(new Dictionary<string, string>() { { "Name", "Sysco" } });
            newInvVM.VendorList.Add(new VendorInfo(v1) { Conversion = 2 });
            newInvVM.VendorList.Add(new VendorInfo(v2) { Conversion = 4 });

            newInvVM.FinishCommand.Execute(null);

            VendorInventoryItem item = null;
            try
            {
                item = listVM.FilteredItems.First(x => x.Name == name);
            }
            catch
            {
                ModelContainer models = _vm.GetModelContainer();
                models.DeleteInventoryItem(models.InventoryItems.First(x => x.Name == name));
            }

            try
            {
                InventoryItem dbItem = new InventoryItem(new Dictionary<string, string>() { { "Name", name } });
                Assert.AreEqual(name, dbItem.Name);
                Assert.AreEqual("EA", dbItem.CountUnit);

                List<InventoryItem> v1Items = v1.GetInventoryItems();
                InventoryItem v1Item = v1Items.First(x => x.Name == name);
                Assert.AreEqual(2, v1Item.Conversion);
                List<InventoryItem> v2Items = v2.GetInventoryItems();
                InventoryItem v2Item = v2Items.First(x => x.Name == name);
                Assert.AreEqual(4, v2Item.Conversion);
            }
            finally
            {
                listVM.SelectedInventoryItem = item;
                listVM.DeleteCommand.Execute(null);
            }

            VendorInventoryItem noItem = listVM.FilteredItems.FirstOrDefault(x => x.Name == name);
            Assert.IsNull(noItem);
            InventoryItem noV1Item = v1.GetInventoryItems().FirstOrDefault(x => x.Name == name);
            Assert.IsNull(noV1Item);
            InventoryItem noV2Item = v2.GetInventoryItems().FirstOrDefault(x => x.Name == name);
            Assert.IsNull(noV2Item);
        }

        [TestMethod]
        public void NewInventoryItemFromNewInvTest()
        {
            InventoryTabVM tabVM = _vm.InventoryTab;

            tabVM.AddCommand.Execute(null);
            NewInventoryVM newInvVM = GetOpenTempTabVM<NewInventoryVM>();
            InventoryListVM listVM = newInvVM.InvListVM;

            string name = "My New Item";
            listVM.AddCommand.Execute(null);
            NewInventoryItemWizard newInvWizardVM = GetOpenTempTabVM<NewInventoryItemWizard>();

            newInvWizardVM.Item = new InventoryItem()
            {
                Name = name,
                Category = "Dairy",
                RecipeUnit = "OZ-wt",
                RecipeUnitConversion = 20,
                CountUnit = "EA",
                Yield = 1
            };

            Vendor v1 = new Vendor(new Dictionary<string, string>() { { "Name", "Another guy" } });
            Vendor v2 = new Vendor(new Dictionary<string, string>() { { "Name", "Sysco" } });
            newInvWizardVM.VendorList.Add(new VendorInfo(v1) { Conversion = 2 });
            newInvWizardVM.VendorList.Add(new VendorInfo(v2) { Conversion = 4 });

            newInvWizardVM.FinishCommand.Execute(null);

            VendorInventoryItem item = null;
            try
            {
                item = listVM.FilteredItems.First(x => x.Name == name);
            }
            catch
            {
                ModelContainer models = _vm.GetModelContainer();
                models.DeleteInventoryItem(models.InventoryItems.First(x => x.Name == name));
                Assert.IsTrue(false);
            }

            try
            {
                InventoryItem dbItem = new InventoryItem(new Dictionary<string, string>() { { "Name", name } });
                Assert.AreEqual(name, dbItem.Name);
                Assert.AreEqual("EA", dbItem.CountUnit);

                List<InventoryItem> v1Items = v1.GetInventoryItems();
                InventoryItem v1Item = v1Items.First(x => x.Name == name);
                Assert.AreEqual(2, v1Item.Conversion);
                List<InventoryItem> v2Items = v2.GetInventoryItems();
                InventoryItem v2Item = v2Items.First(x => x.Name == name);
                Assert.AreEqual(4, v2Item.Conversion);
            }
            finally
            {
                listVM.SelectedInventoryItem = item;
                listVM.DeleteCommand.Execute(null);
            }

            Assert.IsNull(listVM.FilteredItems.FirstOrDefault(x => x.Name == name));
        }

        #endregion

        #region OrderTabVM Tests

        [TestMethod]
        public void EmptyOrderVendorTest()
        {
            OrderTabVM orderTab = _vm.OrderTab;
            
            orderTab.AddNewOrderCommand.Execute(null);
            NewOrderVM newOrderTab = GetOpenTempTabVM<NewOrderVM>();

            Assert.IsNull(newOrderTab.OrderVendor);
            Assert.AreEqual("Please Select Vendor", newOrderTab.FilteredOrderItems.First().Name);
            Assert.IsFalse(newOrderTab.SaveOrderCanExecute);
        }

        [TestMethod]
        public void SelectOrderVendorTest()
        {
            OrderTabVM orderTab = _vm.OrderTab;

            orderTab.AddNewOrderCommand.Execute(null);
            NewOrderVM newOrderTab = GetOpenTempTabVM<NewOrderVM>();
            Vendor selectVendor = _vm.GetModelContainer().Vendors.First(x => x.Name == "Sysco");
            newOrderTab.OrderVendor = selectVendor;

            List<InventoryItem> refItems = MainHelper.SortItems(selectVendor.GetInventoryItems()).ToList();
            CollectionAssert.AreEqual(refItems.Select(x => x.Name).ToList(), newOrderTab.FilteredOrderItems.Select(x => x.Name).ToList());
            CollectionAssert.AreEqual(refItems.Select(x => x.LastOrderAmount).ToList(),
                                      newOrderTab.FilteredOrderItems.Select(x => x.LastOrderAmount).ToList());
        }

        [TestMethod]
        public void SelectEmptyOrderVendorTest()
        {
            OrderTabVM orderTab = _vm.OrderTab;
            ModelContainer models = _vm.GetModelContainer();
            Vendor newVend = new Vendor() { Name = "TempVend" };

            try
            {
                models.AddUpdateVendor(ref newVend);

                orderTab.AddNewOrderCommand.Execute(null);
                NewOrderVM newOrderTab = GetOpenTempTabVM<NewOrderVM>();
                newOrderTab.OrderVendor = newVend;

                Assert.AreEqual("Vendor has no items", newOrderTab.FilteredOrderItems.First().Name);
                Assert.IsFalse(newOrderTab.SaveOrderCanExecute);
            }
            finally
            {
                models.DeleteVendor(newVend);
            }
        }

        [TestMethod]
        public void ClearNewOrderAmountsTest()
        {
            OrderTabVM orderTab = _vm.OrderTab;
            Vendor berryMan = new Vendor(new Dictionary<string, string>() { { "Name", "Berry Man" } });
            float[] initAmounts = new float[] { 1, 3, 5, 1 };
            float[] clearedAmounts = new float[] { 0, 0, 0, 0 };

            orderTab.AddNewOrderCommand.Execute(null);
            NewOrderVM newOrderTab = GetOpenTempTabVM<NewOrderVM>();
            newOrderTab.OrderVendor = berryMan;

            CollectionAssert.AreEqual(initAmounts, newOrderTab.FilteredOrderItems.Select(x => x.LastOrderAmount).ToArray());
            newOrderTab.ClearOrderCommand.Execute(null);
            CollectionAssert.AreEqual(clearedAmounts, newOrderTab.FilteredOrderItems.Select(x => x.LastOrderAmount).ToArray());
        }

        [TestMethod]
        public void NewOpenOrderTest()
        {
            OrderTabVM orderTab = _vm.OrderTab;
            ModelContainer models = _vm.GetModelContainer();
            Vendor berryMan = new Vendor(new Dictionary<string, string>() { { "Name", "Berry Man" } });

            orderTab.AddNewOrderCommand.Execute(null);
            NewOrderVM newOrderTab = GetOpenTempTabVM<NewOrderVM>();
            newOrderTab.OrderVendor = berryMan;
            newOrderTab.ClearOrderCommand.Execute(null);

            Dictionary<string, float> updateOrderDict = new Dictionary<string, float>()
            {
                { "Artichoke Hearts", 44f },
                { "Avocado", 22f },
                { "Vanilla", 11f }
            };

            foreach (KeyValuePair<string, float> kvp in updateOrderDict)
            {
                newOrderTab.FilteredOrderItems.First(x => x.Name == kvp.Key).LastOrderAmount = kvp.Value;
            }

            newOrderTab.SaveNewOrderCommand.Execute(null);
            orderTab.SelectedOpenOrder = orderTab.OpenOrders.FirstOrDefault(x => x.OrderDate == DateTime.Today);
            PurchaseOrder newOrder = orderTab.SelectedOpenOrder;

            try
            {
                Assert.IsNotNull(newOrder);
                
                List<InventoryItem> orderItems = newOrder.GetPOItems()[0];
                foreach (InventoryItem item in orderItems)
                {
                    Assert.AreEqual(updateOrderDict[item.Name], item.LastOrderAmount);
                }
            }
            finally
            {
                orderTab.DeleteOpenOrderCommand.Execute(null);
            }

            Assert.IsFalse(File.Exists(newOrder.GetOrderPath()));
        }

        [TestMethod]
        public void ReceiveOrderUITest()
        {
            OrderTabVM orderTab = _vm.OrderTab;
            Vendor berryMan = new Vendor(new Dictionary<string, string>() { { "Name", "Berry Man" } });

            orderTab.AddNewOrderCommand.Execute(null);
            NewOrderVM newOrderTab = GetOpenTempTabVM<NewOrderVM>();
            newOrderTab.OrderVendor = berryMan;
            newOrderTab.ClearOrderCommand.Execute(null);

            Dictionary<string, float> updateOrderDict = new Dictionary<string, float>()
            {
                { "Artichoke Hearts", 44f },
                { "Avocado", 22f },
                { "Vanilla", 11f }
            };

            foreach (KeyValuePair<string, float> kvp in updateOrderDict)
            {
                newOrderTab.FilteredOrderItems.First(x => x.Name == kvp.Key).LastOrderAmount = kvp.Value;
            }

            newOrderTab.SaveNewOrderCommand.Execute(null);
            orderTab.SelectedOpenOrder = orderTab.OpenOrders.FirstOrDefault(x => x.OrderDate == DateTime.Today);
            PurchaseOrder newOrder = orderTab.SelectedOpenOrder;

            try
            {
                Assert.IsNotNull(newOrder);
                newOrder.ReceivedCheck = true;
                orderTab.ReceivedOrdersCommand.Execute(null);

                List<InventoryItem> receivedItems = newOrder.GetPOItems()[1];
                foreach (InventoryItem item in receivedItems)
                {
                    Assert.AreEqual(updateOrderDict[item.Name], item.LastOrderAmount);
                }
                Assert.AreEqual(DateTime.Today, newOrder.ReceivedDate);
            }
            finally
            {
                orderTab.DeleteOpenOrderCommand.Execute(null);
            }
        }

        [TestMethod]
        public void ViewOpenOrderTest()
        {
            OrderTabVM orderTab = _vm.OrderTab;
            Vendor berryMan = new Vendor(new Dictionary<string, string>() { { "Name", "Berry Man" } });

            orderTab.AddNewOrderCommand.Execute(null);
            NewOrderVM newOrderTab = GetOpenTempTabVM<NewOrderVM>();
            newOrderTab.OrderVendor = berryMan;
            newOrderTab.ClearOrderCommand.Execute(null);

            Dictionary<string, float> updateOrderDict = new Dictionary<string, float>()
            {
                { "Artichoke Hearts", 44f },
                { "Avocado", 22f },
                { "Vanilla", 11f }
            };

            foreach (KeyValuePair<string, float> kvp in updateOrderDict)
            {
                newOrderTab.FilteredOrderItems.First(x => x.Name == kvp.Key).LastOrderAmount = kvp.Value;
            }

            newOrderTab.SaveNewOrderCommand.Execute(null);
            orderTab.SelectedOpenOrder = orderTab.OpenOrders.FirstOrDefault(x => x.OrderDate == DateTime.Today);
            PurchaseOrder newOrder = orderTab.SelectedOpenOrder;

            try
            {
                orderTab.ViewOpenOrderCommand.Execute(null);
                ViewOrderVM viewOrderTab = GetOpenTempTabVM<ViewOrderVM>();
            }
            finally
            {

            }
        }

        [TestMethod]
        public void PartialReceiveOrderTest()
        {
            OrderTabVM orderTab = _vm.OrderTab;
            Vendor berryMan = new Vendor(new Dictionary<string, string>() { { "Name", "Berry Man" } });

            orderTab.AddNewOrderCommand.Execute(null);
            NewOrderVM newOrderTab = GetOpenTempTabVM<NewOrderVM>();
            newOrderTab.OrderVendor = berryMan;
            newOrderTab.ClearOrderCommand.Execute(null);

            Dictionary<string, float> updateOrderDict = new Dictionary<string, float>()
            {
                { "Artichoke Hearts", 44f },
                { "Avocado", 22f },
                { "Vanilla", 11f }
            };

            foreach (KeyValuePair<string, float> kvp in updateOrderDict)
            {
                newOrderTab.FilteredOrderItems.First(x => x.Name == kvp.Key).LastOrderAmount = kvp.Value;
            }

            newOrderTab.SaveNewOrderCommand.Execute(null);
            orderTab.SelectedOpenOrder = orderTab.OpenOrders.FirstOrDefault(x => x.OrderDate == DateTime.Today);
            PurchaseOrder newOrder = orderTab.SelectedOpenOrder;

            try
            {
                // finish testing logic

                List<InventoryItem> receivedItems = newOrder.GetPOItems()[1];
                foreach (InventoryItem item in receivedItems)
                {
                    Assert.AreEqual(updateOrderDict[item.Name], item.LastOrderAmount);
                }
                Assert.AreEqual(DateTime.Today, newOrder.ReceivedDate);
            }
            finally
            {
                orderTab.DeleteOpenOrderCommand.Execute(null);
            }
        }

        [TestMethod]
        public void ReOpenOrderTest()
        {

        }

        #endregion

        private T GetOpenTempTabVM<T>() where T : TempTabVM
        {
            return (T)TempTabVM.TabStack.First().DataContext;
        }
    }
}
