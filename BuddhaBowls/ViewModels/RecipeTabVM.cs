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
        ModelContainer _models;

        // INotifyPropertyChanged event and method
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainViewModel ParentContext { get; set; }

        #region Content Binders
        private ObservableCollection<Recipe> _menuItems;
        public ObservableCollection<Recipe> MenuItems
        {
            get
            {
                return _menuItems;
            }
            set
            {
                _menuItems = value;
                NotifyPropertyChanged("MenuItems");
            }
        }

        public Recipe SelectedMenuItem { get; set; }

        private ObservableCollection<Recipe> _batchItems;
        public ObservableCollection<Recipe> BatchItems
        {
            get
            {
                return _batchItems;
            }
            set
            {
                _batchItems = value;
                NotifyPropertyChanged("BatchItems");
            }
        }

        public Recipe SelectedBatchItem { get; set; }

        #endregion

        #region ICommand and CanExecute
        public ICommand AddNewMenuItemCommand { get; set; }
        public ICommand DeleteMenuItemCommand { get; set; }
        public ICommand AddNewBatchItemCommand { get; set; }
        public ICommand DeleteBatchItemCommand { get; set; }
        public ICommand MenuEditCommand { get; set; }
        public ICommand BatchEditCommand { get; set; }

        public bool SelectedMenuCanExecute
        {
            get
            {
                return SelectedMenuItem != null;
            }
        }

        public bool SelectedBatchCanExecute
        {
            get
            {
                return SelectedBatchItem != null;
            }
        }
        #endregion

        public RecipeTabVM(ModelContainer models, MainViewModel parent)
        {
            _models = models;
            ParentContext = parent;

            AddNewMenuItemCommand = new RelayCommand(AddMenuItem);
            DeleteMenuItemCommand = new RelayCommand(DeleteMenuItem, x => SelectedMenuCanExecute);
            AddNewBatchItemCommand = new RelayCommand(AddBatchItem);
            DeleteBatchItemCommand = new RelayCommand(DeleteBatchItem, x => SelectedBatchCanExecute);
            MenuEditCommand = new RelayCommand(EditMenuItem, x => SelectedMenuCanExecute);
            BatchEditCommand = new RelayCommand(EditBatchItem, x => SelectedBatchCanExecute);

            LoadRecipes();
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

        #endregion

        #region Initializers

        public void LoadRecipes()
        {
            MenuItems = new ObservableCollection<Recipe>(_models.Recipes.Where(x => !x.IsBatch));
            BatchItems = new ObservableCollection<Recipe>(_models.Recipes.Where(x => x.IsBatch));
        }

        #endregion

        #region Update UI Methods

        #endregion
    }
}
