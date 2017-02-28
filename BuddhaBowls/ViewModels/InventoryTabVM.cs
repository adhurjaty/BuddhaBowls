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
using System.Windows.Controls;
using System.Windows.Input;

namespace BuddhaBowls
{
    /// <summary>
    /// Permanent tab showing the current inventory and the inventory history
    /// </summary>
    public class InventoryTabVM : ChangeableTabVM
    {
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
        public ICommand InvListCommand { get; set; }
        public ICommand AddInvCommand { get; set; }

        public bool DeleteEditCanExecute
        {
            get
            {
                return SelectedInventory != null && DBConnection;
            }
        }

        public bool ChangeCountCanExecute { get; set; } = true;

        public bool CompareCanExecute { get; set; } = false;

        #endregion

        public InventoryTabVM() : base()
        {
            Header = "Inventory";
            PrimaryPageName = "Items";
            SecondaryPageName = "History";

            AddInventoryCommand = new RelayCommand(StartNewInventory, x => DBConnection);
            DeleteInventoryCommand = new RelayCommand(DeleteInventoryItem, x => DeleteEditCanExecute && DBConnection);
            ViewInventoryCommand = new RelayCommand(ViewInventory, x => DeleteEditCanExecute && DBConnection);
            CompareCommand = new RelayCommand(CompareInvetories, x => CompareCanExecute && DBConnection);
            InvListCommand = new RelayCommand(GenerateInvList, x => DBConnection);
            AddInvCommand = new RelayCommand(NewInvItem);

            if(DBConnection)
            {
                RefreshInventoryList();
            }
            else
            {
                DisplayItemsNotFound();
            }
        }

        #region ICommand Helpers
        /// <summary>
        /// Creates new tab called Add Inventory Item and populates form - Plus button
        /// </summary>
        /// <param name="obj"></param>
        private void StartNewInventory(object obj)
        {
            NewInventoryVM tabVM = new NewInventoryVM();
            tabVM.Add("New Inventory");
        }

        /// <summary>
        /// Creates a new tab called Edit Inventory Item and populates form - Edit button
        /// </summary>
        /// <param name="obj"></param>
        private void ViewInventory(object obj)
        {
            NewInventoryVM tabVM = new NewInventoryVM(SelectedInventory);
            tabVM.Add("View Inventory");
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

        /// <summary>
        /// Triggered by the Compare Inv button - opens a new temp tab for comparison
        /// </summary>
        /// <param name="obj"></param>
        private void CompareInvetories(object obj)
        {
            Inventory[] invs = SelectedMultiInventories.OrderBy(x => x.Date).ToArray();
            CompareInvVM tabVM = new CompareInvVM(invs[0], invs[1]);
            tabVM.Add("Compare Invs");
        }

        /// <summary>
        /// Triggered by pressing the Excel Inventory List button
        /// </summary>
        /// <param name="obj"></param>
        private void GenerateInvList(object obj)
        {
            ReportGenerator generator = new ReportGenerator(_models);

            if (ParentContext.ExcelThread == null || !ParentContext.ExcelThread.IsAlive)
            {
                try
                {
                    ParentContext.ExcelThread = new Thread(delegate ()
                    {
                        string xlsPath = generator.GenerateMasterInventoryTable("Master Inventory List.xlsx");
                        generator.Close();
                        if (File.Exists(xlsPath))
                            System.Diagnostics.Process.Start(xlsPath);
                    });
                    ParentContext.ExcelThread.Start();
                }
                catch(Exception e)
                {
                    ErrorHandler.ReportError("InventoryTabVM", "GenerateInvList", e);
                }
            }
            else
            {
                MessageBox.Show("Excel process currently running. If you don't know what this means, hit OK and restart the application", "Excel Warning", MessageBoxButton.OK);
            }
        }

        private void NewInvItem(object obj)
        {
            NewInventoryItemWizard wizard = new NewInventoryItemWizard();
            wizard.Add("New Item");
        }

        #endregion

        #region Initializers
        public bool LoadDisplayItems()
        {
            if (_models.Inventories != null)
            {
                RefreshInventoryList();
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

        /// <summary>
        /// Filter list of inventory items based on the string in the filter box above datagrids
        /// </summary>
        /// <param name="filterStr"></param>
        public override void FilterItems(string filterStr)
        {
            if(_pageState != PageState.Primary)
            {
                throw new Exception("How did you get here?");
            }

            if (string.IsNullOrWhiteSpace(filterStr))
                ((InventoryListControl)TabControl).ShowArrowColumn();
            else
                ((InventoryListControl)TabControl).HideArrowColumn();
            FilteredInventoryItems = ParentContext.FilterInventoryItems(filterStr, _models.InventoryItems.Select(x => (IItem)x));
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

        protected override void ChangePageState(PageState state)
        {
            base.ChangePageState(state);

            switch(state)
            {
                case PageState.Primary:
                    TabControl = new InventoryListControl(this);
                    break;
                case PageState.Secondary:
                    TabControl = new InventoryHistoryControl(this);
                    break;
                default:
                    break;
            }
        }

        private void SaveInvOrder()
        {
            Properties.Settings.Default.InventoryOrder = FilteredInventoryItems.Select(x => x.Name).ToList();
            Properties.Settings.Default.Save();

            string dir = Path.Combine(Properties.Settings.Default.DBLocation, "Settings");
            Directory.CreateDirectory(dir);
            File.WriteAllLines(Path.Combine(dir, GlobalVar.INV_ORDER_FILE), Properties.Settings.Default.InventoryOrder);
        }

    }
}
