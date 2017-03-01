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
        #endregion

        #region ICommand and CanExecute

        public ICommand AddCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand EditCommand { get; set; }
        public ICommand ResetCommand { get; set; }

        #endregion

        public InventoryListVM() : base()
        {
            FilteredItems = new ObservableCollection<InventoryItem>(_models.InventoryItems);
            TabControl = new InventoryListControl(this);
            UpdateInvValue();
            CountReadOnly = true;

            AddCommand = new RelayCommand(AddInventoryItem);
            DeleteCommand = new RelayCommand(DeleteInventoryItem, x => SelectedInventoryItem != null);
            EditCommand = new RelayCommand(EditInventoryItem, x => SelectedInventoryItem != null);
            ResetCommand = new RelayCommand(ResetList);
        }

        public InventoryListVM(CountChangedDel countDel) : this()
        {
            CountChanged = countDel;
            CountReadOnly = false;
            TabControl.HideArrowColumn();
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
            if (string.IsNullOrWhiteSpace(filterStr) && CountReadOnly)
                TabControl.ShowArrowColumn();
            else
                TabControl.HideArrowColumn();
            FilteredItems = ParentContext.FilterInventoryItems(filterStr, _models.InventoryItems);
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

        private void UpdateInvValue()
        {
            //InventoryValue = FilteredInventoryItems.Sum(x => ((InventoryItem)x).LastPurchasedPrice * x.Count);
            List<PriceExpanderItem> items = new List<PriceExpanderItem>();
            TotalValueMessage = "Inventory Value: " + FilteredItems.Sum(x => x.LastPurchasedPrice * x.Count).ToString("c");
            foreach (string category in _models.ItemCategories)
            {
                float value = _models.InventoryItems.Where(x => x.Category.ToUpper() == category.ToUpper()).Sum(x => x.LastPurchasedPrice * x.Count);
                items.Add(new PriceExpanderItem() { Label = category + " Value:", Price = value });
            }

            CategoryPrices = new ObservableCollection<PriceExpanderItem>(items);
        }
        #endregion
    }

    public class PriceExpanderItem
    {
        public string Label { get; set; }
        public float Price { get; set; }
    }
}
