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
        #endregion

        #region ICommand and CanExecute Properties 
        // > Button
        public ICommand MoveToReceivedCommand { get; set; }
        // < Button
        public ICommand MoveToOpenCommand { get; set; }
        #endregion

        public ViewOrderVM(OrderTabVM parent, IEnumerable<InventoryItem> openItems = null, IEnumerable<InventoryItem> receivedItems = null)
        {
            //_models = models;
            ParentContext = parent;

            MoveToReceivedCommand = new RelayCommand(MoveToReceived, x => MoveToReceivedCanExecute);
            MoveToOpenCommand = new RelayCommand(MoveToOpen, x => MoveToOpenCanExecute);

            InitBreakdown(openItems, receivedItems);
        }

        #region ICommand Helpers
        private void MoveToReceived(object obj)
        {
            List<InventoryItem> fromList = OpenBreakdownContext.GetInventoryItems();
            List<InventoryItem> toList = ReceivedBreakdownContext.GetInventoryItems();

            InventoryItem selected = OpenBreakdownContext.SelectedItem;
            fromList.Remove(selected);
            toList.Add(selected);

            float oTotal = 0;
            OpenBreakdownContext.BreakdownList = ParentContext.GetOrderBreakdown(fromList, out oTotal);
            OpenBreakdownContext.OrderTotal = oTotal;

            ReceivedBreakdownContext.BreakdownList = ParentContext.GetOrderBreakdown(toList, out oTotal);
            ReceivedBreakdownContext.OrderTotal = oTotal;
        }

        private void MoveToOpen(object obj)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Initializers
        private void InitBreakdown(IEnumerable<InventoryItem> openItems, IEnumerable<InventoryItem> receivedItems)
        {
            float oTotal = 0;
            if (openItems != null)
            {
                OpenBreakdownContext = new OrderBreakdownVM()
                {
                    BreakdownList = ParentContext.GetOrderBreakdown(openItems, out oTotal),
                    OrderTotal = oTotal,
                    Header = "Open Ordered Items"
                };

                ReceivedBreakdownContext = new OrderBreakdownVM()
                {
                    BreakdownList = new ObservableCollection<BreakdownCategoryItem>(),
                    Header = "Received Ordered Items"
                };
            }
            else if (receivedItems != null)
            {
                ReceivedBreakdownContext = new OrderBreakdownVM()
                {
                    BreakdownList = ParentContext.GetOrderBreakdown(receivedItems, out oTotal),
                    OrderTotal = oTotal,
                    Header = "Received Ordered Items"
                };

                OpenBreakdownContext = new OrderBreakdownVM()
                {
                    BreakdownList = new ObservableCollection<BreakdownCategoryItem>(),
                    Header = "Open Ordered Items"
                };
            }
        }
        #endregion
    }
}
