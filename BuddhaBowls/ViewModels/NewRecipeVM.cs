using BuddhaBowls.Models;
using BuddhaBowls.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BuddhaBowls
{
    public class NewRecipeVM : INotifyPropertyChanged
    {
        private ModelContainer _models;
        // INotifyPropertyChanged event and method
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Content Binders

        public string Name { get; set; }
        public List<Recipe> RecipeItems { get; set; }
        public List<Recipe> Ingredients { get; set; }
        public float Price { get; set; }
        public string Category { get; set; }


        #endregion

        #region ICommand Properties and Can Execute

        public ICommand SaveCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        #endregion

        public NewRecipeVM(ModelContainer models)
        {
            _models = models;
        }

        #region ICommand Helpers

        #endregion

        #region Initializers

        #endregion

        #region Update UI

        #endregion
    }
}
