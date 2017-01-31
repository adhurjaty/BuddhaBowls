using BuddhaBowls.Helpers;
using BuddhaBowls.Models;
using BuddhaBowls.Services;
using BuddhaBowls.UserControls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace BuddhaBowls
{
    public class InventoryTabVM : INotifyPropertyChanged, ITabVM
    {
        private enum PageState { Inventory, Batch, Menu }

        private ModelContainer _models;
        private bool _databaseFound;
        private PageState _state;

        // INotifyPropertyChanged event and method
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainViewModel ParentContext { get; set; }

        #region Data Bindings
        private string _header;
        public string Header
        {
            get
            {
                return _header;
            }
            set
            {
                _header = value;
                NotifyPropertyChanged("Header");
            }
        }

        // Inventory item selected in the datagrids for Orders and Master List
        private IItem _selectedInventoryItem;
        public IItem SelectedInventoryItem
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

        // Collection of fields and values for use in Model edit forms
        public ObservableCollection<FieldSetting> FieldsCollection { get; set; }

        private Visibility _savedVisibility = Visibility.Hidden;
        public Visibility SavedVisibility
        {
            get
            {
                return _savedVisibility;
            }
            set
            {
                _savedVisibility = value;
                NotifyPropertyChanged("SavedVisibility");
            }
        }

        public DateTime InventoryDate { get; set; } = DateTime.Now;
        #endregion

        #region ICommand Bindings and Can Execute
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
        // Change display order button in Master inventory form
        public ICommand ChangeOrderCommand { get; set; }
        // Inventory section button
        public ICommand InventorySectionCommand { get; set; }
        // Batch Items section button
        public ICommand BatchSectionCommand { get; set; }
        // Menu Item section button
        public ICommand MenuSectionCommand { get; set; }

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

        public bool ChangeCountCanExecute { get; set; } = true;

        public bool InventoryPageCanExecute
        {
            get
            {
                return _state != PageState.Inventory;
            }
        }

        public bool BatchPageCanExecute
        {
            get
            {
                return _state != PageState.Batch;
            }
        }

        public bool MenuPageCanExecute
        {
            get
            {
                return _state != PageState.Menu;
            }
        }

        #endregion

        public InventoryTabVM(ModelContainer models, MainViewModel parent)
        {
            ParentContext = parent;
            _models = models;

            AddInventoryItemCommand = new RelayCommand(AddInventoryItem, x => AddItemCanExecute);
            DeleteInventoryItemCommand = new RelayCommand(DeleteInventoryItem, x => DeleteEditCanExecute);
            EditInventoryItemCommand = new RelayCommand(EditInventoryItem, x => DeleteEditCanExecute);
            SaveAddEditCommand = new RelayCommand(SaveAddEdit, x => SaveAddEditCanExecute);
            CancelAddEditCommand = new RelayCommand(CancelAddEdit);
            SaveCountCommand = new RelayCommand(SaveCount, x => ChangeCountCanExecute);
            ResetCountCommand = new RelayCommand(ResetCount, x => ChangeCountCanExecute);
            ChangeOrderCommand = new RelayCommand(StartChangeOrder);
            InventorySectionCommand = new RelayCommand(ChangeToInventory, x => InventoryPageCanExecute);
            BatchSectionCommand = new RelayCommand(ChangeToBatch, x => BatchPageCanExecute);
            MenuSectionCommand = new RelayCommand(ChangeToMenu, x => MenuPageCanExecute);

            TryDBConnect();
            ChangePageState(PageState.Inventory);
        }

        #region ICommand Helpers
        /// <summary>
        /// Creates new tab called Add Inventory Item and populates form - Plus button
        /// </summary>
        /// <param name="obj"></param>
        private void AddInventoryItem(object obj)
        {
            AddEditHeader = "Add New Inventory Item";
            FieldsCollection = ParentContext.GetFieldsAndValues<InventoryItem>();

            ParentContext.AddTempTab("Add Inventory Item", new EditItem(this, ClearErrors));
        }

        /// <summary>
        /// Creates a new tab called Edit Inventory Item and populates form - Edit button
        /// </summary>
        /// <param name="obj"></param>
        private void EditInventoryItem(object obj)
        {
            AddEditHeader = "Edit " + SelectedInventoryItem.Name;
            FieldsCollection = ParentContext.GetFieldsAndValues((InventoryItem)SelectedInventoryItem);

            ParentContext.AddTempTab("Edit Inventory Item", new EditItem(this, ClearErrors));
        }

        /// <summary>
        /// Presents user with warning dialog, then removes item from DB and in-memory list - Minus button
        /// </summary>
        /// <param name="obj"></param>
        private void DeleteInventoryItem(object obj)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete " + SelectedInventoryItem.Name,
                                                      "Delete " + SelectedInventoryItem.Name + "?", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                _models.InventoryItems.Remove((InventoryItem)SelectedInventoryItem);
                SelectedInventoryItem.Destroy();
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
            ParentContext.DeleteTempTab();
        }

        /// <summary>
        /// Checks for invalid input, then saves the input values to DB
        /// </summary>
        /// <param name="obj"></param>
        private void SaveAddEdit(object obj)
        {
            InventoryItem item = null;
            if (AddEditHeader.StartsWith("Edit"))
                item = (InventoryItem)SelectedInventoryItem;
            else if (AddEditHeader.StartsWith("Add"))
                item = new InventoryItem();
            else
                throw new NotImplementedException();

            AddEditErrorMessage = ParentContext.SetOrErrorAddEditItem(ref item, FieldsCollection, AddEditHeader.StartsWith("Add"));
            if (string.IsNullOrEmpty(AddEditErrorMessage))
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
                ParentContext.DeleteTempTab();
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
            foreach (InventoryItem item in FilteredInventoryItems)
            {
                item.Update();
            }
            string tableName = @"Inventory History\Inventory_" + InventoryDate.ToString("MM-dd-yyyy");
            ModelHelper.CreateTable(_models.InventoryItems.OrderBy(x => x.Id).ToList(), tableName);
            ChangeCountCanExecute = false;
            FlashSavedMessage();
        }

        private void StartChangeOrder(object obj)
        {
            RefreshInventoryList();
            ParentContext.AddTempTab("Change Inv Order", new ChangeInventoryOrder(new ChangeOrderVM(this)));
        }

        private void ChangeToMenu(object obj)
        {
            ChangePageState(PageState.Menu);
        }

        private void ChangeToBatch(object obj)
        {
            ChangePageState(PageState.Batch);
        }

        private void ChangeToInventory(object obj)
        {
            ChangePageState(PageState.Inventory);
        }

        private void AddBatchItem(object obj)
        {
            throw new NotImplementedException();
        }

        private void DeleteBatchItem(object obj)
        {
            throw new NotImplementedException();
        }

        private void EditBatchItem(object obj)
        {
            throw new NotImplementedException();
        }

        private void AddMenuItem(object obj)
        {
            throw new NotImplementedException();
        }

        private void DeleteMenuItem(object obj)
        {
            throw new NotImplementedException();
        }

        private void EditMenuItem(object obj)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Initializers
        public bool LoadDisplayItems()
        {
            if (_models.InventoryItems != null)
            {
                FilteredInventoryItems = new ObservableCollection<IItem>(ParentContext.SortItems(_models.InventoryItems));

                _databaseFound = true;
                NotifyPropertyChanged("FilteredInventoryItems");

                return true;
            }

            return false;
        }

        /// <summary>
        /// Display on the datagrids that the inventory items could not be found
        /// </summary>
        private void DisplayItemsNotFound()
        {
            FilteredInventoryItems = new ObservableCollection<IItem>() { new InventoryItem() { Name = "Database not found" } };
            _databaseFound = false;
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
            return true;
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
        /// Called when Master List is edited
        /// </summary>
        public void InventoryItemCountChanged()
        {
            ChangeCountCanExecute = true;
        }

        private void FlashSavedMessage()
        {
            new Thread(delegate ()
            {
                SavedVisibility = Visibility.Visible;
                Thread.Sleep(3000);
                SavedVisibility = Visibility.Hidden;
            }).Start();
        }

        #endregion

        /// <summary>
        /// Filter list of inventory items based on the string in the filter box above datagrids
        /// </summary>
        /// <param name="filterStr"></param>
        public void FilterInventoryItems(string filterStr)
        {
            FilteredInventoryItems = ParentContext.FilterInventoryItems(filterStr, GetTotalDisplayList());
            NotifyPropertyChanged("FilteredInventoryItems");
        }

        private void RefreshInventoryList()
        {
            FilteredInventoryItems = new ObservableCollection<IItem>(ParentContext.SortItems(_models.InventoryItems));
        }

        private void ChangePageState(PageState state)
        {
            _state = state;
            switch (state)
            {
                case PageState.Inventory:
                    Header = "Inventory";
                    AddInventoryItemCommand = new RelayCommand(AddInventoryItem, x => AddItemCanExecute);
                    DeleteInventoryItemCommand = new RelayCommand(DeleteInventoryItem, x => DeleteEditCanExecute);
                    EditInventoryItemCommand = new RelayCommand(EditInventoryItem, x => DeleteEditCanExecute);
                    RefreshInventoryList();
                    break;
                case PageState.Batch:
                    Header = "Batch Items";
                    AddInventoryItemCommand = new RelayCommand(AddBatchItem, x => AddItemCanExecute);
                    DeleteInventoryItemCommand = new RelayCommand(DeleteBatchItem, x => DeleteEditCanExecute);
                    EditInventoryItemCommand = new RelayCommand(EditBatchItem, x => DeleteEditCanExecute);
                    break;
                case PageState.Menu:
                    Header = "Menu Items";
                    AddInventoryItemCommand = new RelayCommand(AddMenuItem, x => AddItemCanExecute);
                    DeleteInventoryItemCommand = new RelayCommand(DeleteMenuItem, x => DeleteEditCanExecute);
                    EditInventoryItemCommand = new RelayCommand(EditMenuItem, x => DeleteEditCanExecute);
                    break;
            }
        }

        private IEnumerable<IItem> GetTotalDisplayList()
        {
            switch(_state)
            {
                case PageState.Inventory:
                    return _models.InventoryItems;
                case PageState.Batch:
                    return _models.Recipes.Where(x => x.IsBatch);
                case PageState.Menu:
                    return _models.Recipes.Where(x => !x.IsBatch);
                default:
                    return null;
            }
        }
    }
}
