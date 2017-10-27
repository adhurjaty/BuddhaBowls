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
        //private AddItemDel<Recipe> SaveItem;
        protected List<RecipeItem> _recipeItems;

        #region Content Binders

        public string Name { get; set; }

        private ObservableCollection<Ingredient> _ingredients;
        public ObservableCollection<Ingredient> Ingredients
        {
            get
            {
                return _ingredients;
            }
            set
            {
                _ingredients = value;
                NotifyPropertyChanged("Ingredients");
                NotifyPropertyChanged("RecipeCost");
            }
        }

        public float Price { get; set; }

        private Ingredient _selectedItem;
        public Ingredient SelectedItem
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

        public float RecipeCost
        {
            get
            {
                if (Ingredients != null && Ingredients.Count > 0)
                    return Ingredients.Sum(x => x.RecipeCost);
                return 0;
            }
        }

        public List<CategoryProportion> ProportionDetails
        {
            get
            {
                List<Dictionary<string, float>> catCosts = Ingredients.Select(x => x.GetRecipeItem().GetIItem().GetCategoryCosts()).ToList();
                Dictionary<string, float> combinedCatCosts = new Dictionary<string, float>();
                foreach (Dictionary<string, float> dict in catCosts)
                {
                    foreach (KeyValuePair<string, float> kvp in dict)
                    {
                        if (!combinedCatCosts.ContainsKey(kvp.Key))
                            combinedCatCosts[kvp.Key] = 0;
                        combinedCatCosts[kvp.Key] += kvp.Value;
                    }
                }
                float total = combinedCatCosts.Sum(x => x.Value);
                return combinedCatCosts.Select(x => new CategoryProportion(x.Key, x.Value, total))
                                       .OrderByDescending(x => x.CostProportion).ToList();
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
        private NewRecipeVM(): base()        // AddItemDel<Recipe> addDel) : base()
        {
            AddItemCommand = new RelayCommand(AddItem);
            RemoveItemCommand = new RelayCommand(RemoveItem, x => RemoveCanExecute);
            ModalOkCommand = new RelayCommand(ModalOk, x => ModalOkCanExecute);
            ModalCancelCommand = new RelayCommand(ModalCancel);

            CategoryList = _models.RContainer.GetRecipeCategories();
            //SaveItem = addDel;
            FinishVisibility = Visibility.Visible;
        }

        /// <summary>
        /// New recipe constructor
        /// </summary>
        /// <param name="isBatch"></param>
        /// <param name="addDel"></param>
        public NewRecipeVM(bool isBatch) : this()         //, AddItemDel<Recipe> addDel) : this(addDel)
        {
            _newItem = true;
            IsBatch = isBatch;

            Header = "New " + (isBatch ? "Batch Recipe" : "Menu Item");
            _recipeItems = new List<RecipeItem>();

            Item = new Recipe();
            Item.IsBatch = isBatch;
            Item.RecipeUnitConversion = 1;
            Refresh();
        }

        /// <summary>
        /// Edit recipe constructor
        /// </summary>
        /// <param name="recipe"></param>
        /// <param name="addDel"></param>
        public NewRecipeVM(Recipe recipe) : this()          // AddItemDel<Recipe> addDel) : this(addDel)
        {
            _newItem = false;
            IsBatch = recipe.IsBatch;
            Header = "Edit " + recipe.Name;
            _recipeItems = recipe.GetRecipeItems();

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
            _recipeItems.RemoveAll(x => x.Name == SelectedItem.Name);
            Refresh();
        }

        private void ModalOk(object obj)
        {
            _recipeItems.Add(new Ingredient(ItemToAdd).GetRecipeItem());
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
            if (_recipeItems == null)
            {
                Ingredients = new ObservableCollection<Ingredient>();
                RemainingItems = new ObservableCollection<IItem>(_models.GetAllIItems().OrderBy(x => x.Name));
            }
            else
            {
                Ingredients = new ObservableCollection<Ingredient>(MainHelper.SortItems(_recipeItems.Select(x => new Ingredient(x))));
                RemainingItems = new ObservableCollection<IItem>(_models.GetAllIItems()
                                                                        .Where(x => !_recipeItems.Select(y => y.Name).Contains(x.Name))
                                                                        .OrderBy(x => x.Name));
            }
            if (Item != null)
                RemainingItems.Remove(Item);
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
                if (_newItem)
                {
                    Item.Insert(Ingredients.Select(x => x.GetRecipeItem()).ToList());
                    _models.RContainer.AddItem(Item);
                }
                else
                {
                    Item.Update(Ingredients.Select(x => x.GetRecipeItem()).ToList());
                    _models.RContainer.Update(Item);
                }
                //SaveItem(Item);
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
            NotifyPropertyChanged("RecipeCost");
            NotifyPropertyChanged("ProportionDetails");
        }

        #endregion
    }

    public class Ingredient : ISortable, INotifyPropertyChanged
    {
        // INotifyPropertyChanged event and method
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private RecipeItem _item;

        public string Name
        {
            get
            {
                return _item.Name;
            }
        }
        public string Category
        {
            get
            {
                return _item.GetIItem().Category;
            }
        }
        public string Measure
        {
            get
            {
                return _item.Measure;
            }
            set
            {
                _item.Measure = value;
                NotifyPropertyChanged("Measure");
            }
        }
        public string RecipeUnit
        {
            get
            {
                return _item.GetIItem().RecipeUnit;
            }
        }
        public float Count
        {
            get
            {
                return _item.Quantity;
            }
            set
            {
                _item.Quantity = value;
                NotifyPropertyChanged("Measure");
                NotifyPropertyChanged("RecipeCost");
            }
        }
        public float CostPerRU
        {
            get
            {
                return _item.GetIItem().CostPerRU;
            }
        }
        public float RecipeCost
        {
            get
            {
                return _item.GetIItem().RecipeCost;
            }
        }

        public Ingredient(RecipeItem item)
        {
            _item = item;
        }

        public Ingredient(IItem item)
        {
            _item = new RecipeItem()
            {
                Name = item.Name,
                Quantity = 0
            };

            if (item.GetType() == typeof(InventoryItem))
                _item.InventoryItemId = item.Id;
            else
                _item.InventoryItemId = null;
        }

        public RecipeItem GetRecipeItem()
        {
            return _item;
        }
    }
}
