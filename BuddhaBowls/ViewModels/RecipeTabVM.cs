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
        private List<Recipe> _recipeItems;

        #region Content Binders
        private ObservableCollection<IItem> _filteredItems;
        public ObservableCollection<IItem> FilteredItems
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

        public Recipe SelectedItem { get; set; }

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
            PrimaryPageName = "Batch Items";
            SecondaryPageName = "Menu Items";

            AddNewItemCommand = new RelayCommand(AddItem, x => DBConnection);
            DeleteItemCommand = new RelayCommand(DeleteItem, x => SelectedItemCanExecute && DBConnection);
            EditItemCommand = new RelayCommand(EditRecipe, x => SelectedItemCanExecute && DBConnection);
        }

        #region ICommand Helpers

        private void AddItem(object obj)
        {
            NewRecipeVM tabVM = new NewRecipeVM(_pageState == PageState.Primary);
            tabVM.Add("New Recipe");
        }

        private void DeleteItem(object obj)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete " + SelectedItem.Name,
                                                      "Delete " + SelectedItem.Name + "?", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                _models.Recipes.Remove(SelectedItem);
                SelectedItem.Destroy();
                SelectedItem = null;
                RefreshList();
            }
        }

        private void ChangeToMenuState(object obj)
        {
            ChangePageState(PageState.Secondary);
        }

        private void ChangeToBatchState(object obj)
        {
            ChangePageState(PageState.Primary);
        }

        private void EditRecipe(object obj)
        {
            EditRecipeVM tabVM = new EditRecipeVM(_pageState == PageState.Primary, SelectedItem);
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
        public void FilterInventoryItems(string filterStr)
        {
            FilteredItems = MainHelper.FilterInventoryItems(filterStr, _recipeItems.Select(x => (IItem)x));
        }

        public void RefreshList()
        {
            ChangePageState(_pageState);
        }
        #endregion

        protected override void ChangePageState(PageState state)
        {
            base.ChangePageState(state);

            switch (state)
            {
                case PageState.Primary:
                    _recipeItems = _models.Recipes.Where(x => x.IsBatch).ToList();
                    break;
                case PageState.Secondary:
                    _recipeItems = _models.Recipes.Where(x => !x.IsBatch).ToList();
                    break;
                case PageState.Error:
                    _recipeItems = new List<Recipe>() { new Recipe() { Name = "DB not found" } };
                    break;
            }

            FilterText = "";
            FilteredItems = new ObservableCollection<IItem>(_recipeItems);
        }
    }
}
