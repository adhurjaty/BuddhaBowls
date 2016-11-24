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
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BuddhaBowls
{
    public class MainViewModel : INotifyPropertyChanged
    {
        MainWindow _window;
        ModelContainer _models;

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

        public ObservableCollection<InventoryItem> FilteredInventoryItems { get; set; }
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

        public bool ReportCanExecute
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
                return SelectedInventoryItem != null;
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
            AddInventoryItemCommand = new RelayCommand(AddInventoryItem);
            DeleteInventoryItemCommand = new RelayCommand(DeleteInventoryItem, x => DeleteEditCanExecute);
            EditInventoryItemCommand = new RelayCommand(EditInventoryItem, x => DeleteEditCanExecute);
            SaveAddEditCommand = new RelayCommand(SaveAddEdit, x => SaveAddEditCanExecute);
            CancelAddEditCommand = new RelayCommand(CancelAddEdit);

            _models = new ModelContainer();
            LoadInventoryItems();
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
                field.Error = 0;
                item.SetProperty(field.Name, field.Value);
            }

            AddEditErrorMessage = "";

            if (AddEditHeader.StartsWith("Edit"))
                item.Update();
            else if (AddEditHeader.StartsWith("Add"))
                item.Insert();
            else
                throw new NotImplementedException();

            AddEditErrorMessage = "";
            _window.DeleteEditAddTab();
        }
        #endregion

        public void InitializeWindow(MainWindow window)
        {
            _window = window;
        }

        public void SaveSettings()
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
            NotifyPropertyChanged("FilteredInventoryItems");
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
