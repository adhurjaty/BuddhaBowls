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
        private Dictionary<int, ObservableCollection<VendorInventoryItem>> _vItemsCache;

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
                if (_selectedVendor != null)
                    LoadVendorItems();
                else
                    SelectedVendorItems = new ObservableCollection<VendorInventoryItem>();
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

        private ObservableCollection<VendorInventoryItem> _selectedVendorItems;
        public ObservableCollection<VendorInventoryItem> SelectedVendorItems
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
        // plus button
        public ICommand AddVendorCommand { get; set; }
        // minus button
        public ICommand DeleteVendorCommand { get; set; }
        // save button
        public ICommand SaveCommand { get; set; }
        // reset button
        public ICommand ResetCommand { get; set; }
        // Change Rec List button in vendor main tab
        public ICommand ChangeRecListOrderCommand { get; set; }
        public ICommand EditVendorCommand { get; set; }
        public ICommand GetOrderSheetCommand { get; set; }

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
            DeleteVendorCommand = new RelayCommand(DeleteVendor, x => SelectedVendorCanExecute && DBConnection);
            SaveCommand = new RelayCommand(SaveVendor, x => DBConnection);
            ResetCommand = new RelayCommand(ResetVendor, x => DBConnection);
            //ChangeRecListOrderCommand = new RelayCommand(ChangeRecOrder, x => SelectedVendorCanExecute);
            EditVendorCommand = new RelayCommand(EditVendor, x => SelectedVendorCanExecute && DBConnection);
            GetOrderSheetCommand = new RelayCommand(GenerateOrderSheet, x => GenOrderSheetCanExecute && DBConnection);

            _vItemsCache = new Dictionary<int, ObservableCollection<VendorInventoryItem>>();
            PurchasedUnitsList = _models.GetPurchasedUnits();
            InitVendors();
        }

        #region ICommand Helpers

        private void AddVendor(object obj)
        {
            NewVendorWizardVM newVendor = new NewVendorWizardVM();
            newVendor.Add("New Vendor");
        }

        private void ResetVendor(object obj)
        {
            _vItemsCache = new Dictionary<int, ObservableCollection<VendorInventoryItem>>();

            LoadVendorItems();
        }

        private void SaveVendor(object obj)
        {
            foreach (KeyValuePair<int, ObservableCollection<VendorInventoryItem>> kvp in _vItemsCache)
            {
                Vendor v = _models.Vendors.First(x => x.Id == kvp.Key);
                v.Update(kvp.Value.Select(x => x.ToInventoryItem()).ToList());
            }
        }

        private void DeleteVendor(object obj)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete " + SelectedVendor.Name,
                                                      "Delete " + SelectedVendor.Name + "?", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                _models.DeleteVendor(SelectedVendor);
                SelectedVendor = null;
                ParentContext.Refresh();
            }
        }

        private void EditVendor(object obj)
        {
            _vItemsCache.Remove(SelectedVendor.Id);
            NewVendorWizardVM tabVM = new NewVendorWizardVM(SelectedVendor);
            tabVM.Add("Vendor Items");
        }

        private void GenerateOrderSheet(object obj)
        {
            if(File.Exists(SelectedVendor.GetOrderSheetPath()))
            {
                new Thread(delegate ()
                {
                    System.Diagnostics.Process.Start(SelectedVendor.GetOrderSheetPath());
                }).Start();
            }
            else
            {
                ParentContext.GenerateVendorOrderList(SelectedVendor);
            }
        }

        #endregion

        #region Initializers

        private void InitVendors()
        {
            if (DBConnection)
                RefreshVendorList();
            else
                FilteredVendorList = new ObservableCollection<Vendor>() { new Vendor() { Name = "Could not connect to DB" } };
        }

        #endregion

        #region Update UI Methods
        public void RefreshVendorList()
        {
            FilteredVendorList = new ObservableCollection<Vendor>(_models.Vendors.OrderBy(x => x.Name));
        }

        public void FilterVendors(string filterStr)
        {
            if (string.IsNullOrWhiteSpace(filterStr))
                RefreshVendorList();
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
            if(!_vItemsCache.Keys.Contains(SelectedVendor.Id))
            {
                List<InventoryItem> vendorItems = SelectedVendor.GetInventoryItems();
                if (vendorItems != null)
                {
                    _vItemsCache[SelectedVendor.Id] = new ObservableCollection<VendorInventoryItem>(MainHelper.SortItems(vendorItems).
                                                            Select(x => new VendorInventoryItem(_models.GetVendorsFromItem(x), x)));
                }
                else
                {
                    _vItemsCache[SelectedVendor.Id] = null;
                }
            }

            SelectedVendorItems = _vItemsCache[SelectedVendor.Id];
        }

        #endregion
    }
}
