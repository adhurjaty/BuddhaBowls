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
using System.Windows;
using System.Windows.Input;

namespace BuddhaBowls
{
    public class NewRecipeVM : INotifyPropertyChanged
    {
        private ModelContainer _models;
        private List<IItem> _availableItems;
        // INotifyPropertyChanged event and method
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainViewModel ParentContext { get; set; }

        #region Content Binders

        public string Name { get; set; }
        public List<Recipe> RecipeItems { get; set; }

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
        public string Category { get; set; }

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

        #endregion

        #region ICommand Properties and Can Execute

        public ICommand AddItemCommand { get; set; }
        public ICommand RemoveItemCommand { get; set; }
        public ICommand ResetCommand { get; set; }
        public ICommand SaveCommand { get; set; }
        public ICommand CancelCommand { get; set; }
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

        #endregion

        public NewRecipeVM(ModelContainer models, MainViewModel mvm)
        {
            _models = models;
            ParentContext = mvm;

            _availableItems = new List<IItem>();

            AddItemCommand = new RelayCommand(AddItem);
            RemoveItemCommand = new RelayCommand(RemoveItem, x => RemoveCanExecute);
            ResetCommand = new RelayCommand(ResetList);
            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
            ModalOkCommand = new RelayCommand(ModalOk, x => ModalOkCanExecute);
            ModalCancelCommand = new RelayCommand(ModalCancel);
        }

        #region ICommand Helpers

        private void AddItem(object obj)
        {
            ParentContext.ModalContext = this;
            ModalVisibility = Visibility.Visible;
        }

        private void RemoveItem(object obj)
        {
            _availableItems.Remove(SelectedItem);
            Refresh();
        }

        private void Save(object obj)
        {
            // save recipe
            ParentContext.DeleteTempTab();
        }

        private void Cancel(object obj)
        {
            ParentContext.DeleteTempTab();
        }

        private void ModalOk(object obj)
        {
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

        private void Refresh()
        {
            if (_availableItems == null)
            {
                Ingredients = new ObservableCollection<IItem>();
                RemainingItems = new ObservableCollection<IItem>(_models.InventoryItems.OrderBy(x => x.Name));
            }
            else
            {
                Ingredients = new ObservableCollection<IItem>(ParentContext.SortItems(_availableItems));
                RemainingItems = new ObservableCollection<IItem>(_models.InventoryItems
                                                                                .Where(x => !_availableItems.Select(y => y.Id).Contains(x.Id))
                                                                                .OrderBy(x => x.Name));
            }
        }

        #endregion
    }
}
