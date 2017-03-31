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
    public class NewRecipeVM : TempTabVM
    {
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

        private string _header;
        public string Header
        {
            get
            {
                return _header;
            }
            set
            {
                _header = value;
                NotifyPropertyChanged("Header");
            }
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get
            {
                return _errorMessage;
            }
            set
            {
                _errorMessage = value;
                NotifyPropertyChanged("ErrorMessage");
            }
        }

        private ObservableCollection<FieldSetting> _fieldsCollection;
        public ObservableCollection<FieldSetting> FieldsCollection
        {
            get
            {
                return _fieldsCollection;
            }
            set
            {
                _fieldsCollection = value;
                NotifyPropertyChanged("FieldsCollection");
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
        #endregion

        #region ICommand Properties and Can Execute

        public ICommand AddItemCommand { get; set; }
        public ICommand RemoveItemCommand { get; set; }
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

        public bool SaveCanExecute
        {
            get
            {
                return string.IsNullOrEmpty(ErrorMessage);
            }
        }

        #endregion

        public NewRecipeVM(bool isBatch)
        {
            IsBatch = isBatch;
            _tabControl = new NewRecipe(this);

            Header = "New " + (isBatch ? "Batch Recipe" : "Menu Item");
            _availableItems = new List<IItem>();

            AddItemCommand = new RelayCommand(AddItem);
            RemoveItemCommand = new RelayCommand(RemoveItem, x => RemoveCanExecute);
            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
            ModalOkCommand = new RelayCommand(ModalOk, x => ModalOkCanExecute);
            ModalCancelCommand = new RelayCommand(ModalCancel);

            InitFieldsCollection();
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

        private void Save(object obj)
        {
            Recipe newRecipe = new Recipe();
            ErrorMessage = ParentContext.ObjectFromFields(ref newRecipe, FieldsCollection, true);
            newRecipe.IsBatch = IsBatch;
            newRecipe.ItemList = Ingredients.ToList();

            if (string.IsNullOrEmpty(ErrorMessage))
            {
                newRecipe.Insert();
                _models.Recipes.Add(newRecipe);
                ParentContext.RecipeTab.RefreshList();
                Close();
            }
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

        protected void InitFieldsCollection(IItem item = null)
        {
            if(item == null)
            {
                item = new Recipe();
            }
            if(IsBatch)
            {
                FieldsCollection = new ObservableCollection<FieldSetting>(new List<FieldSetting>()
                {
                    new FieldSetting("Name") { Value = item.Name },
                    new FieldSetting("Category") { Value = item.Category },
                    new FieldSetting("RecipeUnit") { Value = item.RecipeUnit },
                    new FieldSetting("RecipeUnitConversion") { Value = item.RecipeUnitConversion.ToString() },
                    new FieldSetting("Count") { Value = item.Count.ToString() },
                });
            }
            else
            {
                FieldsCollection = new ObservableCollection<FieldSetting>(new List<FieldSetting>()
                {
                    new FieldSetting("Name") { Value = item.Name },
                    new FieldSetting("Price") { Value = ((Recipe)item).Price.ToString() },
                });
            }
        }

        #endregion

        #region Update UI

        protected void Refresh()
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

        public void ClearErrors()
        {
            ErrorMessage = "";
            foreach (FieldSetting field in FieldsCollection)
            {
                field.Error = 0;
            }

            NotifyPropertyChanged("FieldsCollection");
        }

        #endregion
    }
}
