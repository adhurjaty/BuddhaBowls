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

        private void NewInventoryHelper(object obj)
        {
            NewInventoryItemWizard wizard = new NewInventoryItemWizard();
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
                FilteredItems.Remove(SelectedInventoryItem);
                ParentContext.InventoryTab.RemoveInvItem(item);
                SelectedInventoryItem = null;
            }
        }

        private void EditInventoryItem(object obj)
        {
            NewInventoryItemWizard wizard = new NewInventoryItemWizard(SelectedInventoryItem.ToInventoryItem());
            wizard.Add("New Item");
        }

        /// <summary>
        /// Return the displayed inventory list to original order (no filter text)
        /// </summary>
        /// <param name="obj"></param>
        private void ResetList(object obj)
        {
            FilterItems("");
            Refresh();
        }

        private void StartEditOrder(object obj)
        {
            SaveOrderVisibility = Visibility.Visible;
        }

        private void SaveOrder(object obj)
        {
            EditOrderVisibility = Visibility.Visible;
            SaveInvOrder();
        }

        #endregion

        #region Initializers

        private void SetCommandsAndControl()
        {
            TabControl = new InventoryListControl(this);

            AddCommand = new RelayCommand(NewInventoryHelper);
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
            if (IsMasterList)
                _inventoryItems = _models.VendorInvItems;
            else
            {
                if (_inventory == null)
                {
                    _inventoryItems = _models.VendorInvItems.Select(x => x.Copy()).ToList();
                }
                else
                {
                    _inventoryItems = MainHelper.SortItems(_inventory.GetInventoryHistory().Select(x =>
                                    new VendorInventoryItem(x, _models.Vendors.FirstOrDefault(y => y.Id == x.LastVendorId)))).ToList();
                }
            }
            UpdateInvValue();
            FilteredItems = new ObservableCollection<VendorInventoryItem>(_inventoryItems);
            FilterText = "";
        }

        /// <summary>
        /// Updates inventory items list on notification that a new item has been added
        /// </summary>
        public void AddedItem()
        {
            FilterText = "";

            // find the new vendor inventory item
            List<int> newIds = _models.VendorInvItems.Select(x => x.Id).Except(_inventoryItems.Select(x => x.Id)).ToList();
            if (newIds.Count == 0)
                return;
            VendorInventoryItem newItem = _models.VendorInvItems.First(x => x.Id == newIds[0]);

            // add and sort to list
            _inventoryItems.Add(newItem);
            _inventoryItems = MainHelper.SortItems(_inventoryItems).ToList();
            FilteredItems = new ObservableCollection<VendorInventoryItem>(_inventoryItems);
        }

        /// <summary>
        /// Removes an item from this list
        /// </summary>
        /// <param name="item"></param>
        public void RemoveItem(InventoryItem item)
        {
            VendorInventoryItem vendorItem = _inventoryItems.FirstOrDefault(x => x.Id == item.Id);
            _inventoryItems.Remove(vendorItem);
            FilteredItems.Remove(vendorItem);
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
        }

        private void SaveInvOrder()
        {
            Properties.Settings.Default.InventoryOrder = FilteredItems.Select(x => x.Name).ToList();
            Properties.Settings.Default.Save();

            _models.SaveInvOrder();
            _models.ReOrderInvList();
        }

        public void RowEdited(VendorInventoryItem item)
        {
            if (IsMasterList)
            {
                item.Update();
            }
            else
            {
                InventoryItemCountChanged();
                item.NotifyAllChanges();
            }
        }

        /// <summary>
        /// Called when New/Edit Inventory List is edited
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

        /// <summary>
        /// Upates prices in the category price breakdown dropdown list
        /// </summary>
        private void UpdateInvValue()
        {
            List<PriceExpanderItem> items = new List<PriceExpanderItem>();
            float totalValue = 0;
            foreach (KeyValuePair<string, float> kvp in GetCategoryValues())
            {
                items.Add(new PriceExpanderItem() { Label = kvp.Key + " Value:", Price = kvp.Value });
                totalValue += kvp.Value;
            }
            TotalValueMessage = "Inventory Value: " + totalValue.ToString("c");

            CategoryPrices = new ObservableCollection<PriceExpanderItem>(items);
        }

        /// <summary>
        /// Used to calculate value of each category - different method for master vs new inventory list
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, float> GetCategoryValues()
        {
            if (IsMasterList)
                return _models.GetCategoryValues();

            Dictionary<string, float> costDict = _models.GetPrepCatValues();

            foreach (InventoryItem item in _inventoryItems)
            {
                if (!costDict.Keys.Contains(item.Category))
                    costDict[item.Category] = 0;
                costDict[item.Category] += item.PriceExtension;
            }

            return costDict;
        }
        #endregion

        /// <summary>
        /// Called from New Inventory: saves the filtered items
        /// </summary>
        public void SaveNew(DateTime invDate)
        {
            FilterText = "";
            foreach (VendorInventoryItem item in _inventoryItems)
            {
                InventoryItem invItem = item.ToInventoryItem();
                _models.AddUpdateInventoryItem(ref invItem);
                _models.VendorInvItems.First(x => x.Id == item.Id).SetVendorItem(item.SelectedVendor, invItem);
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

            ParentContext.InventoryTab.InvListVM.UpdateInvValue();
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
