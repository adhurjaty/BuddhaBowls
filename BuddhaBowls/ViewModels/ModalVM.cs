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
    public delegate void ModalOkDel<T>(T item);

    public class ModalVM<T> : INotifyPropertyChanged where T : Model
    {
        public ModalOkDel<T> OkProc;
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

        private List<T> _remainingItems;
        public List<T> RemainingItems
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

        public T ItemToAdd { get; set; }

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
        public ModalVM(string header, List<T> remainingItems, ModalOkDel<T> okCallback)
        {
            ModalTitle = header;
            OkProc = okCallback;
            ModalVisibility = Visibility.Visible;

            RemainingItems = remainingItems;

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
