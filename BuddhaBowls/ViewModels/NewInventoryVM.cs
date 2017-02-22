using BuddhaBowls.Helpers;
using BuddhaBowls.Models;
using BuddhaBowls.Services;
using BuddhaBowls.UserControls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace BuddhaBowls
{
    public class NewInventoryVM : INotifyPropertyChanged, ITabVM
    {
        private ModelContainer _models;
        private List<InventoryItem> _inventoryItems;
        private Inventory _inventory;
        private bool _databaseFound;
        NewInventory _control;

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

        private DateTime? _inventoryDate;
        public DateTime InventoryDate
        {
            get
            {
                return _inventoryDate ?? DateTime.Now;
            }
            set
            {
                _inventoryDate = value;
                NotifyPropertyChanged("InventoryDate");
            }
        }

        private float _inventoryValue;
        public float InventoryValue
        {
            get
            {
                return _inventoryValue;
            }
            set
            {
                _inventoryValue = value;
                NotifyPropertyChanged("InventoryValue");
            }
        }

        private string _totalValueMessage;
        public string TotalValueMessage
        {
            get
            {
                return _totalValueMessage;
            }
            set
            {
                _totalValueMessage = value;
                NotifyPropertyChanged("TotalValueMessage");
            }
        }

        private ObservableCollection<PriceExpanderItem> _categoryPrices;
        public ObservableCollection<PriceExpanderItem> CategoryPrices
        {
            get
            {
                return _categoryPrices;
            }
            set
            {
                _categoryPrices = value;
                NotifyPropertyChanged("CategoryPrices");
            }
        }
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
        //public ICommand InventorySectionCommand { get; set; }
        //// Batch Items section button
        //public ICommand BatchSectionCommand { get; set; }
        //// Menu Item section button
        //public ICommand MenuSectionCommand { get; set; }
        // Cancel button to close tab
        public ICommand CancelCommand { get; set; }
        public ICommand ResetOrderCommand { get; set; }

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

        #endregion

        public NewInventoryVM(ModelContainer models, MainViewModel parent)
        {
            ParentContext = parent;
            _models = models;
            _inventoryItems = models.InventoryItems;

            AddInventoryItemCommand = new RelayCommand(AddInventoryItem, x => AddItemCanExecute);
            DeleteInventoryItemCommand = new RelayCommand(DeleteInventoryItem, x => DeleteEditCanExecute);
            EditInventoryItemCommand = new RelayCommand(EditInventoryItem, x => DeleteEditCanExecute);
            SaveAddEditCommand = new RelayCommand(SaveAddEdit, x => SaveAddEditCanExecute);
            CancelAddEditCommand = new RelayCommand(CancelAddEdit);
            SaveCountCommand = new RelayCommand(SaveNewInventory, x => ChangeCountCanExecute);
            ResetCountCommand = new RelayCommand(ResetCount, x => ChangeCountCanExecute);
            ChangeOrderCommand = new RelayCommand(StartChangeOrder);
            CancelCommand = new RelayCommand(CancelInventory);
            ResetOrderCommand = new RelayCommand(ResetOrder);

            TryDBConnect();
        }

        public NewInventoryVM(ModelContainer models, MainViewModel parent, Inventory inv) : this(models, parent)
        {
            _inventory = inv;
            _inventoryItems = inv.GetInventoryHistory();
            InventoryDate = inv.Date;

            ((RelayCommand)SaveCountCommand).ChangeCallback(SaveOldInventory);
            TryDBConnect();
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
                _inventoryItems.Remove((InventoryItem)SelectedInventoryItem);
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

            AddEditErrorMessage = ParentContext.ObjectFromFields(ref item, FieldsCollection, AddEditHeader.StartsWith("Add"));
            if (string.IsNullOrEmpty(AddEditErrorMessage))
            {

                if (AddEditHeader.StartsWith("Edit"))
                {
                    item.Update();
                }
                else if (AddEditHeader.StartsWith("Add"))
                {
                    _inventoryItems.Add(item);
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
        private void SaveNewInventory(object obj)
        {
            foreach (InventoryItem item in FilteredInventoryItems)
            {
                item.Update();
            }
            
            Inventory inv = new Inventory(InventoryDate);
            inv.Id = inv.Insert(_inventoryItems);

            if(_models.Inventories == null)
                _models.Inventories = new List<Inventory>();

            if (_models.Inventories.Count > 0 && _models.Inventories.Select(x => x.Date.Date).Contains(InventoryDate.Date))
            {
                int idx = _models.Inventories.FindIndex(x => x.Date.Date == InventoryDate.Date);
                Inventory oldInv = _models.Inventories[idx];
                inv.Id = oldInv.Id;
                _models.Inventories[idx] = inv;
            }
            else
            {
                _models.Inventories.Add(inv);
            }

            ParentContext.InventoryTab.RefreshInventoryList();
            ParentContext.DeleteTempTab();
        }

        private void SaveOldInventory(object obj)
        {
            _inventory.Update(_inventoryItems);

            ParentContext.DeleteTempTab();
        }

        private void StartChangeOrder(object obj)
        {
            RefreshInventoryList();
            // figure out if I want to add multiple temp tabs or replace the temp tab
            ParentContext.AddTempTab("Change Inv Order", new ChangeInventoryOrder(new ChangeOrderVM(this)));
        }

        private void CancelInventory(object obj)
        {
            ParentContext.DeleteTempTab();
        }

        private void ResetOrder(object obj)
        {
            FilterText = "";
            FilterInventoryItems("");
        }

        #endregion

        #region Initializers
        public bool LoadDisplayItems()
        {
            if (_inventoryItems != null)
            {
                FilteredInventoryItems = new ObservableCollection<IItem>(ParentContext.SortItems(_inventoryItems));

                _databaseFound = true;
                NotifyPropertyChanged("FilteredInventoryItems");

                return true;
            }

            return false;
        }

        public void InitializeControl(NewInventory control)
        {
            _control = control;
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
            UpdateInvValue();
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
            UpdateInvValue();
        }

        private void UpdateInvValue()
        {
            //InventoryValue = FilteredInventoryItems.Sum(x => ((InventoryItem)x).LastPurchasedPrice * x.Count);
            List<PriceExpanderItem> items = new List<PriceExpanderItem>();
            TotalValueMessage = "Inventory Value: " + FilteredInventoryItems.Sum(x => ((InventoryItem)x).LastPurchasedPrice * x.Count).ToString("c");
            foreach(string category in _models.ItemCategories)
            {
                float value = _models.InventoryItems.Where(x => x.Category.ToUpper() == category.ToUpper()).Sum(x => x.LastPurchasedPrice * x.Count);
                items.Add(new PriceExpanderItem() { Label = category + " VALUE:", Price = value });
            }

            CategoryPrices = new ObservableCollection<PriceExpanderItem>(items);
        }

        public void MoveDown(IItem item)
        {
            MoveInList(item, false);
        }

        public void MoveUp(IItem item)
        {
            MoveInList(item, true);
        }

        private void MoveInList(IItem item, bool up)
        {
            List<IItem> orderedList = FilteredInventoryItems.ToList();
            int idx = orderedList.IndexOf(item);
            orderedList.RemoveAt(idx);

            if (idx > 0 && up)
                orderedList.Insert(idx - 1, item);
            if (idx < orderedList.Count - 1 && !up)
                orderedList.Insert(idx + 1, item);

            FilteredInventoryItems = new ObservableCollection<IItem>(orderedList);
            SaveInvOrder();
        }

        #endregion

        /// <summary>
        /// Filter list of inventory items based on the string in the filter box above datagrids
        /// </summary>
        /// <param name="filterStr"></param>
        public void FilterInventoryItems(string filterStr)
        {
            if (string.IsNullOrWhiteSpace(filterStr))
                _control.ShowArrowColumn();
            else
                _control.HideArrowColumn();
            FilteredInventoryItems = ParentContext.FilterInventoryItems(filterStr, _inventoryItems.Select(x => (IItem)x));
            NotifyPropertyChanged("FilteredInventoryItems");
        }

        private void RefreshInventoryList()
        {
            FilteredInventoryItems = new ObservableCollection<IItem>(ParentContext.SortItems(_inventoryItems));
        }

        private void SaveInvOrder()
        {
            Properties.Settings.Default.InventoryOrder = FilteredInventoryItems.Select(x => x.Name).ToList();
            Properties.Settings.Default.Save();

            string dir = Path.Combine(Properties.Settings.Default.DBLocation, "Settings");
            Directory.CreateDirectory(dir);
            File.WriteAllLines(Path.Combine(dir, GlobalVar.INV_ORDER_FILE), Properties.Settings.Default.InventoryOrder);
        }

        //private IEnumerable<IItem> GetTotalDisplayList()
        //{
        //    switch(_state)
        //    {
        //        case PageState.Inventory:
        //            return _models.InventoryItems;
        //        case PageState.Batch:
        //            return _models.Recipes.Where(x => x.IsBatch);
        //        case PageState.Menu:
        //            return _models.Recipes.Where(x => !x.IsBatch);
        //        default:
        //            return null;
        //    }
        //}
    }

    public class PriceExpanderItem
    {
        public string Label { get; set; }
        public float Price { get; set; }
    }
}
