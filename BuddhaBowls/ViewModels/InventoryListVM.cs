using BuddhaBowls.Models;
using BuddhaBowls.UserControls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace BuddhaBowls
{
    public delegate void CountChangedDel();

    public class InventoryListVM : TabVM
    {
        private CountChangedDel CountChanged;
        private List<InventoryItem> _inventoryItems;
        private Inventory _inventory;

        public InventoryListControl TabControl { get; set; }

        #region Content Binders

        private ObservableCollection<InventoryItem> _filteredItems;
        public ObservableCollection<InventoryItem> FilteredItems
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

        private bool _countReadOnly;
        public bool CountReadOnly
        {
            get
            {
                return _countReadOnly;
            }
            set
            {
                _countReadOnly = value;
                NotifyPropertyChanged("CountReadOnly");
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
                    _saveOrderVisibility = Visibility.Hidden;
                else
                    _saveOrderVisibility = Visibility.Visible;
                    
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
                    _editOrderVisibility = Visibility.Hidden;
                else
                    _editOrderVisibility = Visibility.Visible;

                NotifyPropertyChanged("EditOrderVisibility");
                NotifyPropertyChanged("SaveOrderVisibility");
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

        public InventoryListVM() : base()
        {
            _inventoryItems = _models.InventoryItems;
            FilteredItems = new ObservableCollection<InventoryItem>(_inventoryItems);
            TabControl = new InventoryListControl(this);
            UpdateInvValue();
            CountReadOnly = true;
            TabControl.HideArrowColumn();

            AddCommand = new RelayCommand(AddInventoryItem);
            DeleteCommand = new RelayCommand(DeleteInventoryItem, x => SelectedInventoryItem != null);
            EditCommand = new RelayCommand(EditInventoryItem, x => SelectedInventoryItem != null);
            ResetCommand = new RelayCommand(ResetList);
            EditOrderCommand = new RelayCommand(StartEditOrder);
            SaveOrderCommand = new RelayCommand(SaveOrder);
        }

        public InventoryListVM(CountChangedDel countDel) : this()
        {
            CountChanged = countDel;
            CountReadOnly = false;
        }

        public InventoryListVM(CountChangedDel countDel, Inventory inv) : this(countDel)
        {
            _inventory = inv;
            _inventoryItems = inv.GetInventoryHistory();
            FilteredItems = new ObservableCollection<InventoryItem>(_inventoryItems);
            UpdateInvValue();
        }

        #region ICommand Helpers

        private void AddInventoryItem(object obj)
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
                _models.InventoryItems.Remove(SelectedInventoryItem);
                SelectedInventoryItem.Destroy();
                SelectedInventoryItem = null;
                ParentContext.Refresh();
            }
        }

        private void EditInventoryItem(object obj)
        {
            NewInventoryItemWizard wizard = new NewInventoryItemWizard(SelectedInventoryItem);
            wizard.Add("New Item");
        }

        private void ResetList(object obj)
        {
            FilterText = "";
            FilterItems("");
        }

        private void StartEditOrder(object obj)
        {
            TabControl.ShowArrowColumn();
            SaveOrderVisibility = Visibility.Visible;
        }

        private void SaveOrder(object obj)
        {
            TabControl.HideArrowColumn();
            EditOrderVisibility = Visibility.Visible;
        }

        #endregion

        #region Initializers

        #endregion

        #region Update UI Methods

        /// <summary>
        /// Filter list of inventory items based on the string in the filter box above datagrids
        /// </summary>
        /// <param name="filterStr"></param>
        public override void FilterItems(string filterStr)
        {
            //if (string.IsNullOrWhiteSpace(filterStr) && CountReadOnly)
            //    TabControl.ShowArrowColumn();
            //else
            //    TabControl.HideArrowColumn();
            FilteredItems = ParentContext.FilterInventoryItems(filterStr, _inventoryItems);
        }

        public void Refresh()
        {
            FilteredItems = new ObservableCollection<InventoryItem>(ParentContext.SortItems(_inventoryItems));
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
            List<InventoryItem> orderedList = FilteredItems.ToList();
            int idx = orderedList.IndexOf(item);
            orderedList.RemoveAt(idx);

            if (idx > 0 && up)
                orderedList.Insert(idx - 1, item);
            if (idx < orderedList.Count - 1 && !up)
                orderedList.Insert(idx + 1, item);

            FilteredItems = new ObservableCollection<InventoryItem>(orderedList);
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
            TotalValueMessage = "Inventory Value: " + FilteredItems.Sum(x => x.LastPurchasedPrice * x.Count).ToString("c");
            foreach (string category in _models.ItemCategories)
            {
                float value = _inventoryItems.Where(x => x.Category.ToUpper() == category.ToUpper()).Sum(x => x.LastPurchasedPrice * x.Count);
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
            foreach(InventoryItem item in FilteredItems)
            {
                item.Update();
            }

            Inventory inv = new Inventory(invDate);
            inv.Id = inv.Insert(_inventoryItems);

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
            _inventory.Update(_inventoryItems);
        }

    }

    public class PriceExpanderItem
    {
        public string Label { get; set; }
        public float Price { get; set; }
    }
}
