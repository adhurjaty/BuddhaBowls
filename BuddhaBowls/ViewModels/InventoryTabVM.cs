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
        private ModelContainer _models;
        private bool _databaseFound;

        // INotifyPropertyChanged event and method
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainViewModel ParentContext { get; set; }

        #region Content Binders
        // Inventory item selected in the datagrids for Orders and Master List
        private Inventory _selectedInventory;
        public Inventory SelectedInventory
        {
            get
            {
                return _selectedInventory;
            }
            set
            {
                _selectedInventory = value;
                NotifyPropertyChanged("SelectedInventory");
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

        private ObservableCollection<Inventory> _inventoryList;
        public ObservableCollection<Inventory> InventoryList
        {
            get
            {
                return _inventoryList;
            }
            set
            {
                _inventoryList = value;
                NotifyPropertyChanged("InventoryList");
            }
        }

        private List<Inventory> _selectedMultiInventories;
        public List<Inventory> SelectedMultiInventories
        {
            get
            {
                return _selectedMultiInventories;
            }
            set
            {
                _selectedMultiInventories = value;
                CompareCanExecute = value.Count == 2;
            }
        }

        #endregion

        #region ICommand Bindings and Can Execute
        // Plus button in Master invententory list form
        public ICommand AddInventoryCommand { get; set; }
        // Minus button in Master invententory list form
        public ICommand DeleteInventoryCommand { get; set; }
        // View button in Master invententory list form
        public ICommand ViewInventoryCommand { get; set; }
        // Compare button
        public ICommand CompareCommand { get; set; }

        public bool DeleteEditCanExecute
        {
            get
            {
                return SelectedInventory != null && _databaseFound;
            }
        }

        public bool AddInvCanExecute
        {
            get
            {
                return true; // _databaseFound;
            }
        }

        public bool ChangeCountCanExecute { get; set; } = true;

        public bool CompareCanExecute { get; set; } = false;

        #endregion

        public InventoryTabVM(ModelContainer models, MainViewModel parent)
        {
            ParentContext = parent;
            _models = models;

            AddInventoryCommand = new RelayCommand(StartNewInventory, x => AddInvCanExecute);
            DeleteInventoryCommand = new RelayCommand(DeleteInventoryItem, x => DeleteEditCanExecute);
            ViewInventoryCommand = new RelayCommand(ViewInventory, x => DeleteEditCanExecute);
            CompareCommand = new RelayCommand(CompareInvetories, x => CompareCanExecute);

            TryDBConnect();
        }

        #region ICommand Helpers
        /// <summary>
        /// Creates new tab called Add Inventory Item and populates form - Plus button
        /// </summary>
        /// <param name="obj"></param>
        private void StartNewInventory(object obj)
        {
            ParentContext.AddTempTab("New Inventory", new NewInventory(new NewInventoryVM(_models, ParentContext)));
        }

        /// <summary>
        /// Creates a new tab called Edit Inventory Item and populates form - Edit button
        /// </summary>
        /// <param name="obj"></param>
        private void ViewInventory(object obj)
        {
            ParentContext.AddTempTab("View Inventory", new NewInventory(new NewInventoryVM(_models, ParentContext, SelectedInventory)));
        }

        /// <summary>
        /// Presents user with warning dialog, then removes item from DB and in-memory list - Minus button
        /// </summary>
        /// <param name="obj"></param>
        private void DeleteInventoryItem(object obj)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete this record?", "Delete record?", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                _models.Inventories.Remove(SelectedInventory);
                SelectedInventory.Destroy();
                SelectedInventory = null;
                RefreshInventoryList();
            }
        }

        private void CompareInvetories(object obj)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Initializers
        public bool LoadDisplayItems()
        {
            if (_models.Inventories != null)
            {
                RefreshInventoryList();
                _databaseFound = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Display on the datagrids that the inventory items could not be found
        /// </summary>
        private void DisplayItemsNotFound()
        {
            // figure out way to display missing data
            InventoryList = new ObservableCollection<Inventory>() { new Inventory() };
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

        /// <summary>
        /// Called when Master List is edited
        /// </summary>
        public void InventoryItemCountChanged()
        {
            ChangeCountCanExecute = true;
        }

        public void RefreshInventoryList()
        {
            InventoryList = new ObservableCollection<Inventory>(_models.Inventories.OrderByDescending(x => x.Date));
        }

        #endregion
    }
}
