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
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BuddhaBowls
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private MainWindow _window;
        private ModelContainer _models;
        private Thread _thread;

        public Thread ExcelThread;

        // INotifyPropertyChanged event and method
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool DatabaseFound { get; set; }

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
        private ObservableCollection<IItem> _filteredInventoryItems;
        public ObservableCollection<IItem> FilteredInventoryItems
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

        private OrderTabVM _orderTab;
        public OrderTabVM OrderTab
        {
            get
            {
                return _orderTab;
            }
            set
            {
                _orderTab = value;
                NotifyPropertyChanged("OrderTab");
            }
        }

        private InventoryTabVM _inventoryTab;
        public InventoryTabVM InventoryTab
        {
            get
            {
                return _inventoryTab;
            }
            set
            {
                _inventoryTab = value;
                NotifyPropertyChanged("InventoryTab");
            }
        }

        private VendorTabVM _vendorTab;
        public VendorTabVM VendorTab
        {
            get
            {
                return _vendorTab;
            }
            set
            {
                _vendorTab = value;
                NotifyPropertyChanged("VendorTab");
            }
        }

        private RecipeTabVM _recipeTab;
        public RecipeTabVM RecipeTab
        {
            get
            {
                return _recipeTab;
            }
            set
            {
                _recipeTab = value;
                NotifyPropertyChanged("RecipeTab");
            }
        }

        private Visibility _modalVisibility = Visibility.Hidden;
        public Visibility ModalVisibility
        {
            get
            {
                return _modalVisibility;
            }
            set
            {
                _modalVisibility = value;
                NotifyPropertyChanged("ModalVisibility");
            }
        }

        private object _modalContext;
        public object ModalContext
        {
            get
            {
                return _modalContext;
            }
            set
            {
                _modalContext = value;
                NotifyPropertyChanged("ModalContext");
            }
        }
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
                return Directory.Exists(DataFileFolder) && DatabaseFound;
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
            SaveSettingsCommand = new RelayCommand(SaveSettingsHelper, x => SaveSettingsCanExecute);

            ModalContext = this;

            InitTabsAndModel();
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
                MessageBox.Show("Error has occurred generating report:\n" + e.Message, "Report Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
        /// Saves the application settings when Save Settings button is pressed or the application is closed
        /// </summary>
        /// <param name="obj"></param>
        private void SaveSettingsHelper(object obj)
        {
            SaveSettings();
            //ModelContainer.ChangeContainer(null);
            _models = new ModelContainer();
            InitTabsAndModel();
        }
        #endregion

        #region Initializers

        public void InitializeWindow(MainWindow window)
        {
            _window = window;
        }

        private void InitTabsAndModel()
        {
            _models = new ModelContainer();
            DatabaseFound = _models.InventoryItems != null && _models.InventoryItems.Count > 0;
            TabVM.ParentContext = this;
            TabVM.IsDBConnected = DatabaseFound;
            TabVM.SetModelContainer(_models);

            OrderTab = new OrderTabVM();
            InventoryTab = new InventoryTabVM();
            VendorTab = new VendorTabVM();
            RecipeTab = new RecipeTabVM();
        }

        #endregion

        /// <summary>
        /// Replaces the temporary tab (for adding and editing stuff) with some new tab
        /// </summary>
        /// <param name="tab">New tab</param>
        public void ReplaceTempTab(UserControl tab)
        {
            ModalContext = this;
            ModalVisibility = Visibility.Hidden;
            _window.ReplaceTempTab(tab);
        }

        /// <summary>
        /// Remove the temporary tab
        /// </summary>
        public void RemoveTempTab()
        {
            ModalContext = this;
            ModalVisibility = Visibility.Hidden;
            _window.RemoveTempTab();
        }

        /// <summary>
        /// Add a new temporary tab (only used if one does not already exist)
        /// </summary>
        /// <param name="tab"></param>
        public void AppendTempTab(UserControl tab)
        {
            ModalContext = this;
            ModalVisibility = Visibility.Hidden;
            _window.AppendTempTab(tab);
        }

        /// <summary>
        /// Collects the property names and values for display in the add/edit form
        /// </summary>
        /// <param name="type"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        //public ObservableCollection<FieldSetting> GetFieldsAndValues<T>(T obj = null) where T : Model, new()
        //{
        //    ObservableCollection<FieldSetting> fieldsAndVals = new ObservableCollection<FieldSetting>();
        //    string[] properties = new T().GetPropertiesDB(new string[] { "Id" });

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

        /// <summary>
        /// Run all refresh methods for all permanent tabs
        /// </summary>
        public void Refresh()
        {
            OrderTab.RefreshOrderList();
            InventoryTab.Refresh();
            VendorTab.RefreshVendorList();
            RecipeTab.RefreshList();

            foreach (TempTabVM tempVM in TempTabVM.TabStack.Select(x => x.DataContext))
            {
                tempVM.Refresh();
            }
        }

        /// <summary>
        /// Looks through fields in add/edit form to ensure that user-supplied values are valid and changes types when necessary
        /// </summary>
        /// <remarks>Probably will deprecate as I refactor new/edit recipe</remarks>
        /// <param name="item"></param>
        /// <returns></returns>
        public string ObjectFromFields<T>(ref T item, IEnumerable<FieldSetting> fieldsCollection, bool newItem) where T : Model, new()
        {
            foreach (FieldSetting field in fieldsCollection)
            {
                if (!item.IsNullable(field.Name) && string.IsNullOrWhiteSpace(field.Value))
                {
                    field.Error = 1;
                    return field.Name + " must be set";
                }

                if (field.Name == "Name" && _models.InventoryItems.FirstOrDefault(x => x.Name.ToUpper() == field.Value.ToUpper()) != null &&
                   newItem)
                {
                    field.Error = 1;
                    return field.Value + " already exists in the database";
                }

                if (item.GetPropertyType(field.Name) == typeof(int))
                {
                    int val = 0;
                    int.TryParse(field.Value, out val);

                    if (val == 0)
                    {
                        field.Error = 1;
                        return field.Name + " must be an integer";
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
                        return field.Name + " must be a number";
                    }

                    field.Value = (val / divisor).ToString();
                }

                field.Error = 0;
                item.SetProperty(field.Name, field.Value);
            }
            return "";
        }

        public void SaveSettings()
        {
            // TODO: add settings to save
            Properties.Settings.Default.DBLocation = DataFileFolder;
            Properties.Settings.Default.Save();
        }

        public void GeneratePO(PurchaseOrder po, Vendor vendor)
        {
            _thread = new Thread(delegate ()
            {
                ReportGenerator generator = new ReportGenerator(_models);
                string xlsPath = generator.GenerateOrder(po, vendor);
                generator.Close();
                System.Diagnostics.Process.Start(xlsPath);
            });
            _thread.Start();
        }

        public void GenerateVendorOrderList(Vendor vendor, bool open = true)
        {
            _thread = new Thread(delegate ()
            {
                ReportGenerator generator = new ReportGenerator(_models);
                string xlsPath = generator.GenerateVendorOrderSheet(vendor);
                generator.Close();
                if(open)
                    System.Diagnostics.Process.Start(xlsPath);
            });
            _thread.Start();
        }

        public void GenerateReceivingList(PurchaseOrder po, Vendor vendor)
        {
            _thread = new Thread(delegate ()
            {
                ReportGenerator generator = new ReportGenerator(_models);
                string xlsPath = generator.GenerateReceivingList(po, vendor);
                generator.Close();
                System.Diagnostics.Process.Start(xlsPath);
            });
            _thread.Start();
        }

        /// <summary>
        /// Only use for testing
        /// </summary>
        /// <returns></returns>
        public ModelContainer GetModelContainer()
        {
            return _models;
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

        public FieldSetting() { }

        public FieldSetting(string name)
        {
            Name = name;
            Error = 0;
            Value = "";
        }
    }
    #endregion
}
