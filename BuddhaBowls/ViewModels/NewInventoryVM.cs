using BuddhaBowls.Helpers;
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
        #region Data Bindings

        private Inventory _inventory;
        public Inventory Inv
        {
            get
            {
                return _inventory;
            }
            set
            {
                _inventory = value;
                NotifyPropertyChanged("Inventory");
            }
        }

        private DateTime _invDate;
        public DateTime InvDate
        {
            get
            {
                return _invDate;
            }
            set
            {
                _invDate = value;
                NotifyPropertyChanged("InvDate");
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

        public InventoryListVM InvListVM { get; set; }

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

        /// <summary>
        /// Constructor for a new inventory
        /// </summary>
        public NewInventoryVM() : base()
        {
            _tabControl = new NewInventory(this);
            InvListVM = new InventoryListVM(InventoryItemCountChanged);
            InventoryControl = InvListVM.TabControl;
            Inv = new Inventory(DateTime.Now);
            InvDate = Inv.Date;
            
            InitICommand();
            Header = "New Inventory";
        }

        /// <summary>
        /// Constructor for viewing existing inventory
        /// </summary>
        /// <param name="inv"></param>
        public NewInventoryVM(Inventory inv) : base()
        {
            _tabControl = new NewInventory(this);
            Inv = inv;
            InvListVM = new InventoryListVM(inv, InventoryItemCountChanged);
            InventoryControl = InvListVM.TabControl;
            InvDate = Inv.Date;

            InitICommand();
            Header = "Edit Inventory " + Inv.Date.ToShortDateString();
        }

        #region ICommand Helpers

        /// <summary>
        /// Resets the inventory count to the saved value before changing the datagrid
        /// </summary>
        /// <param name="obj"></param>
        private void ResetCount(object obj)
        {
            InvListVM.ResetCount();
            ChangeCountCanExecute = false;
        }

        /// <summary>
        /// Writes the Inventory items to DB as they are in the Master List datagrid
        /// </summary>
        /// <param name="obj"></param>
        private void SaveInventory(object obj)
        {
            if (CheckUniqueInvDate())
            {
                // close at top so that the copy gets removed and there are fewer updates to do
                Close();

                Inv.Date = InvDate;
                Inv.DestroyTable();

                Inv.SetInvItemsContainer(InvListVM.GetItemsContainer().ToInvContainer());
                Inv = _models.InContainer.AddItem(Inv);

                if (Inv.Id == -1)
                    Inv.Insert();
                else
                    Inv.Update();

                // if this is the latest date inventory, change item counts for current inv items
                if (Inv.Date >= _models.InContainer.Items.Max(x => x.Date))
                {
                    _models.VIContainer.SetItems(InvListVM.GetItemsContainer().Items);
                    _models.VIContainer.UpdateContainer();
                }
            }
        }

        private void CancelInventory(object obj)
        {
            Close();
        }

        private bool CheckUniqueInvDate()
        {
            Inventory existingInv = _models.InContainer.Items.FirstOrDefault(x => x.Date.Date == Inv.Date.Date);
            if(existingInv != null && existingInv.Id != Inv.Id)
            {
                if (MessageBox.Show("Inventory with that date already exists. Do you wish to replace it?", "Replace Inventory",
                    MessageBoxButton.YesNo) == MessageBoxResult.No)
                    return false;
            }

            return true;
        }
        #endregion

        #region Initializers

        private void InitICommand()
        {
            SaveCountCommand = new RelayCommand(SaveInventory, x => ChangeCountCanExecute);
            ResetCountCommand = new RelayCommand(ResetCount, x => ChangeCountCanExecute);
            CancelCommand = new RelayCommand(CancelInventory);
        }

        #endregion

        #region Update UI Methods

        /// <summary>
        /// Enables reset and save buttons
        /// </summary>
        public void InventoryItemCountChanged()
        {
            ChangeCountCanExecute = true;
        }

        public override void Refresh()
        {
            InvListVM.InitContainer();
        }

        protected override void Close()
        {
            _models.VIContainer.RemoveCopy(InvListVM.GetItemsContainer());
            base.Close();
        }
        #endregion
    }
}
