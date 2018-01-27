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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace BuddhaBowls
{
    /// <summary>
    /// Temp tab to view or edit an open or received order - opened from the Orders tab
    /// </summary>
    public class ViewOrderVM : TempTabVM
    {
        private PurchaseOrder _order;
        private InventoryItemsContainer _invItemsContainer;
        private Vendor _vendor;

        #region Content Binders
        private OrderBreakdownVM _breakdownContext;
        public OrderBreakdownVM BreakdownContext
        {
            get
            {
                return _breakdownContext;
            }
            set
            {
                _breakdownContext = value;
                NotifyPropertyChanged("BreakdownContext");
            }
        }

        #endregion

        #region ICommand and CanExecute Properties 
        // > Button
        public ICommand MoveToReceivedCommand { get; set; }
        // < Button
        public ICommand MoveToOpenCommand { get; set; }
        // Save Button
        public ICommand SaveCommand { get; set; }
        // Cancel Button
        public ICommand CancelCommand { get; set; }
        // Adds item to PO
        public ICommand AddNewItemCommand { get; set; }
        // Deletes item from PO
        public ICommand DeleteItemCommand { get; set; }

        public bool MoveToReceivedCanExecute
        {
            get
            {
                return BreakdownContext.SelectedItem != null;
            }
        }

        public bool MoveToOpenCanExecute
        {
            get
            {
                return true; //ReceivedBreakdownContext.SelectedItem != null;
            }
        }

        public bool SaveButtonCanExecute
        {
            get
            {
                return true;
            }
        }
        #endregion

        public ViewOrderVM(PurchaseOrder po)
        {
            _order = po;
            _vendor = _models.VContainer.Items.First(x => x.Name == _order.VendorName); _tabControl = new ViewOrderTabControl(this);
            Header = "View " + po.VendorName + " Order";
            //RefreshOrders = refresh;

            SaveCommand = new RelayCommand(SaveEdits, x => SaveButtonCanExecute);
            CancelCommand = new RelayCommand(CancelView);
            AddNewItemCommand = new RelayCommand(AddPOItem);
            DeleteItemCommand = new RelayCommand(DeletePOItem);

            _invItemsContainer = po.GetItemsContainer().Copy();
            //_displayedItems = po.ItemList;
            //_editedItems = new List<InventoryItem>();
            UpdateBreakdown();
        }

        #region ICommand Helpers

        private void CancelView(object obj)
        {
            _order.GetItemsContainer().RemoveCopy(_invItemsContainer);
            Close();
        }

        private void SaveEdits(object obj)
        {
            _order.GetItemsContainer().SyncCopy(_invItemsContainer);
            _order.Update();
            UpdateLatestVendorOrder();
            OverwriteExcelPO(_order);

            _models.VIContainer.UpdateVendorItems(_vendor, _order.ItemList);
            _models.VIContainer.UpdateMasterItemOrderAdded(_order);
            Messenger.Instance.NotifyColleagues(MessageTypes.PO_CHANGED);

            Close();
        }

        private void AddPOItem(object obj)
        {
            List<VendorInventoryItem> vendorItemIds = _models.VIContainer.Items.Where(x => x.Vendors.Select(y => y.Name)
                                                                                            .Contains(_order.VendorName)).ToList();
            List<InventoryItem> remainingItems = vendorItemIds.Where(x => !_invItemsContainer.Items.Select(y => y.Id).Contains(x.Id))
                                                                          .Select(x => x.ToInventoryItem()).ToList();
            ModalVM<InventoryItem> modal = new ModalVM<InventoryItem>("Add Inv Item", remainingItems, AddInvItemToVendor);
            ParentContext.ModalContext = modal;
        }

        private void DeletePOItem(object obj)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to remove " + BreakdownContext.SelectedItem.Name + " from PO?",
                                                      "Remove " + BreakdownContext.SelectedItem.Name + "?", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                _invItemsContainer.Items.Remove(BreakdownContext.SelectedItem);
                UpdateBreakdown();
            }
        }
        #endregion

        #region Initializers
        private void UpdateBreakdown()
        {
            // check where duplicates are being formed. Remove this if I find the source
            //_displayedItems = _order.RemoveViewingDuplicates(_displayedItems);

            string header = _order.Received ? "Received Ordered Items" : "Open Ordered Items";

            BreakdownContext = new OrderBreakdownVM(_invItemsContainer.Items, header, false);
        }
        #endregion

        private void OrderEdited(InventoryItem item)
        {
            BreakdownContext.UpdateDisplay();

            // check whether the item being edited is the latest order from this vendor. If so, then change the properties of the current
            // inventory item (last order price, last order qty...)
            
        }

        /// <summary>
        /// check whether the item being edited is the latest order from this vendor. If so, then change the properties of the current
        /// inventory item (last order price, last order qty...)
        /// </summary>
        private void UpdateLatestVendorOrder()
        {
            
            foreach (InventoryItem item in _invItemsContainer.Items)
            {
                VendorInventoryItem vItem = _models.VIContainer.Items.FirstOrDefault(x => x.Id == item.Id);
                if (_order.OrderDate > item.LastPurchasedDate && vItem != null)
                {
                    vItem.SetVendorItem(_vendor, item);
                    item.LastVendorId = _vendor.Id;
                    item.Update();
                }
            }
            Messenger.Instance.NotifyColleagues(MessageTypes.VENDOR_INV_ITEMS_CHANGED);
        }

        /// <summary>
        /// Adds item to displayed list
        /// </summary>
        /// <param name="item"></param>
        private void AddInvItemToVendor(InventoryItem item)
        {
            item.LastOrderAmount = 1;
            _invItemsContainer.AddItem(item);
            UpdateBreakdown();
        }

        private void OverwriteExcelPO(PurchaseOrder po)
        {
            new Thread(delegate ()
            {
                ReportGenerator generator = new ReportGenerator(_models);
                string xlsPath = generator.GenerateOrder(po, _vendor);
                generator.GenerateReceivingList(po, _vendor);
                generator.Close();
            }).Start();
        }
    }
}
