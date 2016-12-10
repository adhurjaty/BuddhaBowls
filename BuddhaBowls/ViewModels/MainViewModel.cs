﻿using BuddhaBowls.Helpers;
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
    public delegate void ModelPropertyChanged(object sender);

    public class MainViewModel : INotifyPropertyChanged
    {
        private MainWindow _window;
        private ModelContainer _models;
        private bool _databaseFound;
        private Dictionary<string, List<BreakdownListItem>> _breakdownDict;

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

        // name of the vendor in the New Order form
        public string OrderVendor { get; set; }

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
        public bool CancelOrderCanExecute { get; set; } = false;
        public bool SaveOrderCanExecute
        {
            get
            {
                return !string.IsNullOrWhiteSpace(OrderVendor) && _models.InventoryItems.FirstOrDefault(x => x.LastOrderAmount > 0) != null;
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
            CancelNewOrderCommand = new RelayCommand(CancelOrder, x => CancelOrderCanExecute);

            _dbConnected = TryDBConnect();
            InitChangedEvents();
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
            FieldsCollection = GetFieldsAndValues(typeof(InventoryItem));

            _window.DeleteEditAddTab();
            _window.AddTab("Add Inventory Item");
        }

        /// <summary>
        /// Creates a new tab called Edit Inventory Item and populates form - Edit button
        /// </summary>
        /// <param name="obj"></param>
        private void EditInventoryItem(object obj)
        {
            AddEditHeader = "Edit " + SelectedInventoryItem.Name;
            FieldsCollection = GetFieldsAndValues(SelectedInventoryItem.GetType(), SelectedInventoryItem);

            _window.DeleteEditAddTab();
            _window.AddTab("Edit Inventory Item");
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
            _window.DeleteEditAddTab();
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
                _window.DeleteEditAddTab();
                RefreshInventoryList();
            }
        }

        /// <summary>
        /// Resets the inventory count to the saved value before changing the datagrid
        /// </summary>
        /// <param name="obj"></param>
        private void ResetCount(object obj)
        {
            foreach (InventoryItem item in FilteredInventoryItems.Where(x => x.countUpdated))
            {
                item.Count = item.GetLastCount();
                item.countUpdated = false;
            }

            RefreshInventoryList();
            ChangeCountCanExecute = false;
        }

        private void SaveCount(object obj)
        {
            foreach(InventoryItem item in FilteredInventoryItems.Where(x => x.countUpdated))
            {
                item.Update();
            }

            ChangeCountCanExecute = false;
        }

        private void SaveOrder(object obj)
        {
            foreach(InventoryItem item in FilteredInventoryItems.Where(x => x.orderAmountUpdated))
            {
                item.Update();
            }

            PurchaseOrder po = new PurchaseOrder(OrderVendor, _models.InventoryItems.Where(x => x.LastOrderAmount > 0).ToList());

            OrderVendor = "";
        }

        private void CancelOrder(object obj)
        {
            foreach (InventoryItem item in FilteredInventoryItems.Where(x => x.orderAmountUpdated))
            {
                item.LastOrderAmount = item.GetPrevOrderAmount();
                item.orderAmountUpdated = false;
            }

            RefreshInventoryList();
            CancelOrderCanExecute = false;
            OrderVendor = "";
        }
        #endregion

        #region Initializers
        public void InitializeWindow(MainWindow window)
        {
            _window = window;
        }


        public void LoadDisplayItems()
        {
            FilteredInventoryItems = new ObservableCollection<InventoryItem>();
            //FilteredOrderItems = new List<OrderItem>();

            foreach(InventoryItem item in _models.InventoryItems.OrderBy(x => x.Name))
            {
                FilteredInventoryItems.Add(item);
                //FilteredOrderItems.Add((OrderItem)((IItem)item));
            }

            _databaseFound = true;
            NotifyPropertyChanged("FilteredInventoryItems");
            NotifyPropertyChanged("FilteredOrderItems");
        }

        private void InitChangedEvents()
        {
            if (_models != null && _models.InventoryItems != null)
            {
                foreach (InventoryItem item in _models.InventoryItems)
                {
                    item.CountChanged = InventoryItemCountChanged;
                    item.OrderAmountChanged = InventoryOrderAmountChanged;
                }
            }
        }
        #endregion

        public void SaveSettings(object obj = null)
        {
            // TODO: add settings to save
            Properties.Settings.Default.DBLocation = DataFileFolder;
            Properties.Settings.Default.Save();

            TryDBConnect();
        }

        private void DisplayItemsNotFound()
        {
            FilteredInventoryItems = new ObservableCollection<InventoryItem>() { new InventoryItem() { Name = "Database not found" } };
            //FilteredOrderItems = new List<OrderItem>() { new OrderItem() { Name = "Database not found" } };
            _databaseFound = false;
        }

        public void FilterInventoryItems(string filterStr)
        {
            if (string.IsNullOrWhiteSpace(filterStr))
                FilteredInventoryItems = new ObservableCollection<InventoryItem>(_models.InventoryItems.OrderBy(x => x.Name));
            else
                FilteredInventoryItems = new ObservableCollection<InventoryItem>(_models.InventoryItems
                                                        .Where(x => x.Name.ToUpper().Contains(filterStr.ToUpper()))
                                                        .OrderBy(x => x.Name.ToUpper().IndexOf(filterStr.ToUpper())));
        }

        public void ClearErrors()
        {
            AddEditErrorMessage = "";
            foreach(FieldSetting field in FieldsCollection)
            {
                field.Error = 0;
            }

            NotifyPropertyChanged("FieldsCollection");
        }

        private ObservableCollection<FieldSetting> GetFieldsAndValues(Type type, object obj = null)
        {
            ObservableCollection<FieldSetting> fieldsAndVals = new ObservableCollection<FieldSetting>();

            foreach (PropertyInfo prop in type.GetProperties().Where(x => x.Name != "Id"))
            {
                FieldSetting fs = new FieldSetting();
                fs.Name = prop.Name;

                if (obj != null)
                    if (prop.GetValue(obj) != null)
                        fs.Value = prop.GetValue(obj).ToString();
                    else
                        fs.Value = "";
                else
                    fs.Value = "";

                fieldsAndVals.Add(fs);
            }

            return fieldsAndVals;
        }

        private void RefreshInventoryList()
        {
            FilteredInventoryItems = new ObservableCollection<InventoryItem>(_models.InventoryItems.OrderBy(x => x.Name));
        }

        private bool TryDBConnect()
        {
            _models = new ModelContainer();
            if (_models.InventoryItems != null)
            {
                LoadDisplayItems();
                return true;
            }
            else
                DisplayItemsNotFound();
            return false;
        }

        private void InventoryItemCountChanged(object sender)
        {
            ChangeCountCanExecute = true;
        }

        private void InventoryOrderAmountChanged(object sender)
        {
            //NotifyPropertyChanged("FilteredInventoryItems");
            FilteredInventoryItems = new ObservableCollection<InventoryItem>(FilteredInventoryItems);
            CancelOrderCanExecute = true;
        }

        
    }

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

    public class BreakdownListItem
    {
        public long Background { get; set; }
        public string Name { get; set; }
        public float Cost { get; set; }
    }
}
