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
    public class VendorTabVM : TabVM
    {
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
        public string AddEditHeader { get; private set; }
        public ObservableCollection<FieldSetting> FieldsCollection { get; private set; }

        private string _addEditErrorMessage;
        public string AddEditErrorMessage
        {
            get
            {
                return _addEditErrorMessage;
            }
            set
            {
                _addEditErrorMessage = value;
                NotifyPropertyChanged("AddEditErrorMessage");
            }
        }
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
        // save button in new vendor tab
        public ICommand SaveAddEditCommand { get; set; }
        // cancel button in new vendor tab
        public ICommand CancelAddEditCommand { get; set; }
        // Change Rec List button in vendor main tab
        public ICommand ChangeRecListOrderCommand { get; set; }
        public ICommand ChangeVendorItemsCommand { get; set; }
        public ICommand GetOrderSheetCommand { get; set; }

        public bool SelectedVendorCanExecute
        {
            get
            {
                return SelectedVendor != null;
            }
        }

        public bool AlterVendorCanExecute { get; set; }

        public bool SaveAddEditCanExecute
        {
            get
            {
                return string.IsNullOrWhiteSpace(AddEditErrorMessage);
            }
        }

        public bool GenOrderSheetCanExecute
        {
            get
            {
                if(SelectedVendor != null)
                {
                    return File.Exists(SelectedVendor.GetItemsListPath());
                }

                return false;
            }
        }
        #endregion

        public VendorTabVM() : base()
        {
            AddVendorCommand = new RelayCommand(AddVendor, x => DBConnection);
            DeleteVendorCommand = new RelayCommand(DeleteVendor, x => SelectedVendorCanExecute && DBConnection);
            SaveCommand = new RelayCommand(SaveVendor, x => AlterVendorCanExecute && DBConnection);
            ResetCommand = new RelayCommand(ResetVendor, x => AlterVendorCanExecute && DBConnection);
            SaveAddEditCommand = new RelayCommand(SaveAddEdit, x => SaveAddEditCanExecute);
            CancelAddEditCommand = new RelayCommand(CancelAddEdit);
            //ChangeRecListOrderCommand = new RelayCommand(ChangeRecOrder, x => SelectedVendorCanExecute);
            ChangeVendorItemsCommand = new RelayCommand(ChangeVendorItems, x => SelectedVendorCanExecute && DBConnection);
            GetOrderSheetCommand = new RelayCommand(GenerateOrderSheet, x => GenOrderSheetCanExecute && DBConnection);

            InitVendors();
        }

        #region ICommand Helpers

        private void AddVendor(object obj)
        {
            throw new NotImplementedException();
            //AddEditHeader = "Add New Vendor";
            //FieldsCollection = ParentContext.GetFieldsAndValues<Vendor>();

            //ParentContext.AddTempTab("Add Vendor", new EditItem(this, ClearErrors));
        }

        private void ResetVendor(object obj)
        {
            foreach (Vendor vendor in FilteredVendorList)
            {
                vendor.Reset();
            }

            RefreshVendorList();
            AlterVendorCanExecute = false;
        }

        private void SaveVendor(object obj)
        {
            foreach(Vendor v in FilteredVendorList)
            {
                v.Update();
            }
            RefreshVendorList();
            AlterVendorCanExecute = false;
        }

        private void DeleteVendor(object obj)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete " + SelectedVendor.Name,
                                                      "Delete " + SelectedVendor.Name + "?", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                SelectedVendor.Destroy();
                _models.Vendors.Remove(SelectedVendor);
                SelectedVendor = null;
                RefreshVendorList();
            }
        }

        private void CancelAddEdit(object obj)
        {
            throw new NotImplementedException();
            //AddEditErrorMessage = "";
            //ParentContext.DeleteTempTab();
        }

        private void SaveAddEdit(object obj)
        {
            throw new NotImplementedException();
            //Vendor vendor = new Vendor();

            //AddEditErrorMessage = ParentContext.ObjectFromFields(ref vendor, FieldsCollection, true);
            //if(string.IsNullOrEmpty(AddEditErrorMessage))
            //{
            //    vendor.Insert();
            //    _models.Vendors.Add(vendor);
            //    RefreshVendorList();
            //    ParentContext.DeleteTempTab();
            //}
        }

        //private void ChangeRecOrder(object obj)
        //{
        //    ParentContext.AddTempTab("Rec List Order", new ChangeInventoryOrder(new ChangeOrderVM(SelectedVendor)));
        //}

        private void ChangeVendorItems(object obj)
        {
            SetVendorItemsVM tabVM = new SetVendorItemsVM(SelectedVendor);
            tabVM.Add("Vendor Items");
        }

        private void GenerateOrderSheet(object obj)
        {
            if(File.Exists(SelectedVendor.GetItemsListPath()))
            {
                new Thread(delegate ()
                {
                    System.Diagnostics.Process.Start(SelectedVendor.GetItemsListPath());
                }).Start();
            }
            else
            {
                ParentContext.GenerateVendorOrderList(SelectedVendor);
            }
        }

        #endregion

        #region Initializers

        private void InitVendors()
        {
            if (DBConnection)
                RefreshVendorList();
            else
                FilteredVendorList = new ObservableCollection<Vendor>() { new Vendor() { Name = "Could not connect to DB" } };
        }

        #endregion

        #region Update UI Methods
        public void RefreshVendorList()
        {
            FilteredVendorList = new ObservableCollection<Vendor>(_models.Vendors.OrderBy(x => x.Name));
        }

        public void FilterVendors(string filterStr)
        {
            if (string.IsNullOrWhiteSpace(filterStr))
                RefreshVendorList();
            else
                FilteredVendorList = new ObservableCollection<Vendor>(_models.Vendors
                                                        .Where(x => x.Name.ToUpper().Contains(filterStr.ToUpper()))
                                                        .OrderBy(x => x.Name.ToUpper().IndexOf(filterStr.ToUpper())));
        }

        public void ClearErrors()
        {
            AddEditErrorMessage = "";
        }
        #endregion
    }
}
