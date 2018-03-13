using BuddhaBowls.Messengers;
using BuddhaBowls.Models;
using BuddhaBowls.UserControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BuddhaBowls
{
    public class NewPrepItemVM : WizardVM
    {
        private bool _newItem;

        #region Content Binders

        private PrepItem _item;
        public PrepItem Item
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

        private List<IItem> _itemList;
        public List<IItem> ItemList
        {
            get
            {
                return _itemList;
            }
            set
            {
                _itemList = value;
                NotifyPropertyChanged("ItemList");
            }
        }

        private IItem _selectedBaseItem;
        public IItem SelectedBaseItem
        {
            get
            {
                return _selectedBaseItem;
            }
            set
            {
                _selectedBaseItem = value;
                Item.SetItem(SelectedBaseItem);
                NotifyPropertyChanged("SelectedBaseItem");
            }
        }

        private List<string> _prepCountUnitList;
        public List<string> PrepCountUnitList
        {
            get
            {
                return _prepCountUnitList;
            }
            set
            {
                _prepCountUnitList = value;
                NotifyPropertyChanged("PrepCountUnitList");
            }
        }

        #endregion

        #region ICommand and CanExecute

        #endregion

        public NewPrepItemVM() : base()
        {
            Item = new PrepItem();
            Header = "New Prep Item";
            _newItem = true;
            ItemList = _models.GetAllIItems();
            PrepCountUnitList = _models.GetCountUnits();
            FinishVisibility = Visibility.Visible;
        }

        public NewPrepItemVM(PrepItem item) : this()
        {
            Item = item;
            SelectedBaseItem = item.GetBaseItem();
            Header = "Edit " + item.Name;
            _newItem = false;
        }

        #region ICommand Helpers

        #endregion

        #region Initializers

        #endregion

        #region Update UI

        #endregion

        protected override void SetWizardStep()
        {
            WizardStepControl = new NewPrepItem(this);
            base.SetWizardStep();
        }

        protected override void FinishWizard(object obj)
        {
            if (ValidateInputs())
            {
                _models.PIContainer.AddItem(Item);
                base.FinishWizard(obj);
            }
        }

        protected override bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(Item.Name))
            {
                ErrorMessage = "Must supply prep item name";
                return false;
            }
            if (_newItem && _models.PIContainer.Items.FirstOrDefault(x => x.Name.ToUpper().Replace(" ", "") == Item.Name) != null)
            {
                ErrorMessage = Item.Name + " already exists as a prep item";
                return false;
            }

            ErrorMessage = "";
            return true;
        }
    }
}
