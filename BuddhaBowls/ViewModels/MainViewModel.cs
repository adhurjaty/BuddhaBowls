using BuddhaBowls.Helpers;
using BuddhaBowls.Models;
using BuddhaBowls.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace BuddhaBowls
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private MainWindow _window;
        private ModelContainer _models;
        private bool _databaseFound;

        // INotifyPropertyChanged event and method
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Data Bindings
        // Value in text box for selecting the folder location of DB files
        private string _dataFileFolder;
        public string DataFileFolder
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_dataFileFolder))
                    DataFileFolder = Properties.Settings.Default.DBLocation;
                return _dataFileFolder;
            }
            set
            {
                _dataFileFolder = value;
                NotifyPropertyChanged("DataFileFolder");
            }
        }

        // Inventory item selected in the datagrids for Orders and Master List
        private InventoryItem _selectedInventoryItem;
        public InventoryItem SelectedInventoryItem
        {
            get
            {
                return _selectedInventoryItem;
            }
            set
            {
                _selectedInventoryItem = value;
                NotifyPropertyChanged("SelectedInventoryItem");
            }
        }

        // Header of the add/edit tab
        private string _addEditHeader;
        public string AddEditHeader
        {
            get
            {
                return _addEditHeader;
            }
            set
            {
                _addEditHeader = value;
                NotifyPropertyChanged("AddEditHeader");
            }
        }

        private string _addEditErrorMessage;
        public string AddEditErrorMessage
        {
            get
            {
                return _addEditErrorMessage;
            }
            set
            {
                _addEditErrorMessage = value;
                NotifyPropertyChanged("AddEditErrorMessage");
            }
        }

        private string _filterText;
        public string FilterText
        {
            get
            {
                return _filterText;
            }
            set
            {
                _filterText = value;
                NotifyPropertyChanged("FilterText");
            }
        }

        // Collection used for both Master List and New Order List
        private ObservableCollection<InventoryItem> _filteredInventoryItems;
        public ObservableCollection<InventoryItem> FilteredInventoryItems
        {
            get
            {
                return _filteredInventoryItems;
            }
            set
            {
                _filteredInventoryItems = value;
                NotifyPropertyChanged("FilteredInventoryItems");
            }
        }

        // Collection of fields and values for use in Model edit forms
        public ObservableCollection<FieldSetting> FieldsCollection { get; set; }

        public ObservableCollection<BreakdownCategoryItem> BreakdownList { get; set; }

        private ObservableCollection<PurchaseOrder> _openOrders;
        public ObservableCollection<PurchaseOrder> OpenOrders
        {
            get
            {
                return _openOrders;
            }
            set
            {
                _openOrders = value;
                NotifyPropertyChanged("OpenOrders");
            }
        }

        private ObservableCollection<PurchaseOrder> _receivedOrders;
        public ObservableCollection<PurchaseOrder> ReceivedOrders
        {
            get
            {
                return _receivedOrders;
            }
            set
            {
                _receivedOrders = value;
                NotifyPropertyChanged("ReceivedOrders");
            }
        }
        public PurchaseOrder SelectedOrder { get; set; }

        // name of the vendor in the New Order form
        public string OrderVendor { get; set; }

        private float _orderCost;
        public float OrderTotal
        {
            get
            {
                return _orderCost;
            }
            set
            {
                _orderCost = value;
                NotifyPropertyChanged("OrderCost");
            }
        }

        private bool _dbConnected;
        #endregion

        #region ICommand Bindings and Can Execute
        // Browse button in settings form
        public ICommand BrowseButtonCommand { get; set; }
        // Generate Report button in settings form
        public ICommand ReportCommand { get; set; }
        // Plus button in Master invententory list form
        public ICommand AddInventoryItemCommand { get; set; }
        // Minus button in Master invententory list form
        public ICommand DeleteInventoryItemCommand { get; set; }
        // Edit button in Master invententory list form
        public ICommand EditInventoryItemCommand { get; set; }
        // Save button in Add/Edit form
        public ICommand SaveAddEditCommand { get; set; }
        // Cancel button in Add/Edit form
        public ICommand CancelAddEditCommand { get; set; }
        // Save button in Settings form
        public ICommand SaveSettingsCommand { get; set; }
        // Save button in Master inventory form
        public ICommand SaveCountCommand { get; set; }
        // Reset button in Master inventory form
        public ICommand ResetCountCommand { get; set; }
        // Save button in New Order form
        public ICommand SaveNewOrderCommand { get; set; }
        // Reset button in New Order form
        public ICommand CancelNewOrderCommand { get; set; }
        // Clear Amts button in New Order form
        public ICommand ClearOrderCommand { get; set; }
        // Received button in Order Overview form
        public ICommand ReceivedOrdersCommand { get; set; }
        // Clear button in Order Overview form
        public ICommand ClearReceivedCheckCommand { get; set; }
        // View button in Order Overview form
        public ICommand ViewOrderCommand { get; set; }
        // Plus button in Order Overview form
        public ICommand AddNewOrderCommand { get; set; }
        // Minus button in Order Overview form for open orders
        public ICommand DeleteOrderCommand { get; set; }
        // Minus button in Order Overview form for received orders
        public ICommand DeleteReceivedOrderCommand { get; set; }

        public bool ReportCanExecute
        {
            get
            {
                return Directory.Exists(DataFileFolder) && _databaseFound;
            }
        }

        public bool SaveSettingsCanExecute
        {
            get
            {
                return Directory.Exists(DataFileFolder);
            }
        }
        
        public bool DeleteEditCanExecute
        {
            get
            {
                return SelectedInventoryItem != null && _databaseFound;
            }
        }

        public bool AddItemCanExecute
        {
            get
            {
                return _databaseFound;
            }
        }

        public bool SaveAddEditCanExecute
        {
            get
            {
                return string.IsNullOrWhiteSpace(AddEditErrorMessage);
            }
        }

        public bool ChangeCountCanExecute { get; set; } = false;
        public bool SaveOrderCanExecute
        {
            get
            {
                return !string.IsNullOrWhiteSpace(OrderVendor) && _models.InventoryItems.FirstOrDefault(x => x.LastOrderAmount > 0) != null;
            }
        }

        public bool ViewOrderCanExecute
        {
            get
            {
                return SelectedOrder != null;
            }
        }

        public bool RemoveOpenOrderCanExecute
        {
            get
            {
                return SelectedOrder != null && SelectedOrder.ReceivedDate == null;
            }
        }

        public bool RemoveReceivedOrderCanExecute
        {
            get
            {
                return SelectedOrder != null && SelectedOrder.ReceivedDate != null;
            }
        }
        #endregion

        public MainViewModel()
        {
            BrowseButtonCommand = new RelayCommand(BrowseHelper);
            ReportCommand = new RelayCommand(ReportHelper, x => ReportCanExecute);
            AddInventoryItemCommand = new RelayCommand(AddInventoryItem, x => AddItemCanExecute);
            DeleteInventoryItemCommand = new RelayCommand(DeleteInventoryItem, x => DeleteEditCanExecute);
            EditInventoryItemCommand = new RelayCommand(EditInventoryItem, x => DeleteEditCanExecute);
            SaveAddEditCommand = new RelayCommand(SaveAddEdit, x => SaveAddEditCanExecute);
            CancelAddEditCommand = new RelayCommand(CancelAddEdit);
            SaveSettingsCommand = new RelayCommand(SaveSettings, x => SaveSettingsCanExecute);
            SaveCountCommand = new RelayCommand(SaveCount, x => ChangeCountCanExecute);
            ResetCountCommand = new RelayCommand(ResetCount, x => ChangeCountCanExecute);
            SaveNewOrderCommand = new RelayCommand(SaveOrder, x => SaveOrderCanExecute);
            CancelNewOrderCommand = new RelayCommand(CancelOrder);
            ClearOrderCommand = new RelayCommand(ClearOrderAmounts);
            ReceivedOrdersCommand = new RelayCommand(MoveReceivedOrders);
            ClearReceivedCheckCommand = new RelayCommand(ClearReceivedChecks);
            ViewOrderCommand = new RelayCommand(ViewOrder, x => ViewOrderCanExecute);
            AddNewOrderCommand = new RelayCommand(StartNewOrder);
            DeleteOrderCommand = new RelayCommand(RemoveOpenOrder, x => RemoveOpenOrderCanExecute);
            DeleteReceivedOrderCommand = new RelayCommand(RemoveReceivedOrder, x => RemoveReceivedOrderCanExecute);

            _dbConnected = TryDBConnect();

            MakeBreakdownDisplay();
        }

        #region ICommand Helpers
        /// <summary>
        /// Testing function right now - triggered with Generate Report
        /// </summary>
        /// <param name="obj"></param>
        private void ReportHelper(object obj)
        {
            ReportGenerator generator = new ReportGenerator(_models);
            //generator.FillInventoryId("Mac & Cheese");
            //generator.CreateBatchRecipeReport("Mac & Cheese");
            //generator.MakeMasterInventoryTable();
            try
            {
                //generator.CreateMasterInventoryReport();
                //generator.CreateBatchRecipeReport("Mac & Cheese");
                foreach(string recipe in Directory.EnumerateFiles(Path.Combine(Properties.Settings.Default.DBLocation, "Recipes")))
                {
                    generator.FillInventoryId(Path.GetFileNameWithoutExtension(recipe));
                }

            }
            catch (Exception e)
            {
                MessageBox.Show("Error has occurred generating report", "Report Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                generator.Close();
            }
        }

        /// <summary>
        /// Summons folder dialog for choosing DB location - Browse...
        /// </summary>
        /// <param name="obj"></param>
        private void BrowseHelper(object obj)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "Select data folder";

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DataFileFolder = dialog.SelectedPath;
                Properties.Settings.Default.DBLocation = DataFileFolder;
                Properties.Settings.Default.Save();
            }
        }

        /// <summary>
        /// Creates new tab called Add Inventory Item and populates form - Plus button
        /// </summary>
        /// <param name="obj"></param>
        private void AddInventoryItem(object obj)
        {
            AddEditHeader = "Add New Inventory Item";
            FieldsCollection = GetFieldsAndValues<InventoryItem>();

            _window.AddTempTab("Add Inventory Item", new EditItem());
        }

        /// <summary>
        /// Creates a new tab called Edit Inventory Item and populates form - Edit button
        /// </summary>
        /// <param name="obj"></param>
        private void EditInventoryItem(object obj)
        {
            AddEditHeader = "Edit " + SelectedInventoryItem.Name;
            FieldsCollection = GetFieldsAndValues(SelectedInventoryItem);

            _window.AddTempTab("Edit Inventory Item", new EditItem());
        }

        /// <summary>
        /// Presents user with warning dialog, then removes item from DB and in-memory list - Minus button
        /// </summary>
        /// <param name="obj"></param>
        private void DeleteInventoryItem(object obj)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete " + SelectedInventoryItem.Name,
                                                      "Delete " + SelectedInventoryItem.Name + "?", MessageBoxButton.YesNo);
            if(result == MessageBoxResult.Yes)
            {
                SelectedInventoryItem.Destroy();
                _models.InventoryItems.Remove(SelectedInventoryItem);
                SelectedInventoryItem = null;
                RefreshInventoryList();
            }
        }

        /// <summary>
        /// Removes the add/edit tab - Cancel button
        /// </summary>
        /// <param name="obj"></param>
        private void CancelAddEdit(object obj)
        {
            AddEditErrorMessage = "";
            _window.DeleteTempTab();
        }

        /// <summary>
        /// Looks through fields in add/edit form to ensure that user-supplied values are valid and changes types when necessary
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool SetOrErrorAddEditItem(ref InventoryItem item)
        {
            foreach (FieldSetting field in FieldsCollection)
            {
                if (!item.IsNullable(field.Name) && string.IsNullOrWhiteSpace(field.Value))
                {
                    field.Error = 1;
                    AddEditErrorMessage = field.Name + " must be set";
                    return false;
                }

                if (field.Name == "Name" && _models.InventoryItems.FirstOrDefault(x => x.Name.ToUpper() == field.Value.ToUpper()) != null &&
                   AddEditHeader.StartsWith("Add"))
                {
                    field.Error = 1;
                    AddEditErrorMessage = field.Value + " already exists in the database";
                    return false;
                }

                if (item.GetPropertyType(field.Name) == typeof(int))
                {
                    int val = 0;
                    int.TryParse(field.Value, out val);

                    if (val == 0)
                    {
                        field.Error = 1;
                        AddEditErrorMessage = field.Name + " must be an integer";
                        return false;
                    }
                }

                if (item.GetPropertyType(field.Name) == typeof(float))
                {
                    float val = 0;
                    float divisor = 1f;
                    string valueStr = field.Value;

                    if (valueStr.EndsWith("%"))
                    {
                        valueStr = valueStr.Remove(valueStr.Length - 1);
                        divisor = 100f;
                    }
                    else if (valueStr.StartsWith("$"))
                    {
                        valueStr = valueStr.Remove(0, 1);
                    }

                    float.TryParse(valueStr, out val);

                    if (val == 0)
                    {
                        field.Error = 1;
                        AddEditErrorMessage = field.Name + " must be a number";
                        return false;
                    }

                    field.Value = (val / divisor).ToString();
                }

                field.Error = 0;
                item.SetProperty(field.Name, field.Value);
            }

            return true;
        }

        /// <summary>
        /// Checks for invalid input, then saves the input values to DB
        /// </summary>
        /// <param name="obj"></param>
        private void SaveAddEdit(object obj)
        {
            InventoryItem item = null;
            if (AddEditHeader.StartsWith("Edit"))
                item = SelectedInventoryItem;
            else if (AddEditHeader.StartsWith("Add"))
                item = new InventoryItem();
            else
                throw new NotImplementedException();


            if (SetOrErrorAddEditItem(ref item))
            {

                if (AddEditHeader.StartsWith("Edit"))
                {
                    item.Update();
                }
                else if (AddEditHeader.StartsWith("Add"))
                {
                    _models.InventoryItems.Add(item);
                    item.Insert();
                }
                else
                {
                    throw new NotImplementedException();
                }

                AddEditErrorMessage = "";
                _window.DeleteTempTab();
                RefreshInventoryList();
            }
        }

        /// <summary>
        /// Resets the inventory count to the saved value before changing the datagrid
        /// </summary>
        /// <param name="obj"></param>
        private void ResetCount(object obj)
        {
            foreach (InventoryItem item in FilteredInventoryItems)
            {
                item.Count = item.GetLastCount();
            }

            RefreshInventoryList();
            ChangeCountCanExecute = false;
        }

        /// <summary>
        /// Writes the Inventory items to DB as they are in the Master List datagrid
        /// </summary>
        /// <param name="obj"></param>
        private void SaveCount(object obj)
        {
            foreach(InventoryItem item in FilteredInventoryItems)
            {
                item.Update();
            }

            ChangeCountCanExecute = false;
        }

        /// <summary>
        /// Writes the inventory items to DB as they are in the New Order datagrid
        /// </summary>
        /// <param name="obj"></param>
        private void SaveOrder(object obj)
        {
            foreach(InventoryItem item in FilteredInventoryItems)
            {
                item.Update();
            }

            PurchaseOrder po = new PurchaseOrder(OrderVendor, _models.InventoryItems.Where(x => x.LastOrderAmount > 0).ToList());

            OrderVendor = "";
            _window.DeleteTempTab();
        }

        /// <summary>
        /// Resets order amount values to last saved order amount value in New Order datagrid
        /// </summary>
        /// <param name="obj"></param>
        private void CancelOrder(object obj)
        {
            foreach (InventoryItem item in FilteredInventoryItems)
            {
                item.LastOrderAmount = item.GetPrevOrderAmount();
            }

            RefreshInventoryList();
            OrderVendor = "";
            _window.DeleteTempTab();
        }

        /// <summary>
        /// Sets order amounts to 0 in New Order datagrid
        /// </summary>
        /// <param name="obj"></param>
        private void ClearOrderAmounts(object obj)
        {
            foreach (InventoryItem item in FilteredInventoryItems)
            {
                item.LastOrderAmount = 0;
            }

            RefreshInventoryList();
        }

        /// <summary>
        /// Reset the check boxes in open orders data grid to unchecked
        /// </summary>
        /// <param name="obj"></param>
        private void ClearReceivedChecks(object obj)
        {
            foreach (PurchaseOrder order in OpenOrders)
            {
                order.Received = false;
            }

            NotifyPropertyChanged("OpenOrders");
        }

        /// <summary>
        /// Move the received orders
        /// </summary>
        /// <param name="obj"></param>
        private void MoveReceivedOrders(object obj)
        {
            foreach(PurchaseOrder order in OpenOrders.Where(x => x.Received))
            {
                order.ReceivedDate = DateTime.Now;
                order.Update();
            }

            LoadPreviousOrders();
        }

        private void ViewOrder(object obj)
        {
            _window.AddTempTab("PO#: " + SelectedOrder.Id, new NewOrder());
        }

        /// <summary>
        /// Display dialog to user to delete, unreceive, or partial receive received order and take appropriate action on user input
        /// </summary>
        /// <param name="obj"></param>
        private void RemoveReceivedOrder(object obj)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Display dialog to user to delete open order
        /// </summary>
        /// <param name="obj"></param>
        private void RemoveOpenOrder(object obj)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Open new tab to create a new order
        /// </summary>
        /// <param name="obj"></param>
        private void StartNewOrder(object obj)
        {
            _window.AddTempTab("New Order", new NewOrder());
        }
        #endregion

        #region Initializers
        public void InitializeWindow(MainWindow window)
        {
            _window = window;
        }

        public bool LoadDisplayItems()
        {
            _models = new ModelContainer();
            if (_models.InventoryItems != null)
            {
                FilteredInventoryItems = new ObservableCollection<InventoryItem>();

                foreach (InventoryItem item in _models.InventoryItems.OrderBy(x => x.Name))
                {
                    FilteredInventoryItems.Add(item);
                }

                _databaseFound = true;
                NotifyPropertyChanged("FilteredInventoryItems");

                return true;
            }

            return false;
        }

        /// <summary>
        /// Populate the 2 dataGrids in the Orders overview
        /// </summary>
        /// <returns></returns>
        private bool LoadPreviousOrders()
        {
            if (_models != null && _models.PurchaseOrders != null)
            {
                OpenOrders = new ObservableCollection<PurchaseOrder>(_models.PurchaseOrders.Where(x => !x.Received).OrderBy(x => x.OrderDate));
                ReceivedOrders = new ObservableCollection<PurchaseOrder>(_models.PurchaseOrders.Where(x => x.Received).OrderBy(x => x.ReceivedDate));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Display on the datagrids that the inventory items could not be found
        /// </summary>
        private void DisplayItemsNotFound()
        {
            FilteredInventoryItems = new ObservableCollection<InventoryItem>() { new InventoryItem() { Name = "Database not found" } };
            _databaseFound = false;
        }

        private void OrdersNotFound()
        {
            OpenOrders = new ObservableCollection<PurchaseOrder>() { new PurchaseOrder() { Company = "Orders not found" } };
            ReceivedOrders = new ObservableCollection<PurchaseOrder>() { new PurchaseOrder() { Company = "Orders not found" } };
        }

        /// <summary>
        /// Attempt to connect to the data - display a warning message in the datagrid if unsuccessful
        /// </summary>
        /// <returns></returns>
        private bool TryDBConnect()
        {
            if (!LoadDisplayItems())
            {
                DisplayItemsNotFound();
                return false;
            }
            if(!LoadPreviousOrders())
            {
                OrdersNotFound();
                return false;
            }
            return true;
        }

        //private void MakeBreakdownDisplay()
        //{
        //    BreakdownList = new ObservableCollection<BreakdownListItem>();

        //    string category = "";
        //    OrderTotal = 0;
        //    float categoryTotal = 0;
        //    foreach (InventoryItem item in _models.InventoryItems.Where(x => x.LastOrderAmount > 0).OrderBy(x => x.Category))
        //    {
        //        if(category != "" && item.Category != category)
        //        {
        //            BreakdownList.Add(new BreakdownListItem()
        //            {
        //                Name = category + " Total",
        //                Background = _models.GetCategoryColorHex(category),
        //                Cost = categoryTotal
        //            });
        //            categoryTotal = 0;
        //        }
        //        if (item.Category != category)
        //        {
        //            category = item.Category;
        //            BreakdownList.Add(new BreakdownListItem()
        //            {
        //                Name = item.Category,
        //                Background = _models.GetCategoryColorHex(category),
        //                IsHeader = true
        //            });
        //        }
        //        BreakdownList.Add(new BreakdownListItem()
        //        {
        //            Name = item.Name,
        //            Cost = item.PriceExtension,
        //            Background = _models.GetCategoryColorHex("default")
        //        });

        //        OrderTotal += item.PriceExtension;
        //        categoryTotal += item.PriceExtension;
        //    }
        //}
        private void MakeBreakdownDisplay()
        {
            BreakdownList = new ObservableCollection<BreakdownCategoryItem>();
            OrderTotal = 0;

            foreach (string category in _models.ItemCategories)
            {
                IEnumerable<InventoryItem> items = _models.InventoryItems.Where(x =>
                                                        x.Category.ToUpper() == category.ToUpper() && x.LastOrderAmount > 0
                                                    );
                if(items.Count() > 0)
                {
                    BreakdownCategoryItem bdItem = new BreakdownCategoryItem(items);
                    bdItem.Background = _models.GetCategoryColorHex(category);
                    BreakdownList.Add(bdItem);

                    OrderTotal += bdItem.TotalAmount;
                }
            }
        }
        #endregion

        #region Update UI Methods
        public void ClearErrors()
        {
            AddEditErrorMessage = "";
            foreach (FieldSetting field in FieldsCollection)
            {
                field.Error = 0;
            }

            NotifyPropertyChanged("FieldsCollection");
        }

        /// <summary>
        /// Update the datagrid displays for inventory items
        /// </summary>
        private void RefreshInventoryList()
        {
            FilteredInventoryItems = new ObservableCollection<InventoryItem>(_models.InventoryItems.OrderBy(x => x.Name));
        }

        /// <summary>
        /// Called when Master List is edited
        /// </summary>
        public void InventoryItemCountChanged()
        {
            ChangeCountCanExecute = true;
        }

        /// <summary>
        /// Called when New Order is edited
        /// </summary>
        public void InventoryOrderAmountChanged()
        {
            //NotifyPropertyChanged("FilteredInventoryItems");
            FilteredInventoryItems = new ObservableCollection<InventoryItem>(FilteredInventoryItems);
        }

        public void MoveOrderToReceived(PurchaseOrder po)
        {
            po.ReceivedDate = DateTime.Now;
            LoadPreviousOrders();
        }
        #endregion

        /// <summary>
        /// Saves the application settings when Save Settings button is pressed or the application is closed
        /// </summary>
        /// <param name="obj"></param>
        public void SaveSettings(object obj = null)
        {
            // TODO: add settings to save
            Properties.Settings.Default.DBLocation = DataFileFolder;
            Properties.Settings.Default.Save();

            TryDBConnect();
        }

        /// <summary>
        /// Filter list of inventory items based on the string in the filter box above datagrids
        /// </summary>
        /// <param name="filterStr"></param>
        public void FilterInventoryItems(string filterStr)
        {
            if (string.IsNullOrWhiteSpace(filterStr))
                FilteredInventoryItems = new ObservableCollection<InventoryItem>(_models.InventoryItems.OrderBy(x => x.Name));
            else
                FilteredInventoryItems = new ObservableCollection<InventoryItem>(_models.InventoryItems
                                                        .Where(x => x.Name.ToUpper().Contains(filterStr.ToUpper()))
                                                        .OrderBy(x => x.Name.ToUpper().IndexOf(filterStr.ToUpper())));
        }

        /// <summary>
        /// Collects the property names and values for display in the add/edit form
        /// </summary>
        /// <param name="type"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        private ObservableCollection<FieldSetting> GetFieldsAndValues<T>(T obj = null) where T : Model, new()
        {
            ObservableCollection<FieldSetting> fieldsAndVals = new ObservableCollection<FieldSetting>();
            string[] properties = new T().GetPropertiesDB();

            foreach (string prop in properties)
            {
                FieldSetting fs = new FieldSetting();
                fs.Name = prop;

                if (obj != null)
                    if (obj.GetPropertyValue(prop) != null)
                        fs.Value = obj.GetPropertyValue(prop).ToString();
                    else
                        fs.Value = "";
                else
                    fs.Value = "";

                fieldsAndVals.Add(fs);
            }

            return fieldsAndVals;
        }
    }

    #region Classes for display
    /// <summary>
    /// Class to store Model property names and values for display in add/edit form
    /// </summary>
    public class FieldSetting : INotifyPropertyChanged
    {
        private string _name;
        private string _value;
        private int _error = 0;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                NotifyPropertyChanged("Name");
            }
        }

        public string Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
                NotifyPropertyChanged("Value");
            }
        }

        public int Error
        {
            get
            {
                return _error;
            }
            set
            {
                _error = value;
                NotifyPropertyChanged("Error");
            }
        }
    }

    public class BreakdownCategoryItem
    {
        public string Background { get; set; }
        public string Category { get; set; }
        public float TotalAmount { get; set; }
        public ObservableCollection<InventoryItem> Items { get; set; }

        public BreakdownCategoryItem(IEnumerable<InventoryItem> items)
        {
            Items = new ObservableCollection<InventoryItem>(items);
            Category = Items.First().Category;

            TotalAmount = Items.Sum(x => x.PriceExtension);
        }
    }
    #endregion
}
