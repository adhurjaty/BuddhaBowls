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
    /// Permanent tab showing the recipes. Has multiple states for which types of recipes to show
    /// </summary>
    public class RecipeTabVM : ChangeableTabVM
    {
        private List<DisplayRecipe> _recipeItems;

        #region Content Binders
        private ObservableCollection<DisplayRecipe> _filteredItems;
        public ObservableCollection<DisplayRecipe> FilteredItems
        {
            get
            {
                return _filteredItems;
            }
            set
            {
                _filteredItems = value;
                NotifyPropertyChanged("FilteredItems");
            }
        }

        public DisplayRecipe SelectedItem { get; set; }

        private string _filterText;
        public string FilterText
        {
            get
            {
                return _filterText;
            }
            set
            {
                _filterText = value;
                NotifyPropertyChanged("FilterText");
            }
        }

        private List<string> _recipeUnitList;
        public List<string> RecipeUnitList
        {
            get
            {
                return _recipeUnitList;
            }
            set
            {
                _recipeUnitList = value;
                NotifyPropertyChanged("RecipeUnitList");
            }
        }

        #endregion

        #region ICommand and CanExecute

        public ICommand AddNewItemCommand { get; set; }
        public ICommand DeleteItemCommand { get; set; }
        public ICommand BatchItemSelectCommand { get; set; }
        public ICommand MenuItemSelectCommand { get; set; }
        public ICommand EditItemCommand { get; set; }

        public bool SelectedItemCanExecute
        {
            get
            {
                return SelectedItem != null;
            }
        }

        #endregion

        public RecipeTabVM() : base()
        {
            TabControl = new RecipeTabControl(this);
            Header = "Recipe";

            InitSwitchButtons(new string[] { "Batch Items", "Menu Items" });

            AddNewItemCommand = new RelayCommand(AddItem, x => DBConnection);
            DeleteItemCommand = new RelayCommand(DeleteItem, x => SelectedItemCanExecute && DBConnection);
            EditItemCommand = new RelayCommand(EditRecipe, x => SelectedItemCanExecute && DBConnection);

            RecipeUnitList = _models.GetRecipeUnits();
        }

        #region ICommand Helpers

        private void AddItem(object obj)
        {
            //NewRecipeVM tabVM = new NewRecipeVM(_pageIndex == 0, SaveRecipeHandler);
            NewRecipeVM tabVM = new NewRecipeVM(_pageIndex == 0);
            tabVM.Add("New Recipe");
        }

        private void DeleteItem(object obj)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete " + SelectedItem.Name,
                                                      "Delete " + SelectedItem.Name + "?", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                _models.Recipes.Remove(SelectedItem.GetRecipe());
                SelectedItem.GetRecipe().Destroy();
                SelectedItem = null;
                RefreshList();
            }
        }

        private void EditRecipe(object obj)
        {
            //NewRecipeVM tabVM = new NewRecipeVM(SelectedItem.GetRecipe(), SaveRecipeHandler);
            NewRecipeVM tabVM = new NewRecipeVM(SelectedItem.GetRecipe());
            tabVM.Add("Edit Recipe");
        }

        #endregion

        #region Initializers

        #endregion

        #region Update UI Methods

        /// <summary>
        /// Filter list of inventory items based on the string in the filter box above datagrids
        /// </summary>
        /// <param name="filterStr"></param>
        public override void FilterItems(string filterStr)
        {
            FilteredItems = MainHelper.FilterInventoryItems(filterStr, _recipeItems);
        }

        public void RefreshList()
        {
            ChangePageState(_pageIndex);
        }

        private void SaveRecipeHandler(Recipe rec)
        {
            ChangePageState(_pageIndex);
        }

        public void RowEdited(DisplayRecipe item)
        {
            int idx = FilteredItems.IndexOf(item);
            item.GetRecipe().Update();
        }
        #endregion

        protected override void ChangePageState(int pageIdx)
        {
            base.ChangePageState(pageIdx);

            switch (pageIdx)
            {
                case 0:
                    _recipeItems = _models.Recipes.Where(x => x.IsBatch).Select(x => new DisplayRecipe(x)).ToList();
                    break;
                case 1:
                    _recipeItems = _models.Recipes.Where(x => !x.IsBatch).Select(x => new DisplayRecipe(x)).ToList();
                    break;
                case -1:
                    _recipeItems = new List<DisplayRecipe>() { new DisplayRecipe(new Recipe { Name = "DB not found" }) };
                    break;
            }

            FilterText = "";
            FilteredItems = new ObservableCollection<DisplayRecipe>(_recipeItems);
        }
    }

    public class DisplayRecipe : INotifyPropertyChanged, ISortable
    {
        // INotifyPropertyChanged event and method
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private Recipe _recipe;

        public string Name
        {
            get
            {
                return _recipe.Name;
            }
            set
            {
                _recipe.Name = value;
                NotifyPropertyChanged("Name");
            }
        }

        public string Category
        {
            get
            {
                return _recipe.Category;
            }
            set
            {
                _recipe.Category = value;
                NotifyPropertyChanged("Category");
            }
        }

        public string RecipeUnit
        {
            get
            {
                return _recipe.RecipeUnit;
            }
            set
            {
                _recipe.RecipeUnit = value;
                NotifyPropertyChanged("RecipeUnit");
            }
        }

        public float? RecipeUnitConversion
        {
            get
            {
                return _recipe.RecipeUnitConversion;
            }
            set
            {
                _recipe.RecipeUnitConversion = value;
                NotifyPropertyChanged("RecipeUnitConversion");
                NotifyPropertyChanged("CostPerRU");
            }
        }

        public float CostPerRU
        {
            get
            {
                return _recipe.CostPerRU;
            }
        }

        public float RecipeCost
        {
            get
            {
                return _recipe.RecipeCost;
            }
        }

        public List<CategoryProportion> ProportionDetails
        {
            get
            {
                List<IItem> totalItems = _recipe.GetRecipeItems().Select(x => x.GetIItem()).ToList();
                float total = totalItems.Sum(x => x.RecipeCost);
                return totalItems.GroupBy(x => x.Category).ToDictionary(x => x.Key, x => x)
                                 .Select(x => new CategoryProportion(x.Value.ToList(), total)).ToList();
            }
        }

        public DisplayRecipe(Recipe rec)
        {
            _recipe = rec;
        }

        public Recipe GetRecipe()
        {
            return _recipe;
        }
    }

    public class CategoryProportion
    {
        public string Name { get; set; }
        public float Cost { get; set; }
        public float CostProportion { get; set; }

        public CategoryProportion(List<IItem> items, float total)
        {
            Name = items[0].Category;
            Cost = items.Sum(x => x.RecipeCost);
            if (total > 0)
                CostProportion = Cost / total;
            else
                CostProportion = 0;
        }
    }
}
