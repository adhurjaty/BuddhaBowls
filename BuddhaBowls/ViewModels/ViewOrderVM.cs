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
using System.Windows.Input;

namespace BuddhaBowls
{
    public class ViewOrderVM : INotifyPropertyChanged
    {
        //private MainWindow _window;
        //private ModelContainer _models;
        private PurchaseOrder _order;

        // INotifyPropertyChanged event and method
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public OrderTabVM ParentContext { get; set; }

        #region Content Binders
        private OrderBreakdownVM _openBreakdownContext;
        public OrderBreakdownVM OpenBreakdownContext
        {
            get
            {
                return _openBreakdownContext;
            }
            set
            {
                _openBreakdownContext = value;
                NotifyPropertyChanged("OpenBreakdownContext");
            }
        }

        private OrderBreakdownVM _receivedBreakdownContext;
        public OrderBreakdownVM ReceivedBreakdownContext
        {
            get
            {
                return _receivedBreakdownContext;
            }
            set
            {
                _receivedBreakdownContext = value;
                NotifyPropertyChanged("ReceivedBreakdownContext");
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

        public bool MoveToReceivedCanExecute
        {
            get
            {
                return OpenBreakdownContext.SelectedItem != null;
            }
        }

        public bool MoveToOpenCanExecute
        {
            get
            {
                return ReceivedBreakdownContext.SelectedItem != null;
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

        public ViewOrderVM(OrderTabVM parent, PurchaseOrder po)
        {
            ParentContext = parent;
            _order = po;

            MoveToReceivedCommand = new RelayCommand(MoveToReceived, x => MoveToReceivedCanExecute);
            MoveToOpenCommand = new RelayCommand(MoveToOpen, x => MoveToOpenCanExecute);
            SaveCommand = new RelayCommand(SavePartialOrder, x => SaveButtonCanExecute);
            CancelCommand = new RelayCommand(CancelView);

            InitBreakdown(po);
        }

        #region ICommand Helpers
        private void MoveToReceived(object obj)
        {
            MoveItem(OpenBreakdownContext, ReceivedBreakdownContext);
        }

        private void MoveToOpen(object obj)
        {
            MoveItem(ReceivedBreakdownContext, OpenBreakdownContext);
        }

        private void MoveItem(OrderBreakdownVM fromContext, OrderBreakdownVM toContext)
        {
            List<InventoryItem> fromList = fromContext.GetInventoryItems();
            List<InventoryItem> toList = toContext.GetInventoryItems();

            InventoryItem selected = fromContext.SelectedItem;
            fromList.Remove(selected);
            toList.Add(selected);

            float oTotal = 0;
            fromContext.BreakdownList = ParentContext.GetOrderBreakdown(fromList, out oTotal);
            fromContext.OrderTotal = oTotal;

            toContext.BreakdownList = ParentContext.GetOrderBreakdown(toList, out oTotal);
            toContext.OrderTotal = oTotal;
        }

        private void CancelView(object obj)
        {
            ParentContext.DeleteTempTab();
        }

        private void SavePartialOrder(object obj)
        {
            List<InventoryItem> openItems = OpenBreakdownContext.GetInventoryItems();
            List<InventoryItem> receivedItems = ReceivedBreakdownContext.GetInventoryItems();

            if (receivedItems.Count == 0)
            {
                _order.ReceivedDate = null;
                _order.Update();
            }
            else if (openItems.Count != 0)
            {
                _order.SplitToPartials(openItems, receivedItems);
            }

            ParentContext.LoadPreviousOrders();
            ParentContext.DeleteTempTab();
        }
        #endregion

        #region Initializers
        private void InitBreakdown(PurchaseOrder po)
        {
            List<InventoryItem>[] poItems = po.GetPOItems();
            List<InventoryItem> openItems = poItems[0];
            List<InventoryItem> receivedItems = poItems[1];

            float oTotal = 0;
            OpenBreakdownContext = new OrderBreakdownVM()
            {
                BreakdownList = ParentContext.GetOrderBreakdown(openItems, out oTotal),
                OrderTotal = oTotal,
                Header = "Open Ordered Items"
            };

            ReceivedBreakdownContext = new OrderBreakdownVM()
            {
                BreakdownList = ParentContext.GetOrderBreakdown(receivedItems, out oTotal),
                OrderTotal = oTotal,
                Header = "Received Ordered Items"
            };
        }
        #endregion
    }
}
