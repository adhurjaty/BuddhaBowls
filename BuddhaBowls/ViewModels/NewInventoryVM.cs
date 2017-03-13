﻿using BuddhaBowls.Helpers;
using BuddhaBowls.Models;
using BuddhaBowls.Services;
using BuddhaBowls.UserControls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace BuddhaBowls
{
    /// <summary>
    /// Temporary tab for creating a new inventory
    /// </summary>
    public class NewInventoryVM : TempTabVM, INotifyPropertyChanged
    {
        private Inventory _inventory;
        private InventoryListVM _invListVM;

        #region Data Bindings

        private DateTime? _inventoryDate;
        public DateTime InventoryDate
        {
            get
            {
                return _inventoryDate ?? DateTime.Now;
            }
            set
            {
                _inventoryDate = value;
                NotifyPropertyChanged("InventoryDate");
            }
        }
        
        private InventoryListControl _inventoryControl;
        public InventoryListControl InventoryControl
        {
            get
            {
                return _inventoryControl;
            }
            set
            {
                _inventoryControl = value;
                NotifyPropertyChanged("InventoryControl");
            }
        }
        #endregion

        #region ICommand Bindings and Can Execute
        // Save button in Master inventory form
        public ICommand SaveCountCommand { get; set; }
        // Reset button in Master inventory form
        public ICommand ResetCountCommand { get; set; }
        // Cancel button to close tab
        public ICommand CancelCommand { get; set; }

        public bool ChangeCountCanExecute { get; set; } = true;

        #endregion

        public NewInventoryVM() : base()
        {
            _tabControl = new NewInventory(this);
            _invListVM = new InventoryListVM(InventoryItemCountChanged);
            InventoryControl = _invListVM.TabControl;
            
            InitICommand();
        }

        public NewInventoryVM(Inventory inv) : base()
        {
            _tabControl = new NewInventory(this);
            _inventory = inv;
            _invListVM = new InventoryListVM(InventoryItemCountChanged, inv);
            InventoryControl = _invListVM.TabControl;
            InventoryDate = inv.Date;

            InitICommand();
            ((RelayCommand)SaveCountCommand).ChangeCallback(SaveOldInventory);
        }
        
        #region ICommand Helpers

        /// <summary>
        /// Resets the inventory count to the saved value before changing the datagrid
        /// </summary>
        /// <param name="obj"></param>
        private void ResetCount(object obj)
        {
            _invListVM.ResetCount();
            ChangeCountCanExecute = false;
        }

        /// <summary>
        /// Writes the Inventory items to DB as they are in the Master List datagrid
        /// </summary>
        /// <param name="obj"></param>
        private void SaveNewInventory(object obj)
        {
            _invListVM.SaveNew(InventoryDate);

            ParentContext.Refresh();
            Close();
        }

        private void SaveOldInventory(object obj)
        {
            _invListVM.SaveOld();
            Close();
        }

        private void CancelInventory(object obj)
        {
            Close();
        }

        #endregion

        #region Initializers

        private void InitICommand()
        {
            SaveCountCommand = new RelayCommand(SaveNewInventory, x => ChangeCountCanExecute);
            ResetCountCommand = new RelayCommand(ResetCount, x => ChangeCountCanExecute);
            CancelCommand = new RelayCommand(CancelInventory);
        }

        #endregion

        #region Update UI Methods

        /// <summary>
        /// Called when Master List is edited
        /// </summary>
        public void InventoryItemCountChanged()
        {
            ChangeCountCanExecute = true;
        }

        #endregion
    }
}
