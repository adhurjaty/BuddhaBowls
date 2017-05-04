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
        private AddItemDel<PrepItem> SaveItem;
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

        private List<string> _nameList;
        public List<string> NameList
        {
            get
            {
                return _nameList;
            }
            set
            {
                _nameList = value;
                NotifyPropertyChanged("NameList");
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

        //private int _nameError;
        //public int NameError
        //{
        //    get
        //    {
        //        return _nameError;
        //    }
        //    set
        //    {
        //        _nameError = value;
        //        NotifyPropertyChanged("NameError");
        //    }
        //}

        #endregion

        #region ICommand and CanExecute

        #endregion

        public NewPrepItemVM(AddItemDel<PrepItem> addDel) : base()
        {
            SaveItem = addDel;
            Item = new PrepItem();
            Header = "New Prep Item";
            _newItem = true;
            NameList = _models.GetAllIItems().Select(x => x.Name).ToList();
            PrepCountUnitList = _models.GetPrepCountUnits();
            FinishVisibility = Visibility.Visible;
        }

        public NewPrepItemVM(PrepItem item, AddItemDel<PrepItem> addDel) : this(addDel)
        {
            Item = item;
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
                if(_newItem)
                {
                    Item.Insert();
                    _models.PrepItems.Add(Item);
                }
                else
                {
                    Item.Update();
                }
                SaveItem(Item);
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
            if (_newItem && _models.PrepItems.Select(x => x.Name.ToUpper().Replace(" ", "")).Contains(Item.Name.ToUpper().Replace(" ", "")))
            {
                ErrorMessage = Item.Name + " already exists as a prep item";
                return false;
            }

            ErrorMessage = "";
            return true;
        }
    }
}
