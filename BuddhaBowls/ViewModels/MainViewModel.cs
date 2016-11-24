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

        ObservableCollection<InventoryItem> _filteredInventoryItems;
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

        public ObservableCollection<FieldSetting> FieldsCollection { get; set; }
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

        public bool ReportCanExecute
        {
            get
            {
                return Directory.Exists(DataFileFolder) && _databaseFound;
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
            SaveSettingsCommand = new RelayCommand(SaveSettings, x => ReportCanExecute);

            _models = new ModelContainer();
            if (_models.InventoryItems != null)
                LoadInventoryItems();
            else
                InventoryItemsNotFound();
        }

        private void InventoryItemsNotFound()
        {
            FilteredInventoryItems = new ObservableCollection<InventoryItem>() { new InventoryItem() { Name = "Database not found" } };
            _databaseFound = false;
        }

        #region ICommand Helpers
        private void ReportHelper(object obj)
        {
            ReportGenerator generator = new ReportGenerator();
            //generator.FillInventoryId("Mac & Cheese");
            //generator.CreateBatchRecipeReport("Mac & Cheese");
            //generator.MakeMasterInventoryTable();
            try
            {
                generator.CreateMasterInventoryReport();
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
        }

        public void LoadInventoryItems()
        {
            FilteredInventoryItems = new ObservableCollection<InventoryItem>();

            foreach(InventoryItem item in _models.InventoryItems.OrderBy(x => x.Name))
            {
                FilteredInventoryItems.Add(item);
            }

            _databaseFound = true;
            NotifyPropertyChanged("FilteredInventoryItems");
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
}
