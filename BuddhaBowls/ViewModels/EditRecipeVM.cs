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

namespace BuddhaBowls
{
    public class EditRecipeVM : NewRecipeVM
    {
        private Recipe _editItem;

        #region Content Binders

        #endregion

        #region ICommand Properties and Can Execute

        #endregion

        public EditRecipeVM(bool isBatch, Recipe editItem) : base(isBatch)
        {
            _editItem = editItem;
            _availableItems = editItem.ItemList;
            _tabControl = new NewRecipe(this);

            Ingredients = new ObservableCollection<IItem>(editItem.ItemList);
            Header = "Edit " + editItem.Name;

            ((RelayCommand)SaveCommand).ChangeCallback(Save);

            InitFieldsCollection(editItem);
            Refresh();
        }

        #region ICommand Helpers

        private void Save(object obj)
        {
            ErrorMessage = ParentContext.ObjectFromFields(ref _editItem, FieldsCollection, true);
            _editItem.ItemList = Ingredients.ToList();

            if (string.IsNullOrEmpty(ErrorMessage))
            {
                _editItem.Update();
                ParentContext.RecipeTab.RefreshList();
                Close();
            }
        }

        #endregion

        #region Initializers

        #endregion

        #region Update UI

        #endregion
    }
}
