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

        private int _nameError;
        public int NameError
        {
            get
            {
                return _nameError;
            }
            set
            {
                _nameError = value;
                NotifyPropertyChanged("NameError");
            }
        }

        private Visibility _deleteVendorVisibility = Visibility.Hidden;
        public Visibility DeleteVendorVisibility
        {
            get
            {
                return _deleteVendorVisibility;
            }
            set
            {
                _deleteVendorVisibility = value;
                NotifyPropertyChanged("DeleteVendorVisibility");
            }
        }


        #endregion

        #region ICommand and CanExecute

        public ICommand OnlySoldCommand { get; set; }
        public ICommand ShowAllCommand { get; set; }
        public ICommand DeleteVendorCommand { get; set; }

        #endregion

        public NewVendorWizardVM() : base()
        {
            _newVendor = true;
            Refresh();
            Vend = new Vendor();
            Header = "New Vendor";

            OnlySoldCommand = new RelayCommand(ShowSold);
            ShowAllCommand = new RelayCommand(ShowAll);
            DeleteVendorCommand = new RelayCommand(DeleteVendor);
        }

        public NewVendorWizardVM(Vendor v) : this()
        {
            _newVendor = false;
            Vend = v;
            Refresh();

            Header = "Edit Vendor " + v.Name;
            DeleteVendorVisibility = Visibility.Visible;

            ShowSold(null);
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

        private void DeleteVendor(object obj)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete " + Vend.Name,
                                                      "Delete " + Vend.Name + "?", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                _models.DeleteVendor(Vend);
                Close();
                ParentContext.Refresh();
            }
        }

        #endregion

        #region Base Overrides

        protected override bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(Vend.Name))
            {
                ErrorMessage = "Must supply vendor name";
                NameError = 2;
                return false;
            }
            if (_newVendor && _models.Vendors.Select(x => x.Name.ToUpper().Replace(" ", "")).Contains(Vend.Name.ToUpper().Replace(" ", "")))
            {
                ErrorMessage = Vend.Name + " already exists";
                NameError = 2;
                return false;
            }

            NameError = 0;
            ErrorMessage = "";
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
                //List<InventoryItem> vendItems = 
                _models.AddUpdateVendor(ref vendor, InventoryList.Where(x => x.IsSold).Select(x => x.ToInventoryItem()).ToList());

                Close();
                ParentContext.Refresh();
            }
        }

        public override void FilterItems(string filterStr)
        {
            InventoryList = MainHelper.FilterInventoryItems(filterStr, _inventoryItems);
        }

        public override void Refresh()
        {
            if(_newVendor)
            {
                _inventoryItems = _models.InventoryItems.Select(x => new InventoryVendorItem(x)).ToList();
                InventoryList = new ObservableCollection<InventoryVendorItem>(_inventoryItems);
            }
            else
            {
                List<InventoryItem> vendItems = Vend.ItemList;
                List<int> vendItemIds = vendItems.Select(x => x.Id).ToList();
                foreach (InventoryItem vItem in vendItems)
                {
                    int listItemIdx = _inventoryItems.FindIndex(x => x.Id == vItem.Id);
                    if (listItemIdx != -1)
                    {
                        _inventoryItems[listItemIdx] = new InventoryVendorItem(vItem) { IsSold = true };
                    }
                }

                InventoryList = new ObservableCollection<InventoryVendorItem>(_inventoryItems);
            }
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
