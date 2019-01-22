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
    /// Permanent tab showing the recipes. Has multiple states for which types of recipes to show
    /// </summary>
    public class RecipeTabVM : ChangeableTabVM
    {
        //private List<DisplayRecipe> _recipeItems;

        #region Content Binders
        private ObservableCollection<Recipe> _filteredItems;
        public ObservableCollection<Recipe> FilteredItems
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
            Messenger.Instance.Register<Message>(MessageTypes.RECIPE_CHANGED, (msg) => RefreshList());
            Messenger.Instance.Register<Message>(MessageTypes.VENDOR_INV_ITEMS_CHANGED, (msg) => RefreshList());
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
                //Recipe rec = SelectedItem.GetRecipe();
                _models.RContainer.RemoveItem(SelectedItem);
                //rec.Destroy();
                SelectedItem = null;
                RefreshList();
            }
        }

        private void EditRecipe(object obj)
        {
            //NewRecipeVM tabVM = new NewRecipeVM(SelectedItem.GetRecipe(), SaveRecipeHandler);
            NewRecipeVM tabVM = new NewRecipeVM(SelectedItem);
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
            FilteredItems = MainHelper.FilterInventoryItems(filterStr, _models.RContainer.Items);
        }

        public void RefreshList()
        {
            ChangePageState(_pageIndex);
        }

        private void SaveRecipeHandler(Recipe rec)
        {
            ChangePageState(_pageIndex);
        }

        public void RowEdited(Recipe item)
        {
            int idx = FilteredItems.IndexOf(item);
            _models.RContainer.Update(item);
            //SelectedItem.Update();
        }
        #endregion

        protected override void ChangePageState(int pageIdx)
        {
            base.ChangePageState(pageIdx);

            switch (pageIdx)
            {
                case 0:
                    FilteredItems = new ObservableCollection<Recipe>(_models.RContainer.Items.Where(x => x.IsBatch));
                    break;
                case 1:
                    FilteredItems = new ObservableCollection<Recipe>(_models.RContainer.Items.Where(x => !x.IsBatch));
                    break;
                case -1:
                    FilteredItems = new ObservableCollection<Recipe>() { new Recipe() { Name = "DB not found" } };
                    break;
            }

            FilterText = "";
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

        public CategoryProportion(string name, float cost, float total)
        {
            Name = name;
            Cost = cost;
            if (total > 0)
                CostProportion = Cost / total;
            else
                CostProportion = 0;
        }
    }
}
