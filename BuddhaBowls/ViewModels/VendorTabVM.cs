using BuddhaBowls.Helpers;
using BuddhaBowls.Messengers;
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
        //private Dictionary<int, ObservableCollection<InventoryItem>> _vItemsCache;

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
                return SelectedVendorItems != null && SelectedVendorItems.Count > 0;
            }
        }

        #endregion

        public VendorTabVM() : base()
        {
            AddVendorCommand = new RelayCommand(AddVendor, x => DBConnection);
            EditVendorCommand = new RelayCommand(EditVendor, x => SelectedVendorCanExecute && DBConnection);
            GetOrderSheetCommand = new RelayCommand(GenerateOrderSheet, x => GenOrderSheetCanExecute && DBConnection);
            AddVendorItemCommand = new RelayCommand(AddVendorItem, x => SelectedVendor != null);
            DeleteVendorItemCommand = new RelayCommand(DeleteVendorItem, x => SelectedVendorItem != null);
            ChangeRecListOrderCommand = new RelayCommand(EditRecList, x => SelectedVendor != null);

            //_vItemsCache = new Dictionary<int, ObservableCollection<InventoryItem>>();
            PurchasedUnitsList = _models.GetPurchasedUnits();
            InitVendors();
            //_models.VIContainer.AddUpdateBinding(Refresh);
            Messenger.Instance.Register<Message>(MessageTypes.VENDORS_CHANGED, (msg) => Refresh());
        }

        #region ICommand Helpers

        private void AddVendor(object obj)
        {
            NewVendorWizardVM newVendor = new NewVendorWizardVM();
            newVendor.Add("New Vendor");
        }

        private void EditVendor(object obj)
        {
            //_vItemsCache.Remove(SelectedVendor.Id);
            NewVendorWizardVM tabVM = new NewVendorWizardVM(SelectedVendor);
            tabVM.Add("Vendor Items");
        }

        private void GenerateOrderSheet(object obj)
        {
            ParentContext.GenerateVendorOrderList(SelectedVendor);
        }

        private void EditRecList(object obj)
        {
            ChangeRecListOrderVM recList = new ChangeRecListOrderVM(SelectedVendor);
            recList.Add("Vendor Items Order");
        }

        private void AddVendorItem(object obj)
        {
            List<InventoryItem> remainingItems = _models.VIContainer.Items.Where(x => !SelectedVendorItems.Select(y => y.Id).Contains(x.Id))
                                                                          .Select(x => x.ToInventoryItem()).ToList();
            ModalVM<InventoryItem> modal = new ModalVM<InventoryItem>("Add Inv Item", remainingItems, AddInvItemToVendor);
            ParentContext.ModalContext = modal;
        }

        private void DeleteVendorItem(object obj)
        {
            _models.VIContainer.RemoveFromVendor(SelectedVendorItem, SelectedVendor);
            SelectedVendor.Update();
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
            FilteredVendorList = new ObservableCollection<Vendor>(_models.VContainer.Items.OrderBy(x => x.Name));
            //_vItemsCache = new Dictionary<int, ObservableCollection<InventoryItem>>();
            SelectedVendor = SelectedVendor;
        }

        public void FilterVendors(string filterStr)
        {
            if (string.IsNullOrWhiteSpace(filterStr))
                FilteredVendorList = new ObservableCollection<Vendor>(_models.VContainer.Items.OrderBy(x => x.Name));
            else
                FilteredVendorList = new ObservableCollection<Vendor>(_models.VContainer.Items
                                                        .Where(x => x.Name.ToUpper().Contains(filterStr.ToUpper()))
                                                        .OrderBy(x => x.Name.ToUpper().IndexOf(filterStr.ToUpper())));
        }

        public void ClearErrors()
        {
            AddEditErrorMessage = "";
        }

        private void LoadVendorItems()
        {
            SelectedVendorItems = new ObservableCollection<InventoryItem>(SelectedVendor.ItemList);
        }

        public void AddInvItemToVendor(InventoryItem item)
        {
            _models.VIContainer.UpdateItem((InventoryItem)item.Copy(), SelectedVendor);
            SelectedVendor.Update();
        }

        public void VendorItemChanged(InventoryItem item)
        {
            SelectedVendor.Update();
            Messenger.Instance.NotifyColleagues(MessageTypes.VENDOR_INV_ITEMS_CHANGED);
        }
        #endregion
    }
}
