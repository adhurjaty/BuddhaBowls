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
    public class ChangeRecListOrderVM : ReOrderVM
    {
        private Vendor _vendor;

        #region Content Binders

        private string _selectedItem;
        public string SelectedItem
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

        public InventoryItem MovingItem
        {
            get
            {
                return _itemToMove;
            }
            set
            {
                _itemToMove = value;
                NotifyPropertyChanged("MovingItem");
            }
        }
        #endregion

        #region ICommand and CanExecute

        public ICommand GetMovingItemCommand { get; set; }

        #endregion

        public ChangeRecListOrderVM(Vendor vendor) : base()
        {
            _vendor = vendor;
            Header = _vendor.Name + " ";
            InvOrderList = new ObservableCollection<InventoryItem>(_vendor.ItemList);

            GetMovingItemCommand = new RelayCommand(SetMovingItem, x => SelectedOrderedItem != null);
        }

        #region ICommand Helpers

        private void SetMovingItem(object obj)
        {
            MovingItem = SelectedOrderedItem;
        }

        protected override void FinishWizard(object obj)
        {
            if (ValidateInputs())
            {
                _vendor.SaveItemOrder(InvOrderList.Select(x => x.Name));
                Close();
            }
        }

        #endregion

        #region Initializers

        #endregion

        #region UI Updaters

        protected override void SetWizardStep()
        {
            WizardStepControl = new ChangeRecOrderContol(this);
            FinishVisibility = Visibility.Visible;
            BackVisibility = Visibility.Hidden;
            NextVisibility = Visibility.Hidden;
        }

        protected override bool ValidateInputs()
        {
            return true;
        }

        #endregion
    }
}
