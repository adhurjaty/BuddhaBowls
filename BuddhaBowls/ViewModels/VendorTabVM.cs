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

namespace BuddhaBowls.ViewModels
{
    public class VendorTabVM : INotifyPropertyChanged
    {
        private ModelContainer _models;

        // INotifyPropertyChanged event and method
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainViewModel ParentContext { get; private set; }

        #region Data Bindings
        public string FilterText { get; set; }

        private ObservableCollection<Vendor> _filteredVendorList;
        public ObservableCollection<Vendor> FilteredVendorList
        {
            get
            {
                return _filteredVendorList;
            }
            set
            {
                _filteredVendorList = value;
                NotifyPropertyChanged("FilteredVendorList");
            }
        }

        public Vendor SelectedVendor { get; set; }
        #endregion

        #region ICommand Bindings and Can Execute
        // plus button
        public ICommand AddVendorCommand { get; set; }
        // minus button
        public ICommand DeleteVendorCommand { get; set; }
        // save button
        public ICommand SaveCommand { get; set; }
        // reset button
        public ICommand ResetCommand { get; set; }

        public bool SelectedVendorCanExecute
        {
            get
            {
                return SelectedVendor != null;
            }
        }

        public bool ResetCanExecute { get; set; }
        #endregion

        public VendorTabVM(ModelContainer models, MainViewModel parent)
        {
            _models = models;
            ParentContext = parent;

            AddVendorCommand = new RelayCommand(AddVendor);
            DeleteVendorCommand = new RelayCommand(DeleteVendor, x => SelectedVendorCanExecute);
            SaveCommand = new RelayCommand(SaveVendor);
            ResetCommand = new RelayCommand(ResetVendor, x => ResetCanExecute);
        }

        #region ICommand Helpers

        private void AddVendor(object obj)
        {
            throw new NotImplementedException();
        }

        private void ResetVendor(object obj)
        {
            throw new NotImplementedException();
        }

        private void SaveVendor(object obj)
        {
            throw new NotImplementedException();
        }

        private void DeleteVendor(object obj)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Initializers

        #endregion

        #region Update UI Methods

        #endregion
    }
}
