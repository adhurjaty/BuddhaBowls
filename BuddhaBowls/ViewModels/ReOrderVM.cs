using BuddhaBowls.Helpers;
using BuddhaBowls.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BuddhaBowls
{
    public class ReOrderVM : WizardVM
    {
        protected InventoryItem _itemToMove;

        #region Content Binders

        private ObservableCollection<InventoryItem> _invOrderList;
        public ObservableCollection<InventoryItem> InvOrderList
        {
            get
            {
                return _invOrderList;
            }
            set
            {
                _invOrderList = value;
                NotifyPropertyChanged("InvOrderList");
            }
        }

        private InventoryItem _selectedOrderedItem;
        public InventoryItem SelectedOrderedItem
        {
            get
            {
                return _selectedOrderedItem;
            }
            set
            {
                _selectedOrderedItem = value;
                NotifyPropertyChanged("SelectedOrderedItem");
            }
        }

        #endregion

        #region ICommand and CanExecute

        public ICommand PlaceAboveCommand { get; set; }
        public ICommand PlaceBelowCommand { get; set; }
        public ICommand MoveUpCommand { get; set; }
        public ICommand MoveDownCommand { get; set; }

        #endregion

        public ReOrderVM() : base()
        {
            PlaceAboveCommand = new RelayCommand(PlaceAbove, x => SelectedOrderedItem != null && _itemToMove != null);
            PlaceBelowCommand = new RelayCommand(PlaceBelow, x => SelectedOrderedItem != null && _itemToMove != null);
            MoveUpCommand = new RelayCommand(MoveUp, x => _itemToMove != null);
            MoveDownCommand = new RelayCommand(MoveDown, x => _itemToMove != null);
        }

        #region ICommand Helpers

        protected void PlaceAbove(object obj)
        {
            int idx = InvOrderList.IndexOf(SelectedOrderedItem);
            RemoveExistingItem();
            InvOrderList.Insert(idx, _itemToMove);
        }

        protected void PlaceBelow(object obj)
        {
            RemoveExistingItem();
            int idx = InvOrderList.IndexOf(SelectedOrderedItem);
            if (idx == InvOrderList.Count - 1)
                InvOrderList.Add(_itemToMove);
            else
                InvOrderList.Insert(idx + 1, _itemToMove);
        }

        protected void MoveUp(object obj)
        {
            SelectedOrderedItem = GetItemInOrderList();
            if (SelectedOrderedItem != null)
            {
                MoveInList(SelectedOrderedItem, true);
            }
        }

        protected void MoveDown(object obj)
        {
            SelectedOrderedItem = GetItemInOrderList();
            if (SelectedOrderedItem != null)
            {
                MoveInList(SelectedOrderedItem, false);
            }
        }

        protected void MoveInList(InventoryItem item, bool up)
        {
            List<InventoryItem> newItemInList = MainHelper.MoveInList(item, up, InvOrderList.ToList());

            InvOrderList = new ObservableCollection<InventoryItem>(newItemInList);
        }

        #endregion

        #region Initializers

        #endregion

        #region UI Updaters


        private InventoryItem GetItemInOrderList()
        {
            return InvOrderList.FirstOrDefault(x => x.Id == _itemToMove.Id);
        }

        private void RemoveExistingItem()
        {
            InventoryItem existingItem = GetItemInOrderList();
            if (existingItem != null)
                InvOrderList.Remove(existingItem);
        }

        #endregion
    }
}
