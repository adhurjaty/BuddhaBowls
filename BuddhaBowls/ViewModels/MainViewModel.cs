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

        //List<OrderItem> _filteredOrderItems;
        //public List<OrderItem> FilteredOrderItems
        //{
        //    get
        //    {
        //        return _filteredOrderItems;
        //    }
        //    set
        //    {
        //        _filteredOrderItems = value;
        //        NotifyPropertyChanged("FilteredOrderItems");
        //    }
        //}

        public ObservableCollection<FieldSetting> FieldsCollection { get; set; }

        public string OrderVendor { get; set; }

        private bool _dbConnected;

        private ObservableCollection<BreakdownListItem> _priceBreakdown;
        public ObservableCollection<BreakdownListItem> PriceBreakdown
        {
            get
            {
                return _priceBreakdown;
            }
            set
            {
                _priceBreakdown = value;
                NotifyPropertyChanged("PriceBreakdown");
            }
        }
        #endregion

        #region ICommand Bindings
        public ICommand BrowseButtonCommand { get; set; }
        public ICommand ReportCommand { get; set; }
        public ICommand AddInventoryItemCommand { get; set; }
        public ICommand DeleteInventoryItemCommand { get; set; }
        public ICommand EditInventoryItemCommand { get; set; }
        public ICommand SaveAddEditCommand { get; set; }
        public ICommand CancelAddEditCommand { get; set; }
        public ICommand SaveSettingsCommand { get; set; }
        public ICommand SaveCountCommand { get; set; }
        public ICommand ResetCountCommand { get; set; }
        public ICommand SaveNewOrderCommand { get; set; }
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
        public bool SaveOrderCanExecute { get; private set; }
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
            InitPriceBreakdown();
        }

        #region ICommand Helpers
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

        private void AddInventoryItem(object obj)
        {
            AddEditHeader = "Add New Inventory Item";
            FieldsCollection = GetFieldsAndValues(typeof(InventoryItem));

            _window.DeleteEditAddTab();
            _window.AddTab("Add Inventory Item");
        }

        private void EditInventoryItem(object obj)
        {
            AddEditHeader = "Edit " + SelectedInventoryItem.Name;
            FieldsCollection = GetFieldsAndValues(SelectedInventoryItem.GetType(), SelectedInventoryItem);

            _window.DeleteEditAddTab();
            _window.AddTab("Edit Inventory Item");
        }

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

        private void CancelAddEdit(object obj)
        {
            AddEditErrorMessage = "";
            _window.DeleteEditAddTab();
        }

        private void SaveAddEdit(object obj)
        {
            InventoryItem item = null;
            if (AddEditHeader.StartsWith("Edit"))
                item = SelectedInventoryItem;
            else if (AddEditHeader.StartsWith("Add"))
                item = new InventoryItem();
            else
                throw new NotImplementedException();

            foreach(FieldSetting field in FieldsCollection)
            {
                if(!item.IsNullable(field.Name) && string.IsNullOrWhiteSpace(field.Value))
                {
                    field.Error = 1;
                    AddEditErrorMessage = field.Name + " must be set";
                    return;
                }

                if(field.Name == "Name" && _models.InventoryItems.FirstOrDefault(x => x.Name.ToUpper() == field.Value.ToUpper()) != null &&
                   AddEditHeader.StartsWith("Add"))
                {
                    field.Error = 1;
                    AddEditErrorMessage = field.Value + " already exists in the database";
                    return;
                }

                if (item.GetPropertyType(field.Name) == typeof(int))
                {
                    int val = 0;
                    int.TryParse(field.Value, out val);

                    if(val == 0)
                    {
                        field.Error = 1;
                        AddEditErrorMessage = field.Name + " must be an integer";
                        return;
                    }
                }

                if (item.GetPropertyType(field.Name) == typeof(float))
                {
                    float val = 0;
                    float divisor = 1f;
                    string valueStr = field.Value;

                    if(valueStr.EndsWith("%"))
                    {
                        valueStr = valueStr.Remove(valueStr.Length - 1);
                        divisor = 100f;
                    }
                    else if(valueStr.StartsWith("$"))
                    {
                        valueStr = valueStr.Remove(0, 1);
                    }

                    float.TryParse(valueStr, out val);

                    if(val == 0)
                    {
                        field.Error = 1;
                        AddEditErrorMessage = field.Name + " must be a number";
                        return;
                    }

                    field.Value = (val / divisor).ToString();
                }

                field.Error = 0;
                item.SetProperty(field.Name, field.Value);
            }

            if (AddEditHeader.StartsWith("Edit"))
                item.Update();
            else if (AddEditHeader.StartsWith("Add"))
            {
                _models.InventoryItems.Add(item);
                item.Insert();
            }
            else
                throw new NotImplementedException();

            AddEditErrorMessage = "";
            _window.DeleteEditAddTab();
            RefreshInventoryList();
        }

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

            OrderVendor = "";
        }

        private void CancelOrder(object obj)
        {
            foreach (InventoryItem item in FilteredInventoryItems.Where(x => x.orderAmountUpdated))
            {
                item.Count = item.GetPrevOrderAmount();
                item.orderAmountUpdated = false;
            }

            RefreshInventoryList();
            CancelOrderCanExecute = false;
            OrderVendor = "";
        }
        #endregion

        public void InitializeWindow(MainWindow window)
        {
            _window = window;
        }

        public void SaveSettings(object obj = null)
        {
            // TODO: add settings to save
            Properties.Settings.Default.DBLocation = DataFileFolder;
            Properties.Settings.Default.Save();

            TryDBConnect();
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

        private void DisplayItemsNotFound()
        {
            FilteredInventoryItems = new ObservableCollection<InventoryItem>() { new InventoryItem() { Name = "Database not found" } };
            //FilteredOrderItems = new List<OrderItem>() { new OrderItem() { Name = "Database not found" } };
            _databaseFound = false;
        }

        private void InitPriceBreakdown()
        {
            _breakdownDict = new Dictionary<string, List<BreakdownListItem>>();
            foreach(InventoryItem item in _models.InventoryItems)
            {
                if(!_breakdownDict.Keys.Contains(item.Category))
                {
                    _breakdownDict[item.Category] = new List<BreakdownListItem>();
                }
                if (item.LastOrderAmount > 0)
                {
                    _breakdownDict[item.Category].Add(new BreakdownListItem()
                    {
                        Name = item.Name,
                        Background = MainHelper.ColorFromString(GlobalVar.BLANK_COLOR),
                        Cost = item.PriceExtension
                    });
                }
            }

            SetPriceBreakdownList();
        }

        private void SetPriceBreakdownList()
        {
            PriceBreakdown = new ObservableCollection<BreakdownListItem>();

            foreach (string key in _breakdownDict.Keys.OrderBy(x => x))
            {
                PriceBreakdown.Add(new BreakdownListItem() { Name = key, Background = _models.GetColorFromCategory(key) });
                foreach (BreakdownListItem item in _breakdownDict[key])
                {
                    PriceBreakdown.Add(new BreakdownListItem()
                    {
                        Name = item.Name,
                        Background = item.Background,
                        Cost = item.Cost
                    });
                }
            }
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

        //public void FilterOrderItems(string filterStr)
        //{
        //    // really inefficient use of resources
        //    if (string.IsNullOrWhiteSpace(filterStr))
        //        FilteredOrderItems = _models.InventoryItems.OrderBy(x => x.Name).Select(x => (OrderItem)x).ToList();
        //    else
        //        FilteredOrderItems = _models.InventoryItems.Where(x => x.Name.ToUpper().Contains(filterStr.ToUpper()))
        //                                                .OrderBy(x => x.Name.ToUpper().IndexOf(filterStr.ToUpper()))
        //                                                .Select(x => (OrderItem)x).ToList();
        //}

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
