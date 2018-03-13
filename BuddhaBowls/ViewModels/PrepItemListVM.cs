using BuddhaBowls.Helpers;
using BuddhaBowls.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace BuddhaBowls
{
    public partial class InventoryTabVM
    {

        #region Content Binders

        private ObservableCollection<PrepItem> _prepItemList;
        public ObservableCollection<PrepItem> PrepItemList
        {
            get
            {
                return _prepItemList;
            }
            set
            {
                _prepItemList = value;
                NotifyPropertyChanged("PrepItemList");
            }
        }

        private PrepItem _selectedPrepItem;
        public PrepItem SelectedPrepItem
        {
            get
            {
                return _selectedPrepItem;
            }
            set
            {
                _selectedPrepItem = value;
                NotifyPropertyChanged("SelectedPrepItem");
            }
        }

        #endregion

        #region ICommand and Can Execute

        public ICommand AddPrepCommand { get; set; }
        public ICommand DeletePrepCommand { get; set; }
        public ICommand EditPrepCommand { get; set; }

        #endregion

        #region ICommand Helpers

        /// <summary>
        /// Creates new tab called New Prep and populates form - Plus button
        /// </summary>
        /// <param name="obj"></param>
        private void AddNewPrepItem(object obj)
        {
            NewPrepItemVM tabVM = new NewPrepItemVM();
            tabVM.Add("New Prep");
        }

        private void EditPrepItem(object obj)
        {
            NewPrepItemVM tabVM = new NewPrepItemVM(SelectedPrepItem);
            tabVM.Add("Edit Prep");
        }

        /// <summary>
        /// Presents user with warning dialog, then removes item from DB and in-memory list - Minus button
        /// </summary>
        /// <param name="obj"></param>
        private void DeletePrepItem(object obj)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete " + SelectedPrepItem.Name + " ?", "Delete item?", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                _models.PIContainer.RemoveItem(SelectedPrepItem);
                SelectedPrepItem = null;
            }
        }

        #endregion

        public override void FilterItems(string filterStr)
        {
            PrepItemList = new ObservableCollection<PrepItem>(_models.PIContainer.Items.Where(x => x.Name.ToUpper().Contains(filterStr.ToUpper()))
                                                                          .OrderBy(x => x.Name.ToUpper().IndexOf(filterStr.ToUpper())));
        }

        public void PrepRowEdited(PrepItem item)
        {
            _models.PIContainer.Update(item);
            InvListVM.UpdateInvValue();
        }
    }
}
