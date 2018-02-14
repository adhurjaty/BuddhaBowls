using BuddhaBowls.Helpers;
using BuddhaBowls.Messengers;
using BuddhaBowls.Models;
using BuddhaBowls.Services;
using BuddhaBowls.UserControls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace BuddhaBowls
{
    /// <summary>
    /// Temp tab to create a new recipe
    /// </summary>
    public class NewRecipeVM : WizardVM
    {
        //private Action Cleanup;
        private bool _newItem;
        //private AddItemDel<Recipe> SaveItem;
        //protected List<RecipeItem> _recipeItems;

        #region Content Binders


        private string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                NotifyPropertyChanged("Name");
            }
        }

        private float _price;
        public float Price
        {
            get
            {
                return _price;
            }
            set
            {
                _price = value;
                NotifyPropertyChanged("Price");
            }
        }

        private IItem _selectedItem;
        public IItem SelectedItem
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

        private ObservableCollection<IItem> _remainingItems;
        public ObservableCollection<IItem> RemainingItems
        {
            get
            {
                return _remainingItems;
            }
            set
            {
                _remainingItems = value;
                NotifyPropertyChanged("RemainingItems");
            }
        }

        public IItem ItemToAdd { get; set; }

        private Visibility _modalVisibility = Visibility.Hidden;
        public Visibility ModalVisibility
        {
            get
            {
                return _modalVisibility;
            }
            set
            {
                _modalVisibility = value;
                NotifyPropertyChanged("ModalVisibility");
            }
        }

        public Visibility BatchVisibility { get; set; }
        public Visibility MenuVisibility { get; set; }

        private bool _isBatch;
        public bool IsBatch
        {
            get
            {
                return _isBatch;
            }
            set
            {
                _isBatch = value;
                BatchVisibility = _isBatch ? Visibility.Visible : Visibility.Hidden; 
                MenuVisibility = _isBatch ? Visibility.Hidden : Visibility.Visible;
                NotifyPropertyChanged("BatchVisibility");
                NotifyPropertyChanged("MenuVisibility");
            }
        }

        private string _modalTitle;
        public string ModalTitle
        {
            get
            {
                return _modalTitle;
            }
            set
            {
                _modalTitle = value;
                NotifyPropertyChanged("ModalTitle");
            }
        }

        private Recipe _item;
        public Recipe Item
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

        #endregion

        #region ICommand Properties and Can Execute

        public ICommand AddItemCommand { get; set; }
        public ICommand RemoveItemCommand { get; set; }
        public ICommand ModalOkCommand { get; set; }
        public ICommand ModalCancelCommand { get; set; }

        public bool RemoveCanExecute
        {
            get
            {
                return SelectedItem != null;
            }
        }

        public bool ModalOkCanExecute
        {
            get
            {
                return ItemToAdd != null;
            }
        }

        public bool SaveCanExecute
        {
            get
            {
                return string.IsNullOrEmpty(ErrorMessage);
            }
        }

        #endregion

        /// <summary>
        /// Default constructor
        /// </summary>
        private NewRecipeVM(): base()
        {
            AddItemCommand = new RelayCommand(AddItem);
            RemoveItemCommand = new RelayCommand(RemoveItem, x => RemoveCanExecute);
            ModalOkCommand = new RelayCommand(ModalOk, x => ModalOkCanExecute);
            ModalCancelCommand = new RelayCommand(ModalCancel);

            CategoryList = _models.RContainer.GetRecipeCategories();
            FinishVisibility = Visibility.Visible;
        }

        /// <summary>
        /// New recipe constructor
        /// </summary>
        /// <param name="isBatch"></param>
        public NewRecipeVM(bool isBatch) : this()
        {
            _newItem = true;
            IsBatch = isBatch;

            Header = "New " + (isBatch ? "Batch Recipe" : "Menu Item");

            Item = new Recipe();
            Item.IsBatch = isBatch;
            Item.RecipeUnitConversion = 1;
            //Refresh();
        }

        /// <summary>
        /// Edit recipe constructor
        /// </summary>
        /// <param name="recipe"></param>
        /// <param name="addDel"></param>
        public NewRecipeVM(Recipe recipe) : this()
        {
            _newItem = false;
            IsBatch = recipe.IsBatch;
            Header = "Edit " + recipe.Name;

            Item = (Recipe)recipe.Copy();
            // HACK: best way I could think to remove the recipe copies containers
            //Cleanup = new Action(() => recipe.RemoveCopy(Item));
            //Refresh();
        }

        #region ICommand Helpers

        private void AddItem(object obj)
        {
            ModalTitle = "Add Ingredient";
            RemainingItems = new ObservableCollection<IItem>(_models.GetAllIItems().Where(x => Item.ItemList.FirstOrDefault(y => x.Name == y.Name) == null)
                                                                                   .Where(x => x.Name != Item.Name));
            ParentContext.ModalContext = this;
            ModalVisibility = Visibility.Visible;
        }

        private void RemoveItem(object obj)
        {
            Item.RemoveItem(SelectedItem);
        }

        private void ModalOk(object obj)
        {
            Item.AddItem(ItemToAdd);
            ModalVisibility = Visibility.Hidden;
        }

        private void ModalCancel(object obj)
        {
            ItemToAdd = null;
            ModalVisibility = Visibility.Hidden;
        }

        #endregion

        #region Initializers

        #endregion

        #region Update UI

        //public override void Refresh()
        //{
        //    if (_recipeItems == null)
        //    {
        //        Ingredients = new ObservableCollection<Ingredient>();
        //        RemainingItems = new ObservableCollection<IItem>(_models.GetAllIItems().OrderBy(x => x.Name));
        //    }
        //    else
        //    {
        //        Ingredients = new ObservableCollection<Ingredient>(MainHelper.SortItems(_recipeItems.Select(x => new Ingredient(x))));
        //        RemainingItems = new ObservableCollection<IItem>(_models.GetAllIItems()
        //                                                                .Where(x => !_recipeItems.Select(y => y.Name).Contains(x.Name))
        //                                                                .OrderBy(x => x.Name));
        //    }
        //    if (Item != null)
        //        RemainingItems.Remove(Item);
        //}

        protected override void SetWizardStep()
        {
            WizardStepControl = new NewRecipe(this);
            base.SetWizardStep();
        }

        protected override void Cancel(object obj)
        {
            //Cleanup?.Invoke();
            base.Cancel(obj);
        }

        protected override void FinishWizard(object obj)
        {
            if (ValidateInputs())
            {
                _models.RContainer.AddItem(Item);
                //Cleanup?.Invoke();
                Close();
            }
        }

        protected override bool ValidateInputs()
        {
            if(string.IsNullOrWhiteSpace(Item.Name))
            {
                ErrorMessage = "Must supply recipe name";
                NameError = 2;
                return false;
            }
            if(_newItem && _models.RContainer.Items.Select(x => x.Name.ToUpper().Replace(" ", "")).Contains(Item.Name.ToUpper().Replace(" ", "")))
            {
                ErrorMessage = Item.Name + " already exists";
                NameError = 2;
                return false;
            }

            NameError = 0;
            ErrorMessage = "";
            return true;
        }

        public void CountChanged()
        {
            NotifyPropertyChanged("Item");
            //Item.UpdateProperties();
        }

        #endregion
    }
}
