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
    public class VendorTabVM : TabVM
    {
        private Dictionary<int, ObservableCollection<InventoryItem>> _vItemsCache;

        #region Data Bindings
        public string FilterText { get; set; }

        private ObservableCollection<Vendor> _filteredVendorList;
        public ObservableCollection<Vendor> FilteredVendorList
        {
            get
            {
                return _filteredVendorList;
            }
            set
            {
                _filteredVendorList = value;
                NotifyPropertyChanged("FilteredVendorList");
            }
        }

        private Vendor _selectedVendor;
        public Vendor SelectedVendor
        {
            get
            {
                return _selectedVendor;
            }
            set
            {
                _selectedVendor = value;
                SelectedVendorItem = null;
                if (_selectedVendor != null)
                    LoadVendorItems();
                else
                    SelectedVendorItems = new ObservableCollection<InventoryItem>();
                NotifyPropertyChanged("SelectedVendor");
            }
        }

        public string AddEditHeader { get; private set; }
        public ObservableCollection<FieldSetting> FieldsCollection { get; private set; }

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

        private ObservableCollection<InventoryItem> _selectedVendorItems;
        public ObservableCollection<InventoryItem> SelectedVendorItems
        {
            get
            {
                return _selectedVendorItems;
            }
            set
            {
                _selectedVendorItems = value;
                NotifyPropertyChanged("SelectedVendorItems");
            }
        }

        /// <summary>
        /// Selected inventory item (needs vendor to be selected)
        /// </summary>
        private InventoryItem _selectedVendorItem;
        public InventoryItem SelectedVendorItem
        {
            get
            {
                return _selectedVendorItem;
            }
            set
            {
                _selectedVendorItem = value;
                NotifyPropertyChanged("SelectedVendorItem");
            }
        }

        private List<string> _purchasedUnitsList;
        public List<string> PurchasedUnitsList
        {
            get
            {
                return _purchasedUnitsList;
            }
            set
            {
                _purchasedUnitsList = value;
                NotifyPropertyChanged("PurchasedUnitsList");
            }
        }
        #endregion

        #region ICommand Bindings and Can Execute
        public ICommand AddVendorCommand { get; set; }
        // save button
        //public ICommand SaveCommand { get; set; }
        //// reset button
        //public ICommand ResetCommand { get; set; }
        // Change Rec List button in vendor main tab
        public ICommand ChangeRecListOrderCommand { get; set; }
        public ICommand EditVendorCommand { get; set; }
        public ICommand GetOrderSheetCommand { get; set; }

        // add and remove the item that the selected vendor sells
        public ICommand AddVendorItemCommand { get; set; }
        public ICommand DeleteVendorItemCommand { get; set; }

        public bool SelectedVendorCanExecute
        {
            get
            {
                return SelectedVendor != null;
            }
        }

        public bool GenOrderSheetCanExecute
        {
            get
            {
                if(SelectedVendor != null)
                {
                    return File.Exists(SelectedVendor.GetOrderSheetPath());
                }

                return false;
            }
        }

        #endregion

        public VendorTabVM() : base()
        {
            AddVendorCommand = new RelayCommand(AddVendor, x => DBConnection);
            //SaveCommand = new RelayCommand(SaveVendor, x => DBConnection);
            //ResetCommand = new RelayCommand(ResetVendor, x => DBConnection);
            //ChangeRecListOrderCommand = new RelayCommand(ChangeRecOrder, x => SelectedVendorCanExecute);
            EditVendorCommand = new RelayCommand(EditVendor, x => SelectedVendorCanExecute && DBConnection);
            GetOrderSheetCommand = new RelayCommand(GenerateOrderSheet, x => GenOrderSheetCanExecute && DBConnection);
            AddVendorItemCommand = new RelayCommand(AddVendorItem, x => SelectedVendor != null);
            DeleteVendorItemCommand = new RelayCommand(DeleteVendorItem, x => SelectedVendorItem != null);

            _vItemsCache = new Dictionary<int, ObservableCollection<InventoryItem>>();
            PurchasedUnitsList = _models.GetPurchasedUnits();
            InitVendors();
        }

        #region ICommand Helpers

        private void AddVendor(object obj)
        {
            NewVendorWizardVM newVendor = new NewVendorWizardVM();
            newVendor.Add("New Vendor");
        }

        //private void ResetVendor(object obj)
        //{
        //    _vItemsCache = new Dictionary<int, ObservableCollection<VendorInventoryItem>>();
        //    LoadVendorItems();
        //}

        //private void SaveVendor(object obj)
        //{
        //    foreach (KeyValuePair<int, ObservableCollection<VendorInventoryItem>> kvp in _vItemsCache)
        //    {
        //        Vendor v = _models.Vendors.First(x => x.Id == kvp.Key);
        //        v.Update(kvp.Value.Select(x => x.ToInventoryItem()).ToList());
        //    }
        //}

        private void EditVendor(object obj)
        {
            _vItemsCache.Remove(SelectedVendor.Id);
            NewVendorWizardVM tabVM = new NewVendorWizardVM(SelectedVendor);
            tabVM.Add("Vendor Items");
        }

        private void GenerateOrderSheet(object obj)
        {
            //if(File.Exists(SelectedVendor.GetOrderSheetPath()))
            //{
            //    new Thread(delegate ()
            //    {
            //        System.Diagnostics.Process.Start(SelectedVendor.GetOrderSheetPath());
            //    }).Start();
            //}
            //else
            //{
            ParentContext.GenerateVendorOrderList(SelectedVendor);
            //}
        }

        private void AddVendorItem(object obj)
        {
            List<InventoryItem> remainingItems = _models.InventoryItems.Where(x => !SelectedVendorItems.Select(y => y.Id).Contains(x.Id)).ToList();
            ModalVM<InventoryItem> modal = new ModalVM<InventoryItem>("Add Inv Item", remainingItems, AddInvItemToVendor);
            ParentContext.ModalContext = modal;
        }

        private void DeleteVendorItem(object obj)
        {
            SelectedVendor.RemoveInvItem(SelectedVendorItem);
            SelectedVendorItems.Remove(SelectedVendorItem);
        }

        #endregion

        #region Initializers

        private void InitVendors()
        {
            if (DBConnection)
                Refresh();
            else
                FilteredVendorList = new ObservableCollection<Vendor>() { new Vendor() { Name = "Could not connect to DB" } };
        }

        #endregion

        #region Update UI Methods
        public override void Refresh()
        {
            //SelectedVendorItems = new ObservableCollection<VendorInventoryItem>();
            FilteredVendorList = new ObservableCollection<Vendor>(_models.Vendors.OrderBy(x => x.Name));
            _vItemsCache = new Dictionary<int, ObservableCollection<InventoryItem>>();
            SelectedVendor = SelectedVendor;
        }

        public void FilterVendors(string filterStr)
        {
            if (string.IsNullOrWhiteSpace(filterStr))
                new ObservableCollection<Vendor>(_models.Vendors.OrderBy(x => x.Name));
            else
                FilteredVendorList = new ObservableCollection<Vendor>(_models.Vendors
                                                        .Where(x => x.Name.ToUpper().Contains(filterStr.ToUpper()))
                                                        .OrderBy(x => x.Name.ToUpper().IndexOf(filterStr.ToUpper())));
        }

        public void ClearErrors()
        {
            AddEditErrorMessage = "";
        }

        private void LoadVendorItems()
        {
            if (!_vItemsCache.Keys.Contains(SelectedVendor.Id))
            {
                List<InventoryItem> vendorItems = _models.VendorInvItems.Select(x => x.GetInvItemFromVendor(SelectedVendor))
                                                                        .Where(x => x != null).ToList();

                _vItemsCache[SelectedVendor.Id] = new ObservableCollection<InventoryItem>(MainHelper.SortItems(vendorItems));
            }

            SelectedVendorItems = _vItemsCache[SelectedVendor.Id];
        }

        public void AddInvItemToVendor(InventoryItem item)
        {
            SelectedVendorItems.Add(item);
            SelectedVendorItems = new ObservableCollection<InventoryItem>(MainHelper.SortItems(SelectedVendorItems));
            _vItemsCache[SelectedVendor.Id] = SelectedVendorItems;
            _models.VendorInvItems.First(x => x.Id == item.Id).AddVendor(SelectedVendor, item);

            SelectedVendor.Update(SelectedVendorItems.ToList());
        }

        public void VendorItemChanged(InventoryItem item)
        {
            item.NotifyChanges();
            _models.VendorInvItems.First(x => x.Id == item.Id).SetVendorItem(SelectedVendor, item);
            item.Update();
        }
        #endregion
    }
}
