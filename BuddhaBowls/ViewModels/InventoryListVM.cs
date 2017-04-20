using BuddhaBowls.Helpers;
using BuddhaBowls.Models;
using BuddhaBowls.UserControls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace BuddhaBowls
{
    public delegate void StatusUpdatedDel();

    public class InventoryListVM : TabVM
    {
        private StatusUpdatedDel CountChanged;
        private List<VendorInventoryItem> _inventoryItems;
        private Inventory _inventory;

        public InventoryListControl TabControl { get; set; }

        private bool _isMasterList;
        public bool IsMasterList
        {
            get
            {
                return _isMasterList;
            }
            set
            {
                _isMasterList = value;
                if (value)
                    MasterVisibility = Visibility.Visible;
                else
                    NewInvVisibility = Visibility.Visible;
            }
        }

        #region Content Binders

        private ObservableCollection<VendorInventoryItem> _filteredItems;
        public ObservableCollection<VendorInventoryItem> FilteredItems
        {
            get
            {
                return _filteredItems;
            }
            set
            {
                _filteredItems = value;
                NotifyPropertyChanged("FilteredItems");
            }
        }

        // Inventory item selected in the datagrids for Orders and Master List
        private VendorInventoryItem _selectedInventoryItem;
        public VendorInventoryItem SelectedInventoryItem
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

        private Visibility _editOrderVisibility = Visibility.Visible;
        public Visibility EditOrderVisibility
        {
            get
            {
                return _editOrderVisibility;
            }
            set
            {
                _editOrderVisibility = value;
                if (value == Visibility.Visible)
                {
                    _saveOrderVisibility = Visibility.Hidden;
                    ArrowVisibility = Visibility.Hidden;
                }
                else
                {
                    _saveOrderVisibility = Visibility.Visible;
                    ArrowVisibility = Visibility.Visible;
                }

                NotifyPropertyChanged("EditOrderVisibility");
                NotifyPropertyChanged("SaveOrderVisibility");
            }
        }

        private Visibility _saveOrderVisibility = Visibility.Hidden;
        public Visibility SaveOrderVisibility
        {
            get
            {
                return _saveOrderVisibility;
            }
            set
            {
                _saveOrderVisibility = value;
                if (value == Visibility.Visible)
                {
                    _editOrderVisibility = Visibility.Hidden;
                    ArrowVisibility = Visibility.Visible;
                }
                else
                {
                    _editOrderVisibility = Visibility.Visible;
                    ArrowVisibility = Visibility.Hidden;
                }

                NotifyPropertyChanged("EditOrderVisibility");
                NotifyPropertyChanged("SaveOrderVisibility");
            }
        }

        private Visibility _masterVisibility = Visibility.Hidden;
        public Visibility MasterVisibility
        {
            get
            {
                return _masterVisibility;
            }
            set
            {
                _masterVisibility = value;
                if (value == Visibility.Visible)
                {
                    _newInvVisibility = Visibility.Hidden;
                }
                else
                {
                    _newInvVisibility = Visibility.Visible;
                    ArrowVisibility = Visibility.Hidden;
                }

                NotifyPropertyChanged("NewInvVisibility");
                NotifyPropertyChanged("MasterVisibility");
                NotifyPropertyChanged("EditOrderVisibility");
                NotifyPropertyChanged("SaveOrderVisibility");
            }
        }

        private Visibility _newInvVisibility = Visibility.Hidden;
        public Visibility NewInvVisibility
        {
            get
            {
                return _newInvVisibility;
            }
            set
            {
                _newInvVisibility = value;
                if (value == Visibility.Visible)
                {
                    _masterVisibility = Visibility.Hidden;
                    _saveOrderVisibility = Visibility.Hidden;
                    _editOrderVisibility = Visibility.Hidden;
                    ArrowVisibility = Visibility.Hidden;
                }
                else
                    _masterVisibility = Visibility.Visible;

                NotifyPropertyChanged("MasterVisibility");
                NotifyPropertyChanged("NewInvVisibility");
                NotifyPropertyChanged("EditOrderVisibility");
                NotifyPropertyChanged("SaveOrderVisibility");
            }
        }

        private Visibility _arrowVisibility = Visibility.Hidden;
        public Visibility ArrowVisibility
        {
            get
            {
                return _arrowVisibility;
            }
            set
            {
                if (_masterVisibility == Visibility.Visible)
                {
                    _arrowVisibility = value;
                    NotifyPropertyChanged("ArrowVisibility");
                }
            }
        }

        //public Visibility EditVendorVisibility
        //{
        //    get
        //    {
        //        return _inventory == null ? Visibility.Visible : Visibility.Hidden;
        //    }
        //}

        //public Visibility ReadOnlyVendorVisibility
        //{
        //    get
        //    {
        //        return _inventory != null ? Visibility.Visible : Visibility.Hidden;
        //    }
        //}
        #endregion

        #region ICommand and CanExecute

        public ICommand AddCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand EditCommand { get; set; }
        public ICommand ResetCommand { get; set; }
        public ICommand EditOrderCommand { get; set; }
        public ICommand SaveOrderCommand { get; set; }

        #endregion

        /// <summary>
        /// Constructor for Master list
        /// </summary>
        public InventoryListVM() : base()
        {
            IsMasterList = true;

            Refresh();
            UpdateInvValue();

            SetCommandsAndControl();
        }

        /// <summary>
        /// Constructor for a new inventory
        /// </summary>
        /// <param name="countDel"></param>
        public InventoryListVM(StatusUpdatedDel countDel) : base()
        {
            CountChanged = countDel;
            IsMasterList = false;

            Refresh();
            UpdateInvValue();
            SetCommandsAndControl();
        }

        /// <summary>
        /// Constructor for edit inventory
        /// </summary>
        /// <param name="countDel"></param>
        /// <param name="inv"></param>
        public InventoryListVM(Inventory inv, StatusUpdatedDel countDel) : base()
        {
            _inventory = inv;
            CountChanged = countDel;
            IsMasterList = false;

            Refresh();
            UpdateInvValue();
            SetCommandsAndControl();
        }

        #region ICommand Helpers

        private void AddInventoryItem(object obj)
        {
            NewInventoryItemWizard wizard = new NewInventoryItemWizard(AddNewItem);
            wizard.Add("New Item");
        }

        private void DeleteInventoryItem(object obj)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete " + SelectedInventoryItem.Name,
                                                      "Delete " + SelectedInventoryItem.Name + "?", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                InventoryItem item = SelectedInventoryItem.ToInventoryItem();
                foreach (Vendor v in SelectedInventoryItem.Vendors)
                {
                    v.RemoveInvItem(item);
                }
                _models.DeleteInventoryItem(item);
                ParentContext.RemoveInvItem(item);
                SelectedInventoryItem = null;
            }
        }

        private void EditInventoryItem(object obj)
        {
            NewInventoryItemWizard wizard = new NewInventoryItemWizard(AddNewItem, SelectedInventoryItem.ToInventoryItem());
            wizard.Add("New Item");
        }

        /// <summary>
        /// Return the displayed inventory list to original order (no filter text)
        /// </summary>
        /// <param name="obj"></param>
        private void ResetList(object obj)
        {
            FilterText = "";
            FilterItems("");
        }

        private void StartEditOrder(object obj)
        {
            SaveOrderVisibility = Visibility.Visible;
        }

        private void SaveOrder(object obj)
        {
            EditOrderVisibility = Visibility.Visible;
        }

        #endregion

        #region Initializers

        private void SetCommandsAndControl()
        {
            TabControl = new InventoryListControl(this);

            AddCommand = new RelayCommand(AddInventoryItem);
            DeleteCommand = new RelayCommand(DeleteInventoryItem, x => SelectedInventoryItem != null);
            EditCommand = new RelayCommand(EditInventoryItem, x => SelectedInventoryItem != null);
            ResetCommand = new RelayCommand(ResetList);
            EditOrderCommand = new RelayCommand(StartEditOrder, x => string.IsNullOrEmpty(FilterText));
            SaveOrderCommand = new RelayCommand(SaveOrder);
        }

        #endregion

        #region Update UI Methods

        /// <summary>
        /// Filter list of inventory items based on the string in the filter box above datagrids
        /// </summary>
        /// <param name="filterStr"></param>
        public override void FilterItems(string filterStr)
        {
            if (SaveOrderVisibility == Visibility.Visible)
                SaveOrderVisibility = Visibility.Hidden;
            FilteredItems = MainHelper.FilterInventoryItems(filterStr, _inventoryItems);
        }

        public override void Refresh()
        {
            if (_inventory == null)
            {
                _inventoryItems = MainHelper.SortItems(_models.InventoryItems.Select(x =>
                                    new VendorInventoryItem(_models.GetVendorsFromItem(x), x))).ToList();
            }
            else
            {
                _inventoryItems = MainHelper.SortItems(_inventory.GetInventoryHistory().Select(x =>
                                    new VendorInventoryItem(_models.GetVendorsFromItem(x), x)).ToList()).ToList();
            }
            FilteredItems = new ObservableCollection<VendorInventoryItem>(_inventoryItems);
            FilterText = "";
        }

        /// <summary>
        /// Adds an item across the application
        /// </summary>
        /// <param name="item"></param>
        private void AddNewItem(InventoryItem item)
        {
            ParentContext.AddInvItem(item);
        }

        /// <summary>
        /// Adds an item to this list
        /// </summary>
        /// <param name="item"></param>
        public void AddItem(InventoryItem item)
        {
            VendorInventoryItem vendorItem = new VendorInventoryItem(_models.GetVendorsFromItem(item), item);
            int idx = Properties.Settings.Default.InventoryOrder.FindIndex(x => x == item.Name);
            if (idx != -1)
            {
                FilteredItems.Insert(idx, vendorItem);
                _inventoryItems.Insert(idx, vendorItem);
            }
            else
            {
                FilteredItems.Add(vendorItem);
                _inventoryItems.Add(vendorItem);
            }
        }

        /// <summary>
        /// Removes an item from this list
        /// </summary>
        /// <param name="item"></param>
        public void RemoveItem(InventoryItem item)
        {
            VendorInventoryItem vendorItem = _inventoryItems.FirstOrDefault(x => x.Id == item.Id);
            FilteredItems.Remove(vendorItem);
            _inventoryItems.Remove(vendorItem);
        }

        public void MoveDown(InventoryItem item)
        {
            MoveInList(item, false);
        }

        public void MoveUp(InventoryItem item)
        {
            MoveInList(item, true);
        }

        private void MoveInList(InventoryItem item, bool up)
        {
            List<InventoryItem> newItemInList = MainHelper.MoveInList(item, up, FilteredItems.Select(x => (InventoryItem)x).ToList());

            FilteredItems = new ObservableCollection<VendorInventoryItem>(newItemInList.Select(x => (VendorInventoryItem)x));
            SaveInvOrder();
        }

        private void SaveInvOrder()
        {
            Properties.Settings.Default.InventoryOrder = FilteredItems.Select(x => x.Name).ToList();
            Properties.Settings.Default.Save();

            string dir = Path.Combine(Properties.Settings.Default.DBLocation, "Settings");
            Directory.CreateDirectory(dir);
            File.WriteAllLines(Path.Combine(dir, GlobalVar.INV_ORDER_FILE), Properties.Settings.Default.InventoryOrder);
        }

        public void RowEdited(VendorInventoryItem item)
        {
            item.UpdateVendorProps();
            if (!IsMasterList)
            {
                InventoryItemCountChanged();
                item.UpdateProperties();
            }
        }

        /// <summary>
        /// Called when Master List is edited
        /// </summary>
        public void InventoryItemCountChanged()
        {
            CountChanged();
            UpdateInvValue();
        }

        /// <summary>
        /// Resets the inventory count to the saved value before changing the datagrid
        /// </summary>
        /// <param name="obj"></param>
        public void ResetCount()
        {
            FilterText = "";
            foreach (InventoryItem item in FilteredItems)
            {
                item.Count = item.GetLastCount();
            }
            Refresh();
        }

        private void UpdateInvValue()
        {
            List<PriceExpanderItem> items = new List<PriceExpanderItem>();
            TotalValueMessage = "Inventory Value: " + FilteredItems.Sum(x => x.PriceExtension).ToString("c");
            foreach (string category in _models.ItemCategories)
            {
                float value = _inventoryItems.Where(x => x.Category.ToUpper() == category.ToUpper()).Sum(x => x.PriceExtension);
                items.Add(new PriceExpanderItem() { Label = category + " Value:", Price = value });
            }

            CategoryPrices = new ObservableCollection<PriceExpanderItem>(items);
        }
        #endregion

        /// <summary>
        /// Called from New Inventory: saves the filtered items
        /// </summary>
        public void SaveNew(DateTime invDate)
        {
            ResetList(null);
            foreach (VendorInventoryItem item in FilteredItems)
            {
                InventoryItem invItem = item.ToInventoryItem();
                _models.AddUpdateInventoryItem(ref invItem);
            }

            Inventory inv = new Inventory(invDate);
            inv.Id = inv.Insert(_inventoryItems.Select(x => x.ToInventoryItem()).ToList());

            if (_models.Inventories == null)
                _models.Inventories = new List<Inventory>();

            if (_models.Inventories.Count > 0 && _models.Inventories.Select(x => x.Date.Date).Contains(invDate.Date))
            {
                int idx = _models.Inventories.FindIndex(x => x.Date.Date == invDate.Date);
                Inventory oldInv = _models.Inventories[idx];
                inv.Id = oldInv.Id;
                _models.Inventories[idx] = inv;
            }
            else
            {
                _models.Inventories.Add(inv);
            }
        }

        public void SaveOld()
        {
            _inventory.Update(_inventoryItems.Select(x => x.ToInventoryItem()).ToList());
        }

    }

    public class PriceExpanderItem
    {
        public string Label { get; set; }
        public float Price { get; set; }
    }
}
