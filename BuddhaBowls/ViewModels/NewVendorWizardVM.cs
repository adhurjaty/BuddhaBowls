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
    public class NewVendorWizardVM : WizardVM
    {
        private bool _newVendor;
        private List<InventoryVendorItem> _inventoryItems;

        #region Content Binders

        private Vendor _vend;
        public Vendor Vend
        {
            get
            {
                return _vend;
            }
            set
            {
                _vend = value;
                NotifyPropertyChanged("Vend");
            }
        }

        private ObservableCollection<InventoryVendorItem> _inventoryList;
        public ObservableCollection<InventoryVendorItem> InventoryList
        {
            get
            {
                return _inventoryList;
            }
            set
            {
                _inventoryList = value;
                NotifyPropertyChanged("InventoryList");
            }
        }

        private InventoryVendorItem _selectedItem;
        public InventoryVendorItem SelectedItem
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

        private Visibility _showSoldVisibility = Visibility.Visible;
        public Visibility ShowSoldVisibility
        {
            get
            {
                return _showSoldVisibility;
            }
            set
            {
                _showSoldVisibility = value;
                NotifyPropertyChanged("ShowSoldVisibility");
                NotifyPropertyChanged("ShowAllVisibility");
            }
        }

        public Visibility ShowAllVisibility
        {
            get
            {
                return _showSoldVisibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
            }
        }

        #endregion

        #region ICommand and CanExecute

        public ICommand OnlySoldCommand { get; set; }
        public ICommand ShowAllCommand { get; set; }

        #endregion

        public NewVendorWizardVM() : base()
        {
            _newVendor = true;
            _inventoryItems = _models.InventoryItems.Select(x => new InventoryVendorItem(x)).ToList();
            InventoryList = new ObservableCollection<InventoryVendorItem>(_inventoryItems);
            Vend = new Vendor();
            Header = "New Vendor";

            OnlySoldCommand = new RelayCommand(ShowSold);
            ShowAllCommand = new RelayCommand(ShowAll);
        }

        public NewVendorWizardVM(Vendor v) : base()
        {
            _newVendor = false;
            _inventoryItems = _models.InventoryItems.Select(x => new InventoryVendorItem(x)).ToList();
            Vend = v;
            List<int> vendItemIds = v.GetInventoryItems().Select(x => x.Id).ToList();
            foreach (InventoryVendorItem item in _inventoryItems)
            {
                if (vendItemIds.Contains(item.Id))
                    item.IsSold = true;
            }
            Header = "Edit Vendor";
            InventoryList = new ObservableCollection<InventoryVendorItem>(_inventoryItems);

            OnlySoldCommand = new RelayCommand(ShowSold);
            ShowAllCommand = new RelayCommand(ShowAll);
        }

        #region ICommand Helpers

        private void ShowSold(object obj)
        {
            InventoryList = new ObservableCollection<InventoryVendorItem>(InventoryList.Where(x => x.IsSold));
            ShowSoldVisibility = Visibility.Hidden;
        }

        private void ShowAll(object obj)
        {
            InventoryList = new ObservableCollection<InventoryVendorItem>(_inventoryItems);
            ShowSoldVisibility = Visibility.Visible;
        }
        #endregion

        #region Base Overrides

        protected override bool ValidateInputs()
        {
            if(_currentStep == 0)
            {
                if (_models.Vendors.FirstOrDefault(x => x.Name == Vend.Name) != null && _newVendor)
                    return false;
                return !string.IsNullOrEmpty(Vend.Name);
            }

            return true;
        }

        protected override void SetWizardStep()
        {
            switch (_currentStep)
            {
                case 0:
                    WizardStepControl = new AddVendorStep1(this);
                    BackVisibility = Visibility.Hidden;
                    break;
                case 1:
                    WizardStepControl = new AddVendorStep2(this);
                    FinishVisibility = Visibility.Visible;
                    BackVisibility = Visibility.Visible;
                    break;
                default:
                    break;
            }
        }

        protected override void DisplayErrorMessage()
        {
            base.DisplayErrorMessage();
        }

        protected override void FinishWizard(object obj)
        {
            if (ValidateInputs())
            {
                Vendor vendor = Vend;
                _models.AddUpdateVendor(ref vendor, InventoryList.Where(x => x.IsSold).Select(x => x.ToInventoryItem()).ToList());

                ParentContext.Refresh();
                Close();
            }
        }

        public override void FilterItems(string filterStr)
        {
            InventoryList = MainHelper.FilterInventoryItems(filterStr, _inventoryItems);
        }
        #endregion
    }

    public class InventoryVendorItem : InventoryItem
    {

        public bool IsSold { get; set; } = false;

        public InventoryVendorItem(InventoryItem item)
        {
            foreach (string property in item.GetPropertiesDB())
            {
                SetProperty(property, item.GetPropertyValue(property));
            }
            Id = item.Id;
        }

        public InventoryItem ToInventoryItem()
        {
            InventoryItem item = new InventoryItem();
            foreach (string property in item.GetPropertiesDB())
            {
                item.SetProperty(property, GetPropertyValue(property));
            }
            item.Id = Id;
            return item;
        }
    }
}
