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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace BuddhaBowls
{
    public delegate void StatusUpdatedDel();

    public class InventoryListVM : TabVM
    {
        private StatusUpdatedDel CountChanged;
        private VendorInvItemsContainer _invItemsContainer;
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

            InitContainer();
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

            InitContainer();
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
            _models.LoadInvContainer(_inventory);
            CountChanged = countDel;
            IsMasterList = false;

            InitContainer();
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
                SelectedInventoryItem.Destroy();
                Properties.Settings.Default.InventoryOrder.Remove(SelectedInventoryItem.Name);
                _invItemsContainer.RemoveItem(SelectedInventoryItem);
                SelectedInventoryItem = null;
            }
        }

        private void EditInventoryItem(object obj)
        {
            NewInventoryItemWizard wizard = new NewInventoryItemWizard(SelectedInventoryItem);
            wizard.Add("New Item");
        }

        /// <summary>
        /// Return the displayed inventory list to original order (no filter text)
        /// </summary>
        /// <param name="obj"></param>
        private void ResetList(object obj)
        {
            FilterItems("");
            InitContainer();
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
            FilteredItems = MainHelper.FilterInventoryItems(filterStr, _invItemsContainer.Items);
        }

        public void InitContainer()
        {
            if (IsMasterList)
                _invItemsContainer = _models.VIContainer;
            else
            {
                if (_inventory == null)
                {
                    _invItemsContainer = _models.VIContainer.Copy();
                }
                else
                {
                    _invItemsContainer = new VendorInvItemsContainer(_inventory.InvItemsContainer, _models.VContainer);
                }
            }
            FilterText = "";
            CollectionChanged();
            //_invItemsContainer.AddUpdateBinding(CollectionChanged);
            //_invItemsContainer.AddUpdateBinding(UpdateInvValue);
            //_invItemsContainer.PushChange();
        }

        public void CollectionChanged()
        {
            FilteredItems = new ObservableCollection<VendorInventoryItem>(_invItemsContainer.Items);
        }

        /// <summary>
        /// Sync the copy inventory list with the master. Maintain count values however
        /// </summary>
        //public void SyncCopyInvList()
        //{
        //    Dictionary<int, float> vCountDict = _invItemsContainer.Items.ToDictionary(x => x.Id, x => x.Count);
        //    InitContainer();
        //    foreach (VendorInventoryItem item in _invItemsContainer.Items)
        //    {
        //        if (vCountDict.ContainsKey(item.Id))
        //            item.Count = vCountDict[item.Id];
        //    }
        //}

        public void MoveDown(VendorInventoryItem item)
        {
            MoveInList(item, false);
        }

        public void MoveUp(VendorInventoryItem item)
        {
            MoveInList(item, true);
        }

        private void MoveInList(VendorInventoryItem item, bool up)
        {
            List<VendorInventoryItem> newItemInList = MainHelper.MoveInList(item, up, FilteredItems.ToList());

            FilteredItems = new ObservableCollection<VendorInventoryItem>(newItemInList);
        }

        private void SaveInvOrder()
        {
            Properties.Settings.Default.InventoryOrder = FilteredItems.Select(x => x.Name).ToList();
            Properties.Settings.Default.Save();

            _invItemsContainer.SaveOrder();
        }

        /// <summary>
        /// Called from code-behind. Writes changes to the DB if Master list otherwise display relevant changes
        /// </summary>
        /// <param name="item"></param>
        public void RowEdited(VendorInventoryItem item)
        {
            //if (IsMasterList)
            //{
            //    item.Update();
            //    _models.VIContainer.UpdateCopies(item);
            //}
            _invItemsContainer.UpdateCopies(item);
            //item.NotifyAllChanges();
            UpdateInvValue();
        }

        /// <summary>
        /// Resets the inventory count to the saved value before changing the datagrid. Called from New Inventory form
        /// </summary>
        /// <param name="obj"></param>
        public void ResetCount()
        {
            FilterText = "";
            foreach (VendorInventoryItem item in FilteredItems)
            {
                item.Count = item.GetLastCount();
            }
            InitContainer();
        }

        /// <summary>
        /// Upates prices in the category price breakdown dropdown list
        /// </summary>
        public void UpdateInvValue()
        {
            List<PriceExpanderItem> items = new List<PriceExpanderItem>();
            float totalValue = 0;
            foreach (KeyValuePair<string, float> kvp in _invItemsContainer.GetCategoryValues())
            {
                items.Add(new PriceExpanderItem() { Label = kvp.Key + " Value:", Price = kvp.Value });
                totalValue += kvp.Value;
            }
            TotalValueMessage = "Inventory Value: " + totalValue.ToString("c");

            CategoryPrices = new ObservableCollection<PriceExpanderItem>(items);
        }
        #endregion

        public VendorInvItemsContainer GetItemsContainer()
        {
            return _invItemsContainer;
        }
    }

    public class PriceExpanderItem
    {
        public string Label { get; set; }
        public float Price { get; set; }
    }
}
