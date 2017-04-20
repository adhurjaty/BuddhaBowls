using BuddhaBowls.Helpers;
using BuddhaBowls.Models;
using BuddhaBowls.UserControls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace BuddhaBowls
{
    public delegate void AddInvDel(InventoryItem item);

    public class NewInventoryItemWizard : WizardVM
    {
        private bool _newItem;
        private AddInvDel AddInv;

        #region Content Binders

        private InventoryItem _item;
        public InventoryItem Item
        {
            get
            {
                return _item;
            }
            set
            {
                _item = value;
                NotifyPropertyChanged("Item");
            }
        }

        private float _yield = 100;
        public float Yield
        {
            get
            {
                return _yield;
            }
            set
            {
                _yield = value;
                Item.Yield = _yield / 100f;
                NotifyPropertyChanged("Yield");
            }
        }

        private ObservableCollection<VendorInfo> _vendorList;
        public ObservableCollection<VendorInfo> VendorList
        {
            get
            {
                return _vendorList;
            }
            set
            {
                _vendorList = value;
                NotifyPropertyChanged("VendorList");
            }
        }

        private VendorInfo _selectedItem;
        public VendorInfo SelectedItem
        {
            get
            {
                return _selectedItem;
            }
            set
            {
                _selectedItem = value;
                NotifyPropertyChanged("SelectedItem");
            }
        }

        private ObservableCollection<InventoryItem> _invOrderList;
        public ObservableCollection<InventoryItem> InvOrderList
        {
            get
            {
                return _invOrderList;
            }
            set
            {
                _invOrderList = value;
                NotifyPropertyChanged("InvOrderList");
            }
        }

        private InventoryItem _selectedOrderedItem;
        public InventoryItem SelectedOrderedItem
        {
            get
            {
                return _selectedOrderedItem;
            }
            set
            {
                _selectedOrderedItem = value;
                NotifyPropertyChanged("SelectedOrderedItem");
            }
        }
        #endregion

        #region ICommand and Can Execute

        public ICommand AddVendorCommand { get; set; }
        public ICommand DeleteVendorCommand { get; set; }
        public ICommand PlaceAboveCommand { get; set; }
        public ICommand PlaceBelowCommand { get; set; }
        public ICommand MoveUpCommand { get; set; }
        public ICommand MoveDownCommand { get; set; }

        #endregion

        public NewInventoryItemWizard(AddInvDel addInv) : base()
        {
            _newItem = true;
            AddInv = addInv;
            Item = new InventoryItem();
            SetDefaultValues();
            VendorList = new ObservableCollection<VendorInfo>();
            Header = "New Inventory Item";

            InitICommand();
        }

        public NewInventoryItemWizard(AddInvDel addInv, InventoryItem item) : base()
        {
            _newItem = false;
            AddInv = addInv;
            Item = item;
            Header = "Edit Inventory Item";

            Yield = (item.Yield ?? 1) * 100;
            InitVendors();
            InitICommand();
        }

        #region ICommand Helpers

        private void AddVendor(object obj)
        {
            List<Vendor> remainingItems = _models.Vendors.Where(x => !VendorList.Select(y => y.Vendor).Contains(x.Name)).ToList();
            ModalVM modal = new ModalVM("Add Vendor", remainingItems, AddVendor);
            ParentContext.ModalContext = modal;
        }

        private void DeleteVendor(object obj)
        {
            VendorList.Remove(SelectedItem);
        }

        protected override void FinishWizard(object obj)
        {
            if (ValidateInputs())
            {
                InventoryItem invItem = Item;

                Properties.Settings.Default.InventoryOrder = InvOrderList.Select(x => x.Name).ToList();
                Properties.Settings.Default.Save();

                _models.AddUpdateInventoryItem(ref invItem);

                foreach(VendorInfo v in VendorList)
                {
                    Vendor vendor = _models.Vendors.First(x => x.Name == v.Vendor);
                    invItem.LastPurchasedPrice = v.Price;
                    invItem.Conversion = v.Conversion;
                    invItem.PurchasedUnit = v.PurchasedUnit;
                    invItem.Yield = Yield;
                    vendor.Update(invItem);
                }

                invItem.Update();

                AddInv(invItem);
                Close();
            }
        }

        private void PlaceAbove(object obj)
        {
            int idx = InvOrderList.IndexOf(SelectedOrderedItem);
            RemoveExistingItem();
            InvOrderList.Insert(idx, Item);
        }

        private void PlaceBelow(object obj)
        {
            RemoveExistingItem();
            int idx = InvOrderList.IndexOf(SelectedOrderedItem);
            if(idx == InvOrderList.Count - 1)
                InvOrderList.Add(Item);
            else
                InvOrderList.Insert(idx + 1, Item);
        }

        private void MoveUp(object obj)
        {
            SelectedOrderedItem = GetItemInOrderList();
            if(SelectedOrderedItem != null)
            {
                MoveInList(SelectedOrderedItem, true);
            }
        }

        private void MoveDown(object obj)
        {
            SelectedOrderedItem = GetItemInOrderList();
            if (SelectedOrderedItem != null)
            {
                MoveInList(SelectedOrderedItem, false);
            }
        }

        private void MoveInList(InventoryItem item, bool up)
        {
            List<InventoryItem> newItemInList = MainHelper.MoveInList(item, up, InvOrderList.ToList());

            InvOrderList = new ObservableCollection<InventoryItem>(newItemInList);
        }

        #endregion

        #region Initializers

        private void InitICommand()
        {
            AddVendorCommand = new RelayCommand(AddVendor);
            DeleteVendorCommand = new RelayCommand(DeleteVendor, x => SelectedItem != null);
            PlaceAboveCommand = new RelayCommand(PlaceAbove, x => SelectedOrderedItem != null);
            PlaceBelowCommand = new RelayCommand(PlaceBelow, x => SelectedOrderedItem != null);
            MoveUpCommand = new RelayCommand(MoveUp);
            MoveDownCommand = new RelayCommand(MoveDown);
        }

        private void SetDefaultValues()
        {
            Item.RecipeUnitConversion = 1;
        }

        #endregion

        private InventoryItem GetItemInOrderList()
        {
            return InvOrderList.FirstOrDefault(x => x.Id == Item.Id);
        }

        private void RemoveExistingItem()
        {
            InventoryItem existingItem = GetItemInOrderList();
            if (existingItem != null)
                InvOrderList.Remove(existingItem);
        }

        private void InitVendors()
        {
            Dictionary<Vendor, InventoryItem> vendorDict = _models.GetVendorsFromItem(Item);
            VendorList = new ObservableCollection<VendorInfo>();

            foreach(KeyValuePair<Vendor, InventoryItem> kvp in vendorDict)
            {
                Vendor v = kvp.Key;
                InventoryItem invItem = kvp.Value;

                VendorList.Add(new VendorInfo(v, invItem));
            }
        }

        protected override void SetWizardStep()
        {
            switch (_currentStep)
            {
                case 0:
                    WizardStepControl = new AddInvStep1(this);
                    BackVisibility = Visibility.Hidden;
                    Header = "Item Info";
                    break;
                case 1:
                    WizardStepControl = new AddInvStep2(this);
                    NextVisibility = Visibility.Visible;
                    BackVisibility = Visibility.Visible;
                    Header = "Vendor Info";
                    break;
                case 2:
                    if (InvOrderList == null)
                        InvOrderList = new ObservableCollection<InventoryItem>(MainHelper.SortItems(_models.InventoryItems));
                    if (!_newItem)
                        SelectedOrderedItem = InvOrderList.FirstOrDefault(x => x.Id == Item.Id);
                    WizardStepControl = new AddInvStep3(this);
                    FinishVisibility = Visibility.Visible;
                    Header = "Put item in order";
                    break;
                default:
                    break;
            }
        }

        protected override bool ValidateInputs()
        {
            if(_currentStep == 0)
            {
                if (string.IsNullOrWhiteSpace(Item.Name) || (_models.InventoryItems.Select(x => x.Name.ToUpper()).Contains(Item.Name.ToUpper())
                    && _newItem))
                    return false;
                return !string.IsNullOrEmpty(Item.Name) &&
                       !string.IsNullOrEmpty(Item.Category) &&
                       !string.IsNullOrEmpty(Item.CountUnit) &&
                       !string.IsNullOrEmpty(Item.RecipeUnit) &&
                       Item.RecipeUnitConversion != 0 &&
                       Item.Yield != 0;
            }

            return base.ValidateInputs();
        }

        private void AddVendor(Vendor item)
        {
            VendorList.Add(new VendorInfo(item));
        }
    }

    public class VendorInfo
    {
        public string Vendor { get; set; }
        public float Price { get; set; }
        public string PurchasedUnit { get; set; }
        public float Conversion { get; set; }

        public VendorInfo()
        {
            Conversion = 1;
            PurchasedUnit = "EA";
        }

        public VendorInfo(Vendor vendor) : this()
        {
            Vendor = vendor.Name;
        }

        public VendorInfo(Vendor vendor, InventoryItem item) : this()
        {
            Vendor = vendor.Name;
            Price = item.LastPurchasedPrice;
            PurchasedUnit = item.PurchasedUnit;
            Conversion = item.Conversion;
        }
    }
}
