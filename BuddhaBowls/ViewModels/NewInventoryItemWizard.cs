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
    public class NewInventoryItemWizard : ReOrderVM
    {
        private bool _newItem;

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

        /// <summary>
        /// Constructor for new item
        /// </summary>
        public NewInventoryItemWizard() : base()
        {
            _newItem = true;
            Item = new InventoryItem();
            SetDefaultValues();
            VendorList = new ObservableCollection<VendorInfo>();
            Header = "New Inventory Item";

            InitICommand();
        }

        /// <summary>
        /// Constructor for editing item
        /// </summary>
        /// <param name="item"></param>
        public NewInventoryItemWizard(VendorInventoryItem item) : base()
        {
            _newItem = false;
            Item = item.ToInventoryItem();
            Header = "Edit Inventory Item";

            Yield = (item.Yield ?? 1) * 100;

            VendorList = new ObservableCollection<VendorInfo>();
            foreach (Vendor vend in item.Vendors)
            {
                VendorList.Add(new VendorInfo(vend, item.GetInvItemFromVendor(vend)));
            }

            InitICommand();
        }

        #region ICommand Helpers

        private void AddVendor(object obj)
        {
            List<Vendor> remainingItems = _models.VContainer.Items.Where(x => !VendorList.Select(y => y.Name).Contains(x.Name)).ToList();
            ModalVM<Vendor> modal = new ModalVM<Vendor>("Add Vendor", remainingItems, AddVendor);
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

                // save desired inventory order
                Properties.Settings.Default.InventoryOrder = InvOrderList.Select(x => x.Name).ToList();
                Properties.Settings.Default.Save();
                _models.VIContainer.SaveOrder();

                // update the VendorInventoryItem with vendors
                //VendorInventoryItem vInvItem = _models.VendorInvItems.First(x => x.Id == invItem.Id);
                //vInvItem.InvItem = invItem;
                //vInvItem.Update(VendorList.ToList());

                //ParentContext.AddedInvItem();
                _models.VIContainer.AddItem(invItem, VendorList.ToList());

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

        //private void InitVendors()
        //{
        //    Dictionary<Vendor, InventoryItem> vendorDict = _models.GetVendorsFromItem(Item);
        //    VendorList = new ObservableCollection<VendorInfo>();

        //    foreach(KeyValuePair<Vendor, InventoryItem> kvp in vendorDict.OrderBy(x => x.Key.Name))
        //    {
        //        Vendor v = kvp.Key;
        //        InventoryItem invItem = kvp.Value;

        //        VendorList.Add(new VendorInfo(v, invItem));
        //    }
        //}

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
                    _itemToMove = Item;
                    WizardStepControl = new ChangeItemOrderControl(this);
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

            return true;
        }

        private void AddVendor(Vendor item)
        {
            VendorList.Add(new VendorInfo(item));
            VendorList = new ObservableCollection<VendorInfo>(VendorList.OrderBy(x => x.Name));
        }
    }

    public class VendorInfo
    {
        public Vendor Vend { get; set; }
        public string Name
        {
            get
            {
                return Vend.Name;
            }
        }
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
            Vend = vendor;
        }

        public VendorInfo(Vendor vendor, InventoryItem item) : this()
        {
            Vend = vendor;
            Price = item.LastPurchasedPrice;
            PurchasedUnit = item.PurchasedUnit;
            Conversion = item.Conversion;
        }
    }
}
