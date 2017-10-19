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

            _vm.InventoryTab.SwitchButtonList.First(x => x.PageName == "History").SwitchCommand.Execute(1);

            _vm.InventoryTab.PeriodSelector.SelectedPeriod = _vm.InventoryTab.PeriodSelector.PeriodList.First(x => x.ToString().Contains("All"));
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
            tabVM.SwitchButtonList.First(x => x.PageName == "History").SwitchCommand.Execute(1);
            tabVM.PeriodSelector.SelectedPeriod = tabVM.PeriodSelector.PeriodList.First(x => x.ToString().Contains("All"));
            int initInvCount = tabVM.InventoryList.Count;

            tabVM.AddCommand.Execute(null);
            NewInventoryVM newInvVM = GetOpenTempTabVM<NewInventoryVM>();
            newInvVM.InvDate = invDate;

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
            newInvVM.InvDate = invDate;
            int randCount = new Random().Next(1, 1000);
            newInvVM.InvListVM.FilteredItems.First(x => x.Name == "Mozzarella").Count = randCount;

            try
            {
                newInvVM.SaveCountCommand.Execute(null);

                int initInvCount = tabVM.InventoryList.Count;
                tabVM.SelectedInventory = tabVM.InventoryList[0];
                tabVM.ViewCommand.Execute(null);
                NewInventoryVM editInvVM = GetOpenTempTabVM<NewInventoryVM>();

                Assert.AreEqual(randCount, editInvVM.InvListVM.FilteredItems.First(x => x.Name == "Mozzarella").Count);

                editInvVM.InvDate = invDate.AddDays(-1);
                editInvVM.SaveCountCommand.Execute(null);

                Assert.AreEqual(invDate.AddDays(-1), tabVM.InventoryList[0].Date);
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
            string name = "My New Item11";
            Vendor v1 = new Vendor(new Dictionary<string, string>() { { "Name", "Another guy" } });
            Vendor v2 = new Vendor(new Dictionary<string, string>() { { "Name", "Sysco" } });
            InventoryListVM listVM = CreateTestInventoryItem(name, v1, v2);

            VendorInventoryItem item = listVM.FilteredItems.FirstOrDefault(x => x.Name == name);

            try
            {
                Assert.IsNotNull(item);
                InventoryItem dbItem = new InventoryItem(new Dictionary<string, string>() { { "Name", name } });
                Assert.AreEqual(name, dbItem.Name);
                Assert.AreEqual("EA", dbItem.CountUnit);

                List<InventoryItem> v1Items = v1.ItemList;
                InventoryItem v1Item = v1Items.First(x => x.Name == name);
                Assert.AreEqual(2, v1Item.Conversion);
                List<InventoryItem> v2Items = v2.ItemList;
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
            InventoryItem noV1Item = v1.ItemList.FirstOrDefault(x => x.Name == name);
            Assert.IsNull(noV1Item);
            InventoryItem noV2Item = v2.ItemList.FirstOrDefault(x => x.Name == name);
            Assert.IsNull(noV2Item);
        }

        [TestMethod]
        public void NewInventoryItemFromNewInvTest()
        {
            string name = "My New Item0";
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
                DBCache models = _vm.GetModelContainer();
                models.VIContainer.RemoveItem(models.VIContainer.Items.First(x => x.Name == name));
                Assert.IsTrue(false);
            }

            try
            {
                InventoryItem dbItem = new InventoryItem(new Dictionary<string, string>() { { "Name", name } });
                Assert.AreEqual(name, dbItem.Name);
                Assert.AreEqual("EA", dbItem.CountUnit);

                List<InventoryItem> v1Items = v1.ItemList;
                InventoryItem v1Item = v1Items.First(x => x.Name == name);
                Assert.AreEqual(2, v1Item.Conversion);
                List<InventoryItem> v2Items = v2.ItemList;
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
                VendorInventoryItem vItem = listVM.FilteredItems[i];
                vItem.Count = 66;
                listVM.RowEdited(vItem);
            }

            string name = "My New Item9";
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
                if (item != null)
                    item.Destroy();
                else
                    _vm.GetModelContainer().VIContainer.Items.First(x => x.Name == name).Destroy();
            }
        }

        [TestMethod]
        public void RemoveVendorFromInvItemVM()
        {
            string name = "My New Item8";
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
                VendorInfo delVendor = wizardVM.VendorList.First(x => x.Name == "Another guy");
                wizardVM.SelectedItem = delVendor;
                wizardVM.DeleteVendorCommand.Execute(null);
                wizardVM.NextCommand.Execute(null);
                wizardVM.FinishCommand.Execute(null);

                // redundant (I think) but reminder that the Master List should have changed
                item = listVM.FilteredItems.First(x => x.Name == name);
                List<InventoryItem> remainingItems = delVendor.Vend.GetInventoryItems();

                Assert.AreEqual(1, item.Vendors.Count);
                Assert.AreEqual(v2.Name, item.Vendors[0].Name);
                CollectionAssert.DoesNotContain(remainingItems.Select(x => x.Id).ToList(), item.Id);
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
            Vendor selectVendor = _vm.GetModelContainer().VContainer.Items.First(x => x.Name == "Sysco");
            newOrderTab.OrderVendor = selectVendor;

            List<InventoryItem> refItems = MainHelper.SortItems(selectVendor.ItemList).ToList();
            CollectionAssert.AreEqual(refItems.Select(x => x.Name).ToList(), newOrderTab.FilteredOrderItems.Select(x => x.Name).ToList());
            //CollectionAssert.AreEqual(refItems.Select(x => x.LastOrderAmount).ToList(),
            //                          newOrderTab.FilteredOrderItems.Select(x => x.LastOrderAmount).ToList());
        }

        [TestMethod]
        public void SelectEmptyOrderVendorTest()
        {
            OrderTabVM orderTab = _vm.OrderTab;
            DBCache models = _vm.GetModelContainer();
            Vendor newVend = new Vendor() { Name = "TempVend" };

            try
            {
                models.VContainer.AddItem(newVend);

                orderTab.AddNewOrderCommand.Execute(null);
                NewOrderVM newOrderTab = GetOpenTempTabVM<NewOrderVM>();
                newOrderTab.OrderVendor = newVend;

                Assert.AreEqual("Vendor has no items", newOrderTab.FilteredOrderItems.First().Name);
                Assert.IsFalse(newOrderTab.SaveOrderCanExecute);
            }
            finally
            {
                models.VContainer.RemoveItem(newVend);
            }
        }

        [TestMethod]
        public void ClearNewOrderAmountsTest()
        {
            OrderTabVM orderTab = _vm.OrderTab;
            Vendor berryMan = new Vendor(new Dictionary<string, string>() { { "Name", "Berry Man" } });
            float[] clearedAmounts = new float[] { 0, 0, 0, 0, 0 };

            orderTab.AddNewOrderCommand.Execute(null);
            NewOrderVM newOrderTab = GetOpenTempTabVM<NewOrderVM>();
            newOrderTab.OrderVendor = berryMan;

            foreach (VendorInventoryItem item in newOrderTab.FilteredOrderItems)
            {
                item.LastOrderAmount = 6;
                newOrderTab.RowEdited(item);
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
                
                List<InventoryItem> orderItems = newOrder.GetPOItems();
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

                List<InventoryItem> receivedItems = newOrder.GetPOItems();
                foreach (InventoryItem item in receivedItems)
                {
                    Assert.AreEqual(_updateOrderDict[item.Name], item.LastOrderAmount);
                }
                Assert.AreEqual(DateTime.Today, ((DateTime)newOrder.ReceivedDate).Date);
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

                List<InventoryItem> openItems = newOrder.GetPOItems();
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
                OrderBreakdownVM breakdownContext = viewOrderTab.BreakdownContext;
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
                newOrder.ReceivedCheck = true;
                orderTab.ReceivedOrdersCommand.Execute(null);

                orderTab.SelectedReceivedOrder = newOrder;

                orderTab.ViewReceivedOrderCommand.Execute(null);
                ViewOrderVM viewOrderTab = GetOpenTempTabVM<ViewOrderVM>();
                OrderBreakdownVM breakdownContext = viewOrderTab.BreakdownContext;
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
        public void EditReceivedOrderTest()
        {
            OrderTabVM orderTab = CreateTestOrder();

            orderTab.SelectedOpenOrder = orderTab.OpenOrders.FirstOrDefault(x => x.OrderDate == DateTime.Today);
            PurchaseOrder newOrder = orderTab.SelectedOpenOrder;

            try
            {
                newOrder.ReceivedCheck = true;
                orderTab.ReceivedOrdersCommand.Execute(null);

                orderTab.SelectedReceivedOrder = newOrder;

                orderTab.ViewReceivedOrderCommand.Execute(null);
                ViewOrderVM viewOrderTab = GetOpenTempTabVM<ViewOrderVM>();
                OrderBreakdownVM breakdownContext = viewOrderTab.BreakdownContext;

                BreakdownCategoryItem bdItem = breakdownContext.BreakdownList.First(x => x.Items.Select(y => y.Name).Contains("Vanilla"));
                InventoryItem item = bdItem.Items.First(x => x.Name == "Vanilla");
                item.LastOrderAmount = 69f;
                bdItem.UpdateOrderItem(item);
                
                viewOrderTab.SaveCommand.Execute(null);

                orderTab.SelectedReceivedOrder = newOrder;
                orderTab.ViewReceivedOrderCommand.Execute(null);
                viewOrderTab = GetOpenTempTabVM<ViewOrderVM>();

                InventoryItem editedItem = viewOrderTab.BreakdownContext.BreakdownList.Select(x => x.Items.FirstOrDefault(y => y.Name == "Vanilla"))
                                                                                                    .Where(x => x != null).First();

                Assert.AreEqual(69f, editedItem.LastOrderAmount);
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

                List<InventoryItem> vendorItems = newVendor.ItemList;
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
                DeleteVendorVM(newVendor);
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
                List<InventoryItem> vendorItems = newVendor.ItemList;
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
                DeleteVendorVM(newVendor);
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

                List<InventoryItem> vendorItems = newVendor.ItemList;
                InventoryItem testVendorItem = vendorItems.First(x => x.Name == "Vanilla");
                Assert.AreEqual(7, testVendorItem.Conversion);
                Assert.AreEqual(77.40f, testVendorItem.LastPurchasedPrice);
            }
            finally
            {
                newVendor.Destroy();
            }
        }

        //[TestMethod]
        //public void ResetVendorVMTest()
        //{
        //    VendorTabVM vendorTab = _vm.VendorTab;
        //    Vendor berryMan = vendorTab.FilteredVendorList.FirstOrDefault(x => x.Name == "Berry Man");
        //    vendorTab.SelectedVendor = berryMan;

        //    VendorInventoryItem item = vendorTab.SelectedVendorItems[0];
        //    float origConv = item.Conversion;
        //    float origCount = item.Count;

        //    item.Conversion = 666f;
        //    item.Count = 123f;

        //    vendorTab.ResetCommand.Execute(null);
        //    item = vendorTab.SelectedVendorItems[0];

        //    Assert.AreEqual(origConv, item.Conversion);
        //    Assert.AreEqual(origCount, item.Count);
        //}

        [TestMethod]
        public void RemoveVendorItemFromEditTest()
        {
            string name = "My New Vendor";
            VendorTabVM vendorTab = CreateTestVendor(name, "mynew@example.com");

            Vendor newVendor = vendorTab.FilteredVendorList.FirstOrDefault(x => x.Name == name);
            vendorTab.SelectedVendor = newVendor;
            int origCount = vendorTab.SelectedVendorItems.Count;

            try
            {
                vendorTab.EditVendorCommand.Execute(null);
                NewVendorWizardVM editItemsTab = GetOpenTempTabVM<NewVendorWizardVM>();
                editItemsTab.NextCommand.Execute(null);

                InventoryVendorItem removeItem = editItemsTab.InventoryList.First(x => x.Name == "Pesto");
                removeItem.IsSold = false;

                editItemsTab.FinishCommand.Execute(null);

                List<InventoryItem> vendorItems = newVendor.ItemList;

                vendorTab.SelectedVendor = newVendor;
                Assert.AreEqual(origCount - 1, vendorTab.SelectedVendorItems.Count);
                Assert.AreEqual(origCount - 1, vendorItems.Count);
                CollectionAssert.DoesNotContain(vendorTab.SelectedVendorItems.Select(x => x.Name).ToList(), "Pesto");
                CollectionAssert.DoesNotContain(vendorItems.Select(x => x.Name).ToList(), "Pesto");
            }
            finally
            {
                newVendor.Destroy();
            }
        }

        [TestMethod]
        public void AddVendorItemCheckEditTest()
        {
            // add an item to vendor and check that that item appears when editing item
            string name = "My New Vendor";
            VendorTabVM vendorTab = CreateTestVendor(name, "mynew@example.com");

            Vendor newVendor = vendorTab.FilteredVendorList.FirstOrDefault(x => x.Name == name);
            vendorTab.SelectedVendor = newVendor;

            try
            {
                // add sourdough to vendor list
                vendorTab.AddInvItemToVendor(new InventoryItem(new Dictionary<string, string>() { { "Name", "Sourdough" } }));

                vendorTab.EditVendorCommand.Execute(null);
                NewVendorWizardVM editItemsTab = GetOpenTempTabVM<NewVendorWizardVM>();
                editItemsTab.NextCommand.Execute(null);

                CollectionAssert.Contains(editItemsTab.InventoryList.Select(x => x.Name).ToList(), "Sourdough");
                Assert.IsTrue(editItemsTab.InventoryList.First(x => x.Name == "Sourdough").IsSold);
            }
            finally
            {
                newVendor.Destroy();
            }
        }

        [TestMethod]
        public void AddItemToVendorCheckMaster()
        {
            string name = "My New Vendor2";
            VendorTabVM vendorTab = CreateTestVendor(name, "mynew2@example.com");

            Vendor newVendor = vendorTab.FilteredVendorList.FirstOrDefault(x => x.Name == name);
            vendorTab.SelectedVendor = newVendor;

            try
            {
                string itemName = "Avocado";
                InventoryItem itemToAdd = _vm.GetModelContainer().VIContainer.Items.First(x => x.Name == itemName).ToInventoryItem();
                // add sourdough to vendor list
                vendorTab.AddInvItemToVendor(itemToAdd);

                InventoryListVM masterListVM = _vm.InventoryTab.InvListVM;
                VendorInventoryItem alteredItem = masterListVM.FilteredItems.First(x => x.Name == itemName);

                CollectionAssert.Contains(alteredItem.Vendors.Select(x => x.Id).ToList(), newVendor.Id);
                CollectionAssert.Contains(newVendor.ItemList.Select(x => x.Name).ToList(), itemToAdd.Name);
                List<InventoryItem> vendorItems = newVendor.GetInventoryItems();
                CollectionAssert.Contains(vendorItems.Select(x => x.Id).ToList(), itemToAdd.Id);

                // test delete this item from vendor as well
                vendorTab.SelectedVendor = newVendor;
                vendorTab.SelectedVendorItem = itemToAdd;
                vendorTab.DeleteVendorItemCommand.Execute(null);
                CollectionAssert.DoesNotContain(alteredItem.Vendors.Select(x => x.Id).ToList(), newVendor.Id);
                CollectionAssert.DoesNotContain(newVendor.ItemList.Select(x => x.Name).ToList(), itemToAdd.Name);
                vendorItems = newVendor.GetInventoryItems();
                CollectionAssert.DoesNotContain(vendorItems.Select(x => x.Id).ToList(), itemToAdd.Id);
            }
            finally
            {
                newVendor.Destroy();
            }
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
            List<IItem> baseItems = new List<IItem>()
            {
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Avocado" } }),
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Black Beans" } }),
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Cinnamon" } }),
                new Recipe(new Dictionary<string, string>() { { "Name", "New England Clam Chowder" } })
            };

            List<RecipeItem> recipeItems = baseItems.Select(x => new Ingredient(x).GetRecipeItem()).ToList();

            RecipeTabVM recipeTab = CreateTestBatchRecipe(name, recipeItems);
            DisplayRecipe newRecipe = recipeTab.FilteredItems.FirstOrDefault(x => x.Name == name);
            recipeTab.SelectedItem = newRecipe;

            try
            {
                Assert.AreEqual(name, newRecipe.Name);
                CollectionAssert.AreEquivalent(recipeItems.Select(x => x.Name).ToList(), newRecipe.GetRecipe().GetItems().Select(x => x.Name).ToList());
            }
            finally
            {
                recipeTab.DeleteItemCommand.Execute(null);
            }

            DisplayRecipe emptyRecipe = recipeTab.FilteredItems.FirstOrDefault(x => x.Name == name);
            Assert.IsNull(emptyRecipe);
        }

        [TestMethod]
        public void EditRecipeVMTest()
        {
            string name = "New Recipe";
            List<IItem> baseItems = new List<IItem>()
            {
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Avocado" } }),
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Black Beans" } }),
                new InventoryItem(new Dictionary<string, string>() { { "Name", "Cinnamon" } }),
                new Recipe(new Dictionary<string, string>() { { "Name", "New England Clam Chowder" } })
            };

            List<RecipeItem> recipeItems = baseItems.Select(x => new Ingredient(x).GetRecipeItem()).ToList();

            RecipeTabVM recipeTab = CreateTestBatchRecipe(name, recipeItems);
            DisplayRecipe newRecipe = recipeTab.FilteredItems.FirstOrDefault(x => x.Name == name);
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

                DisplayRecipe editedRecipe = recipeTab.FilteredItems.First(x => x.Name == name);
                CollectionAssert.Contains(newRecipe.GetRecipe().GetItems().Select(x => x.Name).ToList(), "Butter");
                Assert.AreEqual(42, newRecipe.GetRecipe().GetItems().First(x => x.Name == "Avocado").Count);
            }
            finally
            {
                newRecipe.GetRecipe().Destroy();
            }
        }

        #endregion

        #region Cross Tab Tests

        [TestMethod]
        public void ChangeMasterCheckVendorTest()
        {
            string name = "My New Item7";
            Vendor v1 = new Vendor(new Dictionary<string, string>() { { "Name", "Another guy" } });
            Vendor v2 = new Vendor(new Dictionary<string, string>() { { "Name", "Sysco" } });
            InventoryListVM listVM = CreateTestInventoryItem(name, v1, v2);

            VendorInventoryItem item = listVM.FilteredItems.FirstOrDefault(x => x.Name == name);

            try
            {
                listVM.SelectedInventoryItem = item;
                item.SelectedVendor = v1;
                item.Conversion = 10;
                item.LastPurchasedPrice = 10f;
                listVM.RowEdited(item);

                item.SelectedVendor = v2;
                item.Conversion = 5;
                item.LastPurchasedPrice = 5f;
                listVM.RowEdited(item);

                VendorTabVM vendorTab = _vm.VendorTab;
                vendorTab.SelectedVendor = v1;
                InventoryItem vendItem = vendorTab.SelectedVendorItems.First(x => x.Name == name);
                Assert.AreEqual(10, vendItem.Conversion);
                Assert.AreEqual(10f, vendItem.LastPurchasedPrice);

                vendorTab.SelectedVendor = v2;
                vendItem = vendorTab.SelectedVendorItems.First(x => x.Name == name);
                Assert.AreEqual(5, vendItem.Conversion);
                Assert.AreEqual(5f, vendItem.LastPurchasedPrice);
            }
            finally
            {
                item.Destroy();
            }
        }

        [TestMethod]
        public void ChangeVendorCheckMasterTest()
        {
            string name = "My New Item6";
            Vendor v1 = new Vendor(new Dictionary<string, string>() { { "Name", "Another guy" } });
            Vendor v2 = new Vendor(new Dictionary<string, string>() { { "Name", "Sysco" } });
            InventoryListVM listVM = CreateTestInventoryItem(name, v1, v2);

            VendorInventoryItem item = listVM.FilteredItems.FirstOrDefault(x => x.Name == name);

            try
            {
                listVM.SelectedInventoryItem = item;
                item.SelectedVendor = v1;

                VendorTabVM vendorTab = _vm.VendorTab;
                vendorTab.SelectedVendor = v1;
                vendorTab.SelectedVendorItem = vendorTab.SelectedVendorItems.First(x => x.Id == item.Id);

                vendorTab.SelectedVendorItem.Conversion = 10;
                vendorTab.SelectedVendorItem.LastPurchasedPrice = 10f;
                vendorTab.VendorItemChanged(vendorTab.SelectedVendorItem);

                vendorTab.SelectedVendor = v2;
                vendorTab.SelectedVendorItem = vendorTab.SelectedVendorItems.First(x => x.Id == item.Id);

                vendorTab.SelectedVendorItem.Conversion = 5;
                vendorTab.SelectedVendorItem.LastPurchasedPrice = 5f;
                vendorTab.VendorItemChanged(vendorTab.SelectedVendorItem);
                
                VendorInventoryItem masterItem = listVM.FilteredItems.First(x => x.Name == name);
                Assert.AreEqual(v1.Id, listVM.SelectedInventoryItem.SelectedVendor.Id);
                Assert.AreEqual(10, masterItem.Conversion);
                Assert.AreEqual(10f, masterItem.LastPurchasedPrice);

                masterItem.SelectedVendor = v2;
                Assert.AreEqual(5, masterItem.Conversion);
                Assert.AreEqual(5f, masterItem.LastPurchasedPrice);
            }
            finally
            {
                item.Destroy();
            }
        }

        [TestMethod]
        public void ChangeMasterCheckNewInvTest()
        {
            string name = "My New Item5";
            Vendor v1 = new Vendor(new Dictionary<string, string>() { { "Name", "Another guy" } });
            Vendor v2 = new Vendor(new Dictionary<string, string>() { { "Name", "Sysco" } });
            InventoryListVM listVM = CreateTestInventoryItem(name, v1, v2);

            VendorInventoryItem item = listVM.FilteredItems.FirstOrDefault(x => x.Name == name);

            try
            {
                _vm.InventoryTab.AddCommand.Execute(null);
                NewInventoryVM newInvTab = GetOpenTempTabVM<NewInventoryVM>();

                listVM.SelectedInventoryItem = item;
                item.SelectedVendor = v1;
                item.Conversion = 10;
                item.LastPurchasedPrice = 10f;
                listVM.RowEdited(item);

                item.SelectedVendor = v2;
                item.Conversion = 5;
                item.LastPurchasedPrice = 5f;
                listVM.RowEdited(item);

                VendorInventoryItem vendItem = newInvTab.InvListVM.FilteredItems.First(x => x.Name == name);
                Assert.AreEqual(v2.Id, vendItem.SelectedVendor.Id);
                Assert.AreEqual(5, vendItem.Conversion);
                Assert.AreEqual(5f, vendItem.LastPurchasedPrice);

                vendItem.SelectedVendor = v1;
                Assert.AreEqual(10, vendItem.Conversion);
                Assert.AreEqual(10f, vendItem.LastPurchasedPrice);
            }
            finally
            {
                item.Destroy();
            }
        }

        [TestMethod]
        public void ChangeVendorCheckNewInvTest()
        {
            string name = "My New Item4";
            Vendor v1 = new Vendor(new Dictionary<string, string>() { { "Name", "Another guy" } });
            Vendor v2 = new Vendor(new Dictionary<string, string>() { { "Name", "Sysco" } });
            InventoryListVM listVM = CreateTestInventoryItem(name, v1, v2);

            VendorInventoryItem item = listVM.FilteredItems.FirstOrDefault(x => x.Name == name);

            try
            {
                _vm.InventoryTab.AddCommand.Execute(null);
                NewInventoryVM newInv = GetOpenTempTabVM<NewInventoryVM>();

                listVM.SelectedInventoryItem = item;
                item.SelectedVendor = v1;

                VendorTabVM vendorTab = _vm.VendorTab;
                vendorTab.SelectedVendor = v1;
                vendorTab.SelectedVendorItem = vendorTab.SelectedVendorItems.First(x => x.Id == item.Id);

                vendorTab.SelectedVendorItem.Conversion = 10;
                vendorTab.SelectedVendorItem.LastPurchasedPrice = 10f;
                vendorTab.VendorItemChanged(vendorTab.SelectedVendorItem);

                vendorTab.SelectedVendor = v2;
                vendorTab.SelectedVendorItem = vendorTab.SelectedVendorItems.First(x => x.Id == item.Id);

                vendorTab.SelectedVendorItem.Conversion = 5;
                vendorTab.SelectedVendorItem.LastPurchasedPrice = 5f;
                vendorTab.VendorItemChanged(vendorTab.SelectedVendorItem);

                VendorInventoryItem vendItem = newInv.InvListVM.FilteredItems.First(x => x.Name == name);
                Assert.AreEqual(v1.Id, listVM.SelectedInventoryItem.SelectedVendor.Id);
                Assert.AreEqual(10, vendItem.Conversion);
                Assert.AreEqual(10f, vendItem.LastPurchasedPrice);

                vendItem.SelectedVendor = v2;
                Assert.AreEqual(5, vendItem.Conversion);
                Assert.AreEqual(5f, vendItem.LastPurchasedPrice);
            }
            finally
            {
                item.Destroy();
            }
        }

        [TestMethod]
        public void ChangeNewInvCheckMasterTest()
        {
            // ensure that master inventory does not change when changing new inventory (before saving)

            string name = "My New Item3";
            Vendor v1 = new Vendor(new Dictionary<string, string>() { { "Name", "Another guy" } });
            Vendor v2 = new Vendor(new Dictionary<string, string>() { { "Name", "Sysco" } });
            InventoryListVM listVM = CreateTestInventoryItem(name, v1, v2);

            VendorInventoryItem item = listVM.FilteredItems.FirstOrDefault(x => x.Name == name);

            try
            {
                item.SelectedVendor = v1;
                _vm.InventoryTab.AddCommand.Execute(null);
                NewInventoryVM newInv = GetOpenTempTabVM<NewInventoryVM>();
                VendorInventoryItem vendItem = newInv.InvListVM.FilteredItems.First(x => x.Name == name);

                listVM.SelectedInventoryItem = item;
                item.SelectedVendor = v1;

                VendorTabVM vendorTab = _vm.VendorTab;
                vendItem.SelectedVendor = v1;
                float v1conv = vendItem.Conversion;
                float v1price = vendItem.LastPurchasedPrice;

                vendItem.Conversion = 10;
                vendItem.LastPurchasedPrice = 10f;
                newInv.InvListVM.RowEdited(vendItem);

                vendItem.SelectedVendor = v2;
                float v2conv = vendItem.Conversion;
                float v2price = vendItem.LastPurchasedPrice;

                vendItem.Conversion = 5;
                vendItem.LastPurchasedPrice = 5f;
                newInv.InvListVM.RowEdited(vendItem);

                Assert.AreEqual(v1.Id, listVM.SelectedInventoryItem.SelectedVendor.Id);
                Assert.AreEqual(v1conv, listVM.SelectedInventoryItem.Conversion);
                Assert.AreEqual(v1price, listVM.SelectedInventoryItem.LastPurchasedPrice);

                listVM.SelectedInventoryItem.SelectedVendor = v2;
                Assert.AreEqual(v2conv, listVM.SelectedInventoryItem.Conversion);
                Assert.AreEqual(v2price, listVM.SelectedInventoryItem.LastPurchasedPrice);
            }
            finally
            {
                item.Destroy();
            }
        }

        [TestMethod]
        public void ChangeMasterCheckNewPOTest()
        {
            string name = "My New Item2";
            Vendor v1 = new Vendor(new Dictionary<string, string>() { { "Name", "Another guy" } });
            Vendor v2 = new Vendor(new Dictionary<string, string>() { { "Name", "Sysco" } });
            InventoryListVM listVM = CreateTestInventoryItem(name, v1, v2);

            VendorInventoryItem item = listVM.FilteredItems.FirstOrDefault(x => x.Name == name);

            try
            {
                _vm.OrderTab.AddNewOrderCommand.Execute(null);
                NewOrderVM newOrderTab = GetOpenTempTabVM<NewOrderVM>();

                listVM.SelectedInventoryItem = item;
                item.SelectedVendor = v1;
                item.Conversion = 10;
                item.LastPurchasedPrice = 10f;
                listVM.RowEdited(item);

                item.SelectedVendor = v2;
                item.Conversion = 5;
                item.LastPurchasedPrice = 5f;
                listVM.RowEdited(item);

                newOrderTab.OrderVendor = v1;
                VendorInventoryItem vendItem = newOrderTab.FilteredOrderItems.First(x => x.Name == name);
                Assert.AreEqual(10, vendItem.Conversion);
                Assert.AreEqual(10f, vendItem.LastPurchasedPrice);

                newOrderTab.OrderVendor = v2;
                vendItem = newOrderTab.FilteredOrderItems.First(x => x.Name == name);
                Assert.AreEqual(5, vendItem.Conversion);
                Assert.AreEqual(5f, vendItem.LastPurchasedPrice);
            }
            finally
            {
                item.Destroy();
            }
        }

        [TestMethod]
        public void ChangeNewPOCheckMasterTest()
        {
            // ensure that master inventory does not change when changing item info in new PO (before saving)

            string name = "My New Item1";
            Vendor v1 = new Vendor(new Dictionary<string, string>() { { "Name", "Another guy" } });
            Vendor v2 = new Vendor(new Dictionary<string, string>() { { "Name", "Sysco" } });
            InventoryListVM listVM = CreateTestInventoryItem(name, v1, v2);

            VendorInventoryItem masterItem = listVM.FilteredItems.FirstOrDefault(x => x.Name == name);

            try
            {
                _vm.OrderTab.AddNewOrderCommand.Execute(null);
                NewOrderVM newOrder = GetOpenTempTabVM<NewOrderVM>();
                newOrder.OrderVendor = v1;
                VendorInventoryItem orderItem = newOrder.FilteredOrderItems.First(x => x.Name == name);
                newOrder.SelectedOrderItem = orderItem;

                listVM.SelectedInventoryItem = masterItem;
                masterItem.SelectedVendor = v1;

                VendorTabVM vendorTab = _vm.VendorTab;
                orderItem.SelectedVendor = v1;
                float v1conv = orderItem.Conversion;
                float v1price = orderItem.LastPurchasedPrice;

                orderItem.Conversion = 10;
                orderItem.LastPurchasedPrice = 10f;
                newOrder.RowEdited(orderItem);

                newOrder.OrderVendor = v2;
                float v2conv = orderItem.Conversion;
                float v2price = orderItem.LastPurchasedPrice;

                orderItem.Conversion = 5;
                orderItem.LastPurchasedPrice = 5f;
                newOrder.RowEdited(orderItem);

                Assert.AreEqual(v1.Id, listVM.SelectedInventoryItem.SelectedVendor.Id);
                Assert.AreEqual(v1conv, listVM.SelectedInventoryItem.Conversion);
                Assert.AreEqual(v1price, listVM.SelectedInventoryItem.LastPurchasedPrice);

                listVM.SelectedInventoryItem.SelectedVendor = v2;
                Assert.AreEqual(v2conv, listVM.SelectedInventoryItem.Conversion);
                Assert.AreEqual(v2price, listVM.SelectedInventoryItem.LastPurchasedPrice);
            }
            finally
            {
                masterItem.Destroy();
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

            newInvVM.NextCommand.Execute(null);

            newInvVM.VendorList.Add(new VendorInfo(v1) { Conversion = 2 });
            newInvVM.VendorList.Add(new VendorInfo(v2) { Conversion = 4 });

            newInvVM.NextCommand.Execute(null);

            newInvVM.InvOrderList = new ObservableCollection<InventoryItem>(_vm.GetModelContainer().VIContainer.ToInvContainer().Items);

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
            //foreach (InventoryItem item in newOrderTab.FilteredOrderItems)
            //{
            //    item.LastOrderAmount = new Random().Next(1, 30);
            //}

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

        private RecipeTabVM CreateTestBatchRecipe(string recipeName, List<RecipeItem> items)
        {
            RecipeTabVM recipeTab = _vm.RecipeTab;
            recipeTab.SwitchButtonList[0].SwitchCommand.Execute(0);

            recipeTab.AddNewItemCommand.Execute(null);

            NewRecipeVM newRecipeTab = GetOpenTempTabVM<NewRecipeVM>();
            newRecipeTab.Item.Name = recipeName;
            newRecipeTab.Ingredients = new ObservableCollection<Ingredient>(items.Select(x => new Ingredient(x)));

            newRecipeTab.FinishCommand.Execute(null);

            return recipeTab;
        }

        private void DeleteVendorVM(Vendor v)
        {
            _vm.VendorTab.SelectedVendor = v;
            _vm.VendorTab.EditVendorCommand.Execute(null);
            NewVendorWizardVM editVendTab = GetOpenTempTabVM<NewVendorWizardVM>();
            editVendTab.DeleteVendorCommand.Execute(null);
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
