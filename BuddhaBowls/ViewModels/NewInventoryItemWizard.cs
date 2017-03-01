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
    public class NewInventoryItemWizard : WizardVM
    {
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
        #endregion

        #region ICommand and Can Execute

        public ICommand AddVendorCommand { get; set; }
        public ICommand DeleteVendorCommand { get; set; }

        #endregion

        public NewInventoryItemWizard() : base()
        {
            Item = new InventoryItem();
            SetDefaultValues();
            VendorList = new ObservableCollection<VendorInfo>();

            InitICommand();
        }

        public NewInventoryItemWizard(InventoryItem item) : base()
        {
            Item = item;

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
                _models.AddInventoryItem(Item);

                foreach(VendorInfo v in VendorList)
                {
                    Vendor vendor = _models.Vendors.First(x => x.Name == v.Vendor);
                    vendor.AddInvItem(Item);
                }

                ParentContext.Refresh();
                Close();
            }
        }

        #endregion

        #region Initializers

        private void InitICommand()
        {
            AddVendorCommand = new RelayCommand(AddVendor);
            DeleteVendorCommand = new RelayCommand(DeleteVendor, x => SelectedItem != null);
        }

        private void SetDefaultValues()
        {
            Item.RecipeUnitConversion = 1;
        }

        #endregion

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
                    break;
                case 1:
                    WizardStepControl = new AddInvStep2(this);
                    FinishVisibility = Visibility.Visible;
                    BackVisibility = Visibility.Visible;
                    break;
                default:
                    break;
            }
        }

        protected override bool ValidateInputs()
        {
            if(_currentStep == 0)
            {
                if (_models.InventoryItems.Select(x => x.Name.ToUpper()).Contains(Item.Name.ToUpper()))
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
