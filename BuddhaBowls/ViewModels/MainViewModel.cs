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

        public OrderTabVM OrderTab { get; set; }
        public InventoryTabVM InventoryTab { get; set; }

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
        
        private bool _dbConnected;
        #endregion

        #region ICommand Bindings and Can Execute
        // Browse button in settings form
        public ICommand BrowseButtonCommand { get; set; }
        // Generate Report button in settings form
        public ICommand ReportCommand { get; set; }
        // Save button in Settings form
        public ICommand SaveSettingsCommand { get; set; }

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

        #endregion

        public MainViewModel()
        {
            BrowseButtonCommand = new RelayCommand(BrowseHelper);
            ReportCommand = new RelayCommand(ReportHelper, x => ReportCanExecute);
            //AddInventoryItemCommand = new RelayCommand(AddInventoryItem, x => AddItemCanExecute);
            //DeleteInventoryItemCommand = new RelayCommand(DeleteInventoryItem, x => DeleteEditCanExecute);
            //EditInventoryItemCommand = new RelayCommand(EditInventoryItem, x => DeleteEditCanExecute);
            //SaveAddEditCommand = new RelayCommand(SaveAddEdit, x => SaveAddEditCanExecute);
            //CancelAddEditCommand = new RelayCommand(CancelAddEdit);
            //SaveCountCommand = new RelayCommand(SaveCount, x => ChangeCountCanExecute);
            //ResetCountCommand = new RelayCommand(ResetCount, x => ChangeCountCanExecute);
            SaveSettingsCommand = new RelayCommand(SaveSettings, x => SaveSettingsCanExecute);

            _models = new ModelContainer();

            OrderTab = new OrderTabVM(_models, this);
            InventoryTab = new InventoryTabVM(_models, this);

            //MakeBreakdownDisplay();
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
        #endregion

        #region Initializers
        public void InitializeWindow(MainWindow window)
        {
            _window = window;
            OrderTab.InitializeWindow(window);
            InventoryTab.InitializeWindow(window);
        }

        //public bool LoadDisplayItems()
        //{
        //    _models = new ModelContainer();
        //    if (_models.InventoryItems != null)
        //    {
        //        FilteredInventoryItems = new ObservableCollection<InventoryItem>();

        //        foreach (InventoryItem item in _models.InventoryItems.OrderBy(x => x.Name))
        //        {
        //            FilteredInventoryItems.Add(item);
        //        }

        //        _databaseFound = true;
        //        NotifyPropertyChanged("FilteredInventoryItems");

        //        return true;
        //    }

        //    return false;
        //}

        ///// <summary>
        ///// Display on the datagrids that the inventory items could not be found
        ///// </summary>
        //private void DisplayItemsNotFound()
        //{
        //    FilteredInventoryItems = new ObservableCollection<InventoryItem>() { new InventoryItem() { Name = "Database not found" } };
        //    _databaseFound = false;
        //}

        ///// <summary>
        ///// Attempt to connect to the data - display a warning message in the datagrid if unsuccessful
        ///// </summary>
        ///// <returns></returns>
        //private bool TryDBConnect()
        //{
        //    if (!LoadDisplayItems())
        //    {
        //        DisplayItemsNotFound();
        //        return false;
        //    }
        //    return true;
        //}
        #endregion

        //#region Update UI Methods
        //public void ClearErrors()
        //{
        //    AddEditErrorMessage = "";
        //    foreach (FieldSetting field in FieldsCollection)
        //    {
        //        field.Error = 0;
        //    }

        //    NotifyPropertyChanged("FieldsCollection");
        //}

        ///// <summary>
        ///// Update the datagrid displays for inventory items
        ///// </summary>
        //private void RefreshInventoryList()
        //{
        //    FilteredInventoryItems = new ObservableCollection<InventoryItem>(_models.InventoryItems.OrderBy(x => x.Name));
        //}

        ///// <summary>
        ///// Called when Master List is edited
        ///// </summary>
        //public void InventoryItemCountChanged()
        //{
        //    ChangeCountCanExecute = true;
        //}
        //#endregion

        /// <summary>
        /// Saves the application settings when Save Settings button is pressed or the application is closed
        /// </summary>
        /// <param name="obj"></param>
        public void SaveSettings(object obj = null)
        {
            // TODO: add settings to save
            Properties.Settings.Default.DBLocation = DataFileFolder;
            Properties.Settings.Default.Save();
        }

        ///// <summary>
        ///// Filter list of inventory items based on the string in the filter box above datagrids
        ///// </summary>
        ///// <param name="filterStr"></param>
        //public void FilterInventoryItems(string filterStr)
        //{
        //    if (string.IsNullOrWhiteSpace(filterStr))
        //        FilteredInventoryItems = new ObservableCollection<InventoryItem>(_models.InventoryItems.OrderBy(x => x.Name));
        //    else
        //        FilteredInventoryItems = new ObservableCollection<InventoryItem>(_models.InventoryItems
        //                                                .Where(x => x.Name.ToUpper().Contains(filterStr.ToUpper()))
        //                                                .OrderBy(x => x.Name.ToUpper().IndexOf(filterStr.ToUpper())));
        //}

        ///// <summary>
        ///// Collects the property names and values for display in the add/edit form
        ///// </summary>
        ///// <param name="type"></param>
        ///// <param name="obj"></param>
        ///// <returns></returns>
        //private ObservableCollection<FieldSetting> GetFieldsAndValues<T>(T obj = null) where T : Model, new()
        //{
        //    ObservableCollection<FieldSetting> fieldsAndVals = new ObservableCollection<FieldSetting>();
        //    string[] properties = new T().GetPropertiesDB();

        //    foreach (string prop in properties)
        //    {
        //        FieldSetting fs = new FieldSetting();
        //        fs.Name = prop;

        //        if (obj != null)
        //            if (obj.GetPropertyValue(prop) != null)
        //                fs.Value = obj.GetPropertyValue(prop).ToString();
        //            else
        //                fs.Value = "";
        //        else
        //            fs.Value = "";

        //        fieldsAndVals.Add(fs);
        //    }

        //    return fieldsAndVals;
        //}
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
    #endregion
}
