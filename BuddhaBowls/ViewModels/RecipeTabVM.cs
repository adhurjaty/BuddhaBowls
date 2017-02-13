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
    public class RecipeTabVM : INotifyPropertyChanged
    {
        private enum PageState { Batch, Menu }
        private PageState _state;

        private ModelContainer _models;
        private List<Recipe> _recipeItems;

        // INotifyPropertyChanged event and method
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainViewModel ParentContext { get; set; }

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

        public RecipeTabVM(ModelContainer models, MainViewModel parent)
        {
            _models = models;
            ParentContext = parent;

            AddNewItemCommand = new RelayCommand(AddItem);
            DeleteItemCommand = new RelayCommand(DeleteItem, x => SelectedItemCanExecute);
            BatchItemSelectCommand = new RelayCommand(ChangeToBatchState, x => _state == PageState.Menu);
            MenuItemSelectCommand = new RelayCommand(ChangeToMenuState, x => _state == PageState.Batch);
            EditItemCommand = new RelayCommand(EditRecipe, x => SelectedItemCanExecute);

            ChangePageState(PageState.Batch);
        }

        #region ICommand Helpers

        private void AddItem(object obj)
        {
            ParentContext.AddTempTab("New Recipe", new NewRecipe(new NewRecipeVM(_models, ParentContext, _state == PageState.Batch)));
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
            ChangePageState(PageState.Menu);
        }

        private void ChangeToBatchState(object obj)
        {
            ChangePageState(PageState.Batch);
        }

        private void EditRecipe(object obj)
        {
            ParentContext.AddTempTab("Edit Recipe", new NewRecipe(new EditRecipeVM(_models, ParentContext, _state == PageState.Batch, (Recipe)SelectedItem)));
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
            FilteredItems = ParentContext.FilterInventoryItems(filterStr, _recipeItems.Select(x => (IItem)x));
        }

        public void RefreshList()
        {
            ChangePageState(_state);
        }
        #endregion

        private void ChangePageState(PageState state)
        {
            _state = state;
            switch (state)
            {
                case PageState.Batch:
                    _recipeItems = _models.Recipes.Where(x => x.IsBatch).ToList();
                    break;
                case PageState.Menu:
                    _recipeItems = _models.Recipes.Where(x => !x.IsBatch).ToList();
                    break;
            }

            FilterText = "";
            FilteredItems = new ObservableCollection<IItem>(_recipeItems);
        }
    }
}
