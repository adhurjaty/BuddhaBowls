using BuddhaBowls.Models;
using BuddhaBowls.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
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
        public ICommand EditCommand { get; set; }
        public ICommand BatchItemSelectCommand { get; set; }
        public ICommand MenuItemSelectCommand { get; set; }

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

            AddNewItemCommand = new RelayCommand(AddMenuItem);
            DeleteItemCommand = new RelayCommand(DeleteMenuItem, x => SelectedItemCanExecute);
            EditCommand = new RelayCommand(EditMenuItem, x => SelectedItemCanExecute);
            BatchItemSelectCommand = new RelayCommand(ChangeToBatchState, x => _state == PageState.Menu);
            MenuItemSelectCommand = new RelayCommand(ChangeToMenuState, x => _state == PageState.Batch);

            ChangePageState(PageState.Batch);

        }

        #region ICommand Helpers

        private void AddMenuItem(object obj)
        {
            throw new NotImplementedException();
        }

        private void DeleteMenuItem(object obj)
        {
            throw new NotImplementedException();
        }

        private void AddBatchItem(object obj)
        {
            throw new NotImplementedException();
        }

        private void DeleteBatchItem(object obj)
        {
            throw new NotImplementedException();
        }

        private void EditMenuItem(object obj)
        {
            throw new NotImplementedException();
        }

        private void EditBatchItem(object obj)
        {
            throw new NotImplementedException();
        }

        private void ChangeToMenuState(object obj)
        {
            ChangePageState(PageState.Menu);
        }

        private void ChangeToBatchState(object obj)
        {
            ChangePageState(PageState.Batch);
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

        #endregion

        private void ChangePageState(PageState state)
        {
            _state = state;
            switch (state)
            {
                case PageState.Batch:
                    _recipeItems = _models.Recipes.Where(x => x.IsBatch).ToList();
                    ((RelayCommand)AddNewItemCommand).ChangeCallback(AddBatchItem);
                    ((RelayCommand)DeleteItemCommand).ChangeCallback(DeleteBatchItem);
                    ((RelayCommand)EditCommand).ChangeCallback(EditBatchItem);
                    break;
                case PageState.Menu:
                    _recipeItems = _models.Recipes.Where(x => !x.IsBatch).ToList();
                    ((RelayCommand)AddNewItemCommand).ChangeCallback(AddMenuItem);
                    ((RelayCommand)DeleteItemCommand).ChangeCallback(DeleteMenuItem);
                    ((RelayCommand)EditCommand).ChangeCallback(EditMenuItem);
                    break;
            }

            FilterText = "";
            FilteredItems = new ObservableCollection<IItem>(_recipeItems);
        }

    }
}
