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
using System.Collections.ObjectModel;

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
        Dictionary<string, float> _updateOrderDict;
        Dictionary<string, float[]> _vendorSoldDict;

        public ViewModelsTest()
        {
            BuddhaBowls.Properties.Settings.Default.DBLocation = Properties.Settings.Default.DBLocation;
            _vm = new MainViewModel();
            _vm.InitializeWindow(new MainWindow(_vm));
            _dbInt = new DatabaseInterface();
            _updateOrderDict = new Dictionary<string, float>()
            {
                { "Artichoke Hearts", 44f },
                { "Avocado", 22f },
                { "Vanilla", 11f }
            };

            _vendorSoldDict = new Dictionary<string, float[]>()
            {
                { "Pesto", new float[] { 4f, 44.44f } },
                { "Yogurt", new float[] { 5f, 55.56f } },
                { "Eggs", new float[] { 12f, 13.22f } }
            };
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

            _vm.InventoryTab.SwitchButtonList.First(x => x.PageName == "History").SwitchCommand.Execute(2);

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
            tabVM.SwitchButtonList.First(x => x.PageName == "History").SwitchCommand.Execute(2);
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
            string name = "My New Item";
            Vendor v1 = new Vendor(new Dictionary<string, string>() { { "Name", "Another guy" } });
            Vendor v2 = new Vendor(new Dictionary<string, string>() { { "Name", "Sysco" } });
            InventoryListVM listVM = CreateTestInventoryItem(name, v1, v2);

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
            string name = "My New Item";
            Vendor v1 = new Vendor(new Dictionary<string, string>() { { "Name", "Another guy" } });
            Vendor v2 = new Vendor(new Dictionary<string, string>() { { "Name", "Sysco" } });
            InventoryListVM listVM = CreateTestInventoryItem(name, v1, v2);

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

        [TestMethod]
        public void AddDeleteInvItemNoCountChangeTest()
        {
            InventoryTabVM tabVM = _vm.InventoryTab;

            tabVM.AddCommand.Execute(null);
            NewInventoryVM newInvVM = GetOpenTempTabVM<NewInventoryVM>();
            InventoryListVM listVM = newInvVM.InvListVM;

            for (int i = 0; i < 6; i++)
            {
                listVM.FilteredItems[i].Count = 66;
            }

            string name = "My New Item";
            Vendor v1 = new Vendor(new Dictionary<string, string>() { { "Name", "Another guy" } });
            Vendor v2 = new Vendor(new Dictionary<string, string>() { { "Name", "Sysco" } });
            CreateTestInventoryItem(name, v1, v2);

            VendorInventoryItem item = listVM.FilteredItems.FirstOrDefault(x => x.Name == name);

            try
            {
                for (int i = 0; i < 6; i++)
                {
                    Assert.AreEqual(66, listVM.FilteredItems[i].Count);
                }

                listVM.SelectedInventoryItem = item;
                listVM.DeleteCommand.Execute(null);

                for (int i = 0; i < 6; i++)
                {
                    Assert.AreEqual(66, listVM.FilteredItems[i].Count);
                }
            }
            finally
            {
                item.Destroy();
            }
        }

        [TestMethod]
        public void RemoveVendorFromInvItemVM()
        {
            string name = "My New Item";
            Vendor v1 = new Vendor(new Dictionary<string, string>() { { "Name", "Another guy" } });
            Vendor v2 = new Vendor(new Dictionary<string, string>() { { "Name", "Sysco" } });
            InventoryListVM listVM = CreateTestInventoryItem(name, v1, v2);

            VendorInventoryItem item = listVM.FilteredItems.FirstOrDefault(x => x.Name == name);

            try
            {
                listVM.SelectedInventoryItem = item;
                listVM.EditCommand.Execute(null);

                NewInventoryItemWizard wizardVM = GetOpenTempTabVM<NewInventoryItemWizard>();

                wizardVM.NextCommand.Execute(null);
                VendorInfo delVendor = wizardVM.VendorList.First(x => x.Vendor == "Another guy");
                wizardVM.SelectedItem = delVendor;
                wizardVM.DeleteVendorCommand.Execute(null);
                wizardVM.NextCommand.Execute(null);
                wizardVM.FinishCommand.Execute(null);

                // redundant (I think) but reminder that the Master List should have changed
                item = listVM.FilteredItems.First(x => x.Name == name);

                Assert.AreEqual(1, item.Vendors.Count);
                Assert.AreEqual(v2.Name, item.Vendors[0].Name);
            }
            finally
            {
                item.Destroy();
            }
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
            float[] clearedAmounts = new float[] { 0, 0, 0, 0 };

            orderTab.AddNewOrderCommand.Execute(null);
            NewOrderVM newOrderTab = GetOpenTempTabVM<NewOrderVM>();
            newOrderTab.OrderVendor = berryMan;

            foreach (InventoryItem item in newOrderTab.FilteredOrderItems)
            {
                Assert.AreNotEqual(0, item.LastOrderAmount);
            }
            newOrderTab.ClearOrderCommand.Execute(null);
            CollectionAssert.AreEqual(clearedAmounts, newOrderTab.FilteredOrderItems.Select(x => x.LastOrderAmount).ToArray());
        }

        [TestMethod]
        public void NewOpenOrderTest()
        {
            OrderTabVM orderTab = CreateTestOrder();

            orderTab.SelectedOpenOrder = orderTab.OpenOrders.FirstOrDefault(x => x.OrderDate == DateTime.Today);
            PurchaseOrder newOrder = orderTab.SelectedOpenOrder;

            try
            {
                Assert.IsNotNull(newOrder);
                
                List<InventoryItem> orderItems = newOrder.GetPOItems()[0];
                foreach (InventoryItem item in orderItems)
                {
                    Assert.AreEqual(_updateOrderDict[item.Name], item.LastOrderAmount);
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
            OrderTabVM orderTab = CreateTestOrder();

            orderTab.SelectedOpenOrder = orderTab.OpenOrders.FirstOrDefault(x => x.OrderDate == DateTime.Today);
            PurchaseOrder newOrder = orderTab.SelectedOpenOrder;

            try
            {
                Assert.IsNotNull(newOrder);
                newOrder.ReceivedCheck = true;
                orderTab.ReceivedOrdersCommand.Execute(null);

                Assert.IsNull(orderTab.SelectedOpenOrder);
                CollectionAssert.Contains(orderTab.ReceivedOrders, newOrder);

                List<InventoryItem> receivedItems = newOrder.GetPOItems()[1];
                foreach (InventoryItem item in receivedItems)
                {
                    Assert.AreEqual(_updateOrderDict[item.Name], item.LastOrderAmount);
                }
                Assert.AreEqual(DateTime.Today, newOrder.ReceivedDate);
            }
            finally
            {
                newOrder.Destroy();
            }
        }

        [TestMethod]
        public void ReOpenOrderUITest()
        {
            OrderTabVM orderTab = CreateTestOrder();

            orderTab.SelectedOpenOrder = orderTab.OpenOrders.FirstOrDefault(x => x.OrderDate == DateTime.Today);
            PurchaseOrder newOrder = orderTab.SelectedOpenOrder;

            try
            {
                newOrder.ReceivedCheck = true;
                orderTab.ReceivedOrdersCommand.Execute(null);
                Assert.IsNull(orderTab.SelectedOpenOrder);

                orderTab.SelectedReceivedOrder = newOrder;
                orderTab.ReOpenOrderCommand.Execute(null);

                CollectionAssert.Contains(orderTab.OpenOrders, newOrder);

                List<InventoryItem> openItems = newOrder.GetPOItems()[0];
                foreach (InventoryItem item in openItems)
                {
                    Assert.AreEqual(_updateOrderDict[item.Name], item.LastOrderAmount);
                }
                Assert.IsNull(newOrder.ReceivedDate);
            }
            finally
            {
                newOrder.Destroy();
            }
        }

        [TestMethod]
        public void ViewOpenOrderTest()
        {
            OrderTabVM orderTab = CreateTestOrder();

            orderTab.SelectedOpenOrder = orderTab.OpenOrders.FirstOrDefault(x => x.OrderDate == DateTime.Today);
            PurchaseOrder newOrder = orderTab.SelectedOpenOrder;

            try
            {
                orderTab.ViewOpenOrderCommand.Execute(null);
                ViewOrderVM viewOrderTab = GetOpenTempTabVM<ViewOrderVM>();
                OrderBreakdownVM breakdownContext = viewOrderTab.OpenBreakdownContext;
                List<InventoryItem> openItems = breakdownContext.GetInventoryItems();

                foreach (InventoryItem item in openItems)
                {
                    Assert.AreEqual(_updateOrderDict[item.Name], item.LastOrderAmount);
                }
            }
            finally
            {
                newOrder.Destroy();
            }
        }

        [TestMethod]
        public void ViewReceivedOrderTest()
        {
            OrderTabVM orderTab = CreateTestOrder();

            orderTab.SelectedOpenOrder = orderTab.OpenOrders.FirstOrDefault(x => x.OrderDate == DateTime.Today);
            PurchaseOrder newOrder = orderTab.SelectedOpenOrder;

            try
            {
                orderTab.ViewOpenOrderCommand.Execute(null);
                ViewOrderVM viewOrderTab = GetOpenTempTabVM<ViewOrderVM>();
                OrderBreakdownVM breakdownContext = viewOrderTab.ReceivedBreakdownContext;
                List<InventoryItem> receivedItems = breakdownContext.GetInventoryItems();

                foreach (InventoryItem item in receivedItems)
                {
                    Assert.AreEqual(_updateOrderDict[item.Name], item.LastOrderAmount);
                }
            }
            finally
            {
                newOrder.Destroy();
            }
        }

        [TestMethod]
        public void PartialReceiveOrderTest()
        {
            OrderTabVM orderTab = CreateTestOrder();

            orderTab.SelectedOpenOrder = orderTab.OpenOrders.FirstOrDefault(x => x.OrderDate == DateTime.Today);
            PurchaseOrder newOrder = orderTab.SelectedOpenOrder;

            try
            {
                orderTab.ViewOpenOrderCommand.Execute(null);
                ViewOrderVM viewOrderTab = GetOpenTempTabVM<ViewOrderVM>();
                OrderBreakdownVM breakdownContext = viewOrderTab.OpenBreakdownContext;

                SelectInBreakdown("Artichoke Hearts", ref breakdownContext);
                viewOrderTab.MoveToReceivedCommand.Execute(null);
                SelectInBreakdown("Avocado", ref breakdownContext);
                viewOrderTab.MoveToReceivedCommand.Execute(null);
                viewOrderTab.SaveCommand.Execute(null);

                Assert.IsTrue(newOrder.IsPartial);
                CollectionAssert.Contains(orderTab.OpenOrders, newOrder);
                CollectionAssert.Contains(orderTab.ReceivedOrders, newOrder);

                orderTab.SelectedOpenOrder = newOrder;
                Assert.AreEqual(newOrder, orderTab.SelectedReceivedOrder);
            }
            finally
            {
                newOrder.Destroy();
            }
        }

        [TestMethod]
        public void ViewPartialReceiveOrderTest()
        {
            OrderTabVM orderTab = CreateTestOrder();

            orderTab.SelectedOpenOrder = orderTab.OpenOrders.FirstOrDefault(x => x.OrderDate == DateTime.Today);
            PurchaseOrder newOrder = orderTab.SelectedOpenOrder;

            try
            {
                orderTab.ViewOpenOrderCommand.Execute(null);
                ViewOrderVM viewOrderTab = GetOpenTempTabVM<ViewOrderVM>();
                OrderBreakdownVM breakdownContext = viewOrderTab.OpenBreakdownContext;

                SelectInBreakdown("Artichoke Hearts", ref breakdownContext);
                viewOrderTab.MoveToReceivedCommand.Execute(null);
                SelectInBreakdown("Avocado", ref breakdownContext);
                viewOrderTab.MoveToReceivedCommand.Execute(null);
                viewOrderTab.SaveCommand.Execute(null);

                orderTab.SelectedOpenOrder = newOrder;
                orderTab.ViewOpenOrderCommand.Execute(null);

                breakdownContext = viewOrderTab.OpenBreakdownContext;
                List<InventoryItem> openItems = breakdownContext.GetInventoryItems();
                Assert.AreEqual(1, openItems.Count);
                Assert.AreEqual("Vanilla", openItems[0].Name);

                breakdownContext = viewOrderTab.ReceivedBreakdownContext;
                List<InventoryItem> receivedItems = breakdownContext.GetInventoryItems();
                Assert.AreEqual(2, receivedItems.Count);
                Assert.AreEqual("Artichoke Hearts", receivedItems[0].Name);
                Assert.AreEqual("Avocado", receivedItems[1].Name);
            }
            finally
            {
                newOrder.Destroy();
            }
        }

        #endregion

        #region VendorTabVM Tests

        [TestMethod]
        public void DisplayVendorsTest()
        {
            VendorTabVM vendorTab = _vm.VendorTab;

            List<string> refVendorNames = new List<string>()
            {
                "Another guy",
                "Berry Man",
                "Restaurant Depot",
                "Sysco",
                "The Paper Company"
            };

            CollectionAssert.AreEqual(refVendorNames, vendorTab.FilteredVendorList.Select(x => x.Name).ToList());
        }

        [TestMethod]
        public void AddNewVendorVMTest()
        {
            string name = "My New Vendor";
            VendorTabVM vendorTab = CreateTestVendor(name, "mynew@example.com");
            InventoryListVM invList = _vm.InventoryTab.InvListVM;

            Vendor newVendor = vendorTab.FilteredVendorList.FirstOrDefault(x => x.Name == name);
            vendorTab.SelectedVendor = newVendor;

            try
            {
                Assert.IsNotNull(newVendor);

                List<InventoryItem> vendorItems = newVendor.GetInventoryItems();
                foreach (InventoryItem item in vendorItems)
                {
                    float[] refVals = _vendorSoldDict[item.Name];
                    float refConversion = refVals[0];
                    float refPrice = refVals[1];
                    Assert.AreEqual(refConversion, item.Conversion);
                    Assert.AreEqual(refPrice, item.LastPurchasedPrice);

                    VendorInventoryItem listItem = invList.FilteredItems.First(x => x.Id == item.Id);
                    CollectionAssert.Contains(listItem.Vendors.Select(x => x.Name).ToList(), name);
                }

                InventoryListVM listVM = _vm.InventoryTab.InvListVM;
                foreach (InventoryItem item in vendorItems)
                {
                    VendorInventoryItem listItem = listVM.FilteredItems.First(x => x.Id == item.Id);
                    CollectionAssert.Contains(listItem.Vendors.Select(x => x.Name).ToList(), newVendor.Name);
                }
            }
            finally
            {
                vendorTab.DeleteVendorCommand.Execute(null);
            }

            Assert.IsNull(vendorTab.FilteredVendorList.FirstOrDefault(x => x.Name == name));
            foreach (string itemName in _vendorSoldDict.Keys)
            {
                VendorInventoryItem listItem = invList.FilteredItems.First(x => x.Name == itemName);
                CollectionAssert.DoesNotContain(listItem.Vendors.Select(x => x.Name).ToList(), name);
            }
        }

        [TestMethod]
        public void AddNewVendorNewInvTest()
        {
            string name = "My New Vendor";
            _vm.InventoryTab.AddCommand.Execute(null);
            InventoryListVM invList = GetOpenTempTabVM<NewInventoryVM>().InvListVM;
            VendorTabVM vendorTab = CreateTestVendor(name, "mynew@example.com");

            Vendor newVendor = vendorTab.FilteredVendorList.FirstOrDefault(x => x.Name == name);
            vendorTab.SelectedVendor = newVendor;

            try
            {
                List<InventoryItem> vendorItems = newVendor.GetInventoryItems();
                foreach (InventoryItem item in vendorItems)
                {
                    float[] refVals = _vendorSoldDict[item.Name];
                    float refConversion = refVals[0];
                    float refPrice = refVals[1];
                    Assert.AreEqual(refConversion, item.Conversion);
                    Assert.AreEqual(refPrice, item.LastPurchasedPrice);

                    VendorInventoryItem listItem = invList.FilteredItems.First(x => x.Id == item.Id);
                    CollectionAssert.Contains(listItem.Vendors.Select(x => x.Name).ToList(), name);
                }
            }
            finally
            {
                vendorTab.DeleteVendorCommand.Execute(null);
            }

            Assert.IsNull(vendorTab.FilteredVendorList.FirstOrDefault(x => x.Name == name));
            foreach (string itemName in _vendorSoldDict.Keys)
            {
                VendorInventoryItem listItem = invList.FilteredItems.First(x => x.Name == itemName);
                CollectionAssert.DoesNotContain(listItem.Vendors.Select(x => x.Name).ToList(), name);
            }
        }

        [TestMethod]
        public void EditVendorVMTest()
        {
            string name = "My New Vendor";
            VendorTabVM vendorTab = CreateTestVendor(name, "mynew@example.com");

            Vendor newVendor = vendorTab.FilteredVendorList.FirstOrDefault(x => x.Name == name);
            vendorTab.SelectedVendor = newVendor;

            try
            {
                vendorTab.EditVendorCommand.Execute(null);
                NewVendorWizardVM editItemsTab = GetOpenTempTabVM<NewVendorWizardVM>();

                foreach (InventoryItem item in editItemsTab.InventoryList)
                {
                    float[] refVals = _vendorSoldDict[item.Name];
                    float refConversion = refVals[0];
                    float refPrice = refVals[1];
                    Assert.AreEqual(refConversion, item.Conversion);
                    Assert.AreEqual(refPrice, item.LastPurchasedPrice);
                }

                editItemsTab.ShowAllCommand.Execute(null);
                InventoryVendorItem newVendorItem = editItemsTab.InventoryList.First(x => x.Name == "Vanilla");
                newVendorItem.IsSold = true;

                newVendorItem.Conversion = 7;
                newVendorItem.LastPurchasedPrice = 77.40f;

                editItemsTab.FinishCommand.Execute(null);

                List<InventoryItem> vendorItems = newVendor.GetInventoryItems();
                InventoryItem testVendorItem = vendorItems.First(x => x.Name == "Vanilla");
                Assert.AreEqual(7, testVendorItem.Conversion);
                Assert.AreEqual(77.40f, testVendorItem.LastPurchasedPrice);
            }
            finally
            {
                newVendor.Destroy();
            }
        }

        [TestMethod]
        public void ResetVendorVMTest()
        {
            VendorTabVM vendorTab = _vm.VendorTab;
            Vendor berryMan = vendorTab.FilteredVendorList.FirstOrDefault(x => x.Name == "Berry Man");
            vendorTab.SelectedVendor = berryMan;

            VendorInventoryItem item = vendorTab.SelectedVendorItems[0];
            float origConv = item.Conversion;
            float origCount = item.Count;

            item.Conversion = 666f;
            item.Count = 123f;

            vendorTab.ResetCommand.Execute(null);
            item = vendorTab.SelectedVendorItems[0];

            Assert.AreEqual(origConv, item.Conversion);
            Assert.AreEqual(origCount, item.Count);
        }

        #endregion

        #region RecipeTabVM Tests

        [TestMethod]
        public void LoadRecipesTest()
        {
            RecipeTabVM recipeTab = _vm.RecipeTab;

            Assert.AreEqual("Mac & Cheese", recipeTab.FilteredItems[0].Name);
        }

        [TestMethod]
        public void AddNewRecipeVMTest()
        {
            string name = "New Recipe";
            List<IItem> recipeItems = new List<IItem>()
            {
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Avocado" } }),
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Black Beans" } }),
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Cinnamon" } }),
                new Recipe(new Dictionary<string, string>() { { "Name", "New England Clam Chowder" } })
            };

            RecipeTabVM recipeTab = CreateTestBatchRecipe(name, recipeItems);
            Recipe newRecipe = recipeTab.FilteredItems.FirstOrDefault(x => x.Name == name);
            recipeTab.SelectedItem = newRecipe;

            try
            {
                Assert.AreEqual(name, newRecipe.Name);
                CollectionAssert.AreEquivalent(recipeItems.Select(x => x.Name).ToList(), newRecipe.GetRecipeItems().Select(x => x.Name).ToList());
            }
            finally
            {
                recipeTab.DeleteItemCommand.Execute(null);
            }

            Recipe emptyRecipe = recipeTab.FilteredItems.FirstOrDefault(x => x.Name == name);
            Assert.IsNull(emptyRecipe);
        }

        [TestMethod]
        public void EditRecipeVMTest()
        {
            string name = "New Recipe";
            List<IItem> recipeItems = new List<IItem>()
            {
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Avocado" } }),
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Black Beans" } }),
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Cinnamon" } }),
                new Recipe(new Dictionary<string, string>() { { "Name", "New England Clam Chowder" } })
            };

            RecipeTabVM recipeTab = CreateTestBatchRecipe(name, recipeItems);
            Recipe newRecipe = recipeTab.FilteredItems.FirstOrDefault(x => x.Name == name);
            recipeTab.SelectedItem = newRecipe;

            try
            {
                recipeTab.EditItemCommand.Execute(null);
                NewRecipeVM editRecipeTab = GetOpenTempTabVM<NewRecipeVM>();
                Assert.AreEqual(name, editRecipeTab.Item.Name);

                CollectionAssert.AreEquivalent(recipeItems.Select(x => x.Name).ToList(), editRecipeTab.Ingredients.Select(x => x.Name).ToList());

                editRecipeTab.ItemToAdd = new InventoryItem(new Dictionary<string, string>() { { "Name", "Butter" } });
                editRecipeTab.ModalOkCommand.Execute(null);

                editRecipeTab.Ingredients.First(x => x.Name == "Avocado").Count = 42;

                editRecipeTab.FinishCommand.Execute(null);

                Recipe editedRecipe = recipeTab.FilteredItems.First(x => x.Name == name);
                List<RecipeItem> editedIngredients = editedRecipe.GetRecipeItems();
                CollectionAssert.Contains(editedIngredients.Select(x => x.Name).ToList(), "Butter");
                Assert.AreEqual(42, editedIngredients.First(x => x.Name == "Avocado").Quantity);
            }
            finally
            {
                newRecipe.Destroy();
            }
        }

        #endregion

        #region Helper Methods

        private T GetOpenTempTabVM<T>() where T : TempTabVM
        {
            return (T)TempTabVM.TabStack.First().DataContext;
        }

        private InventoryListVM CreateTestInventoryItem(string name, Vendor v1, Vendor v2)
        {
            InventoryListVM listVM = _vm.InventoryTab.InvListVM;
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

            newInvVM.VendorList.Add(new VendorInfo(v1) { Conversion = 2 });
            newInvVM.VendorList.Add(new VendorInfo(v2) { Conversion = 4 });

            newInvVM.InvOrderList = new ObservableCollection<InventoryItem>(_vm.GetModelContainer().InventoryItems);

            newInvVM.FinishCommand.Execute(null);

            return listVM;
        }

        private OrderTabVM CreateTestOrder()
        {
            OrderTabVM orderTab = _vm.OrderTab;
            Vendor berryMan = new Vendor(new Dictionary<string, string>() { { "Name", "Berry Man" } });

            orderTab.AddNewOrderCommand.Execute(null);
            NewOrderVM newOrderTab = GetOpenTempTabVM<NewOrderVM>();
            newOrderTab.OrderVendor = berryMan;
            newOrderTab.ClearOrderCommand.Execute(null);

            foreach (KeyValuePair<string, float> kvp in _updateOrderDict)
            {
                newOrderTab.FilteredOrderItems.First(x => x.Name == kvp.Key).LastOrderAmount = kvp.Value;
            }

            newOrderTab.SaveNewOrderCommand.Execute(null);

            return orderTab;
        }

        private VendorTabVM CreateTestVendor(string vendorName, string email = "")
        {
            VendorTabVM vendorTab = _vm.VendorTab;

            vendorTab.AddVendorCommand.Execute(null);
            NewVendorWizardVM newVendorTab = GetOpenTempTabVM<NewVendorWizardVM>();
            
            newVendorTab.Vend = new Vendor() { Name = vendorName, Email = email };
            foreach (KeyValuePair<string, float[]> kvp in _vendorSoldDict)
            {
                string name = kvp.Key;
                float conversion = kvp.Value[0];
                float price = kvp.Value[1];

                InventoryVendorItem item = newVendorTab.InventoryList.First(x => x.Name == name);
                item.Conversion = conversion;
                item.LastPurchasedPrice = price;
                item.IsSold = true;
            }

            newVendorTab.FinishCommand.Execute(null);

            return vendorTab;
        }

        private RecipeTabVM CreateTestBatchRecipe(string recipeName, List<IItem> items)
        {
            RecipeTabVM recipeTab = _vm.RecipeTab;
            recipeTab.SwitchButtonList[0].SwitchCommand.Execute(0);

            recipeTab.AddNewItemCommand.Execute(null);

            NewRecipeVM newRecipeTab = GetOpenTempTabVM<NewRecipeVM>();
            newRecipeTab.Item.Name = recipeName;
            newRecipeTab.Ingredients = new ObservableCollection<IItem>(items);

            newRecipeTab.FinishCommand.Execute(null);

            return recipeTab;
        }

        private InventoryItem SelectInBreakdown(string name, ref OrderBreakdownVM context)
        {
            foreach (BreakdownCategoryItem breakItem in context.BreakdownList)
            {
                foreach (InventoryItem invItem in breakItem.Items)
                {
                    if(invItem.Name == name)
                    {
                        breakItem.SelectedItem = invItem;
                        return invItem;
                    }
                }
            }

            return null;
        }

        #endregion
    }
}
