using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Models
{
    public class VendorInventoryItem : InventoryItem, INotifyPropertyChanged
    {
        private Dictionary<Vendor, InventoryItem> _vendorDict;
        
        // INotifyPropertyChanged event and method
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public List<Vendor> Vendors
        {
            get
            {
                return _vendorDict.Keys.ToList();
            }
        }

        private Vendor _selectedVendor;
        public Vendor SelectedVendor
        {
            get
            {
                return _selectedVendor;
            }
            set
            {
                if (value != null)
                {
                    _selectedVendor = value;
                    NotifyPropertyChanged("SelectedVendor");
                    UpdateVendorParams();
                    UpdateProperties();
                }
            }
        }

        public VendorInventoryItem()
        {
        }

        public VendorInventoryItem(Dictionary<Vendor, InventoryItem> vendorDict, InventoryItem item)
        {
            _vendorDict = vendorDict;

            foreach (string property in item.GetPropertiesDB())
            {
                SetProperty(property, item.GetPropertyValue(property));
            }
            Id = item.Id;

            if (LastVendorId != null)
                SelectedVendor = vendorDict.Keys.FirstOrDefault(x => x.Id == LastVendorId);
            else
                SelectedVendor = vendorDict.Keys.FirstOrDefault();
        }

        public void UpdateVendorPrice()
        {
            NotifyPropertyChanged("LastPurchasedPrice");
            InventoryItem item = ToInventoryItem();
            if (SelectedVendor != null)
            {
                SelectedVendor.UpdateInvItem(item);
                _vendorDict[SelectedVendor] = item;
            }
        }

        public InventoryItem ToInventoryItem()
        {
            InventoryItem item = new InventoryItem();
            foreach(string property in item.GetPropertiesDB())
            {
                item.SetProperty(property, GetPropertyValue(property));
            }
            item.Id = Id;
            return item;
        }

        public void UpdateProperties()
        {
            NotifyPropertyChanged("PriceExtension");
            NotifyPropertyChanged("CountPrice");
        }

        private void UpdateVendorParams()
        {
            if (SelectedVendor != null)
            {
                InventoryItem item = _vendorDict[SelectedVendor];
                LastPurchasedPrice = item.LastPurchasedPrice;
                PurchasedUnit = item.PurchasedUnit;
                NotifyPropertyChanged("LastPurchasedPrice");
                NotifyPropertyChanged("PurchasedUnit");
                LastVendorId = SelectedVendor.Id;
                NotifyPropertyChanged("LastVendorId");
            }
        }
    }
}
