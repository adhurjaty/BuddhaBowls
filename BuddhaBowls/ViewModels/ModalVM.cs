using BuddhaBowls.Models;
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
    public delegate void ModalOkDel(Vendor item);

    public class ModalVM : INotifyPropertyChanged
    {
        public ModalOkDel OkProc;
        // INotifyPropertyChanged event and method
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Content Binders

        private string _modalTitle;
        public string ModalTitle
        {
            get
            {
                return _modalTitle;
            }
            set
            {
                _modalTitle = value;
                NotifyPropertyChanged("ModalTitle");
            }
        }

        private List<Vendor> _remainingItems;
        public List<Vendor> RemainingItems
        {
            get
            {
                return _remainingItems;
            }
            set
            {
                _remainingItems = value;
                NotifyPropertyChanged("RemainingItems");
            }
        }

        public Vendor ItemToAdd { get; set; }

        private Visibility _modalVisibility = Visibility.Hidden;
        public Visibility ModalVisibility
        {
            get
            {
                return _modalVisibility;
            }
            set
            {
                _modalVisibility = value;
                NotifyPropertyChanged("ModalVisibility");
            }
        }
        #endregion

        #region ICommand and CanExecute

        public ICommand ModalOkCommand { get; set; }
        public ICommand ModalCancelCommand { get; set; }

        public bool ModalOkCanExecute
        {
            get
            {
                return ItemToAdd != null;
            }
        }

        #endregion

        public ModalVM(string header, List<Vendor> remainingItems, ModalOkDel okCallback)
        {
            ModalTitle = header;
            RemainingItems = remainingItems;
            OkProc = okCallback;
            ModalVisibility = Visibility.Visible;

            ModalOkCommand = new RelayCommand(ModalOk, x => ModalOkCanExecute);
            ModalCancelCommand = new RelayCommand(ModalCancel);
        }

        #region ICommand Helpers

        private void ModalOk(object obj)
        {
            OkProc(ItemToAdd);
            ModalVisibility = Visibility.Hidden;
        }

        private void ModalCancel(object obj)
        {
            ModalVisibility = Visibility.Hidden;
        }

        #endregion
    }
}
