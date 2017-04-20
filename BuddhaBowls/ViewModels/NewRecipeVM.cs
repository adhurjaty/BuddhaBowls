using BuddhaBowls.Helpers;
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
        private bool _newItem;
        private AddItemDel<Recipe> SaveItem;
        protected List<IItem> _availableItems;

        #region Content Binders

        public string Name { get; set; }

        private ObservableCollection<IItem> _ingredients;
        public ObservableCollection<IItem> Ingredients
        {
            get
            {
                return _ingredients;
            }
            set
            {
                _ingredients = value;
                NotifyPropertyChanged("Ingredients");
            }
        }

        public float Price { get; set; }

        private InventoryItem _selectedItem;
        public InventoryItem SelectedItem
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

        public InventoryItem ItemToAdd { get; set; }

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
        /// <param name="addDel"></param>
        private NewRecipeVM(AddItemDel<Recipe> addDel)
        {
            AddItemCommand = new RelayCommand(AddItem);
            RemoveItemCommand = new RelayCommand(RemoveItem, x => RemoveCanExecute);
            ModalOkCommand = new RelayCommand(ModalOk, x => ModalOkCanExecute);
            ModalCancelCommand = new RelayCommand(ModalCancel);

            CategoryList = _models.GetRecipeCategories();
            SaveItem = addDel;
            FinishVisibility = Visibility.Visible;
        }

        /// <summary>
        /// New recipe constructor
        /// </summary>
        /// <param name="isBatch"></param>
        /// <param name="addDel"></param>
        public NewRecipeVM(bool isBatch, AddItemDel<Recipe> addDel) : this(addDel)
        {
            _newItem = true;
            IsBatch = isBatch;

            Header = "New " + (isBatch ? "Batch Recipe" : "Menu Item");
            _availableItems = new List<IItem>();

            Item = new Recipe();
            Item.IsBatch = isBatch;
            Refresh();
        }

        /// <summary>
        /// Edit recipe constructor
        /// </summary>
        /// <param name="recipe"></param>
        /// <param name="addDel"></param>
        public NewRecipeVM(Recipe recipe, AddItemDel<Recipe> addDel) : this(addDel)
        {
            _newItem = false;
            IsBatch = recipe.IsBatch;
            Header = "Edit " + recipe.Name;
            _availableItems = recipe.ItemList;

            Item = recipe;
            Refresh();
        }

        #region ICommand Helpers

        private void AddItem(object obj)
        {
            ModalTitle = "Add Ingredient";
            ParentContext.ModalContext = this;
            ModalVisibility = Visibility.Visible;
        }

        private void RemoveItem(object obj)
        {
            _availableItems.Remove(SelectedItem);
            Refresh();
        }

        private void Cancel(object obj)
        {
            Close();
        }

        private void ModalOk(object obj)
        {
            ItemToAdd.Count = 0;
            _availableItems.Add(ItemToAdd);
            Refresh();
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

        public override void Refresh()
        {
            if (_availableItems == null)
            {
                Ingredients = new ObservableCollection<IItem>();
                RemainingItems = new ObservableCollection<IItem>(_models.InventoryItems.OrderBy(x => x.Name));
            }
            else
            {
                Ingredients = new ObservableCollection<IItem>(MainHelper.SortItems(_availableItems));
                RemainingItems = new ObservableCollection<IItem>(_models.InventoryItems
                                                                        .Where(x => !_availableItems.Select(y => y.Id).Contains(x.Id))
                                                                        .OrderBy(x => x.Name));
            }
        }

        protected override void SetWizardStep()
        {
            WizardStepControl = new NewRecipe(this);
            base.SetWizardStep();
        }

        protected override void FinishWizard(object obj)
        {
            if (ValidateInputs())
            {
                Item.ItemList = Ingredients.ToList();

                if (_newItem)
                {
                    Item.Insert();
                    _models.Recipes.Add(Item);
                }
                else
                {
                    Item.Update();
                }
                SaveItem(Item);
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
            if(_models.Recipes.Select(x => x.Name.ToUpper().Replace(" ", "")).Contains(Item.Name.ToUpper().Replace(" ", "")))
            {
                ErrorMessage = Item.Name + " already exists";
                NameError = 2;
                return false;
            }

            NameError = 0;
            ErrorMessage = "";
            return true;
        }
        #endregion
    }
}
