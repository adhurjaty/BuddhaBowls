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
                }
            }
        }

        public VendorInventoryItem(Dictionary<Vendor, InventoryItem> vendorDict) : base()
        {
            _vendorDict = vendorDict;
            SelectedVendor = vendorDict.Keys.FirstOrDefault();
        }

        public VendorInventoryItem(Dictionary<Vendor, InventoryItem> vendorDict, InventoryItem item) : this(vendorDict)
        {
            foreach (string property in item.GetPropertiesDB())
            {
                SetProperty(property, item.GetPropertyValue(property));
            }
            Id = item.Id;
        }

        public void UpdateVendorPrice()
        {
            NotifyPropertyChanged("LastPurchasedPrice");
            SelectedVendor.UpdateInvItem(ToInventoryItem());
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

        private void UpdateVendorParams()
        {
            if (SelectedVendor != null)
            {
                InventoryItem item = _vendorDict[SelectedVendor];
                LastPurchasedPrice = item.LastPurchasedPrice;
                PurchasedUnit = item.PurchasedUnit;
                NotifyPropertyChanged("LastPurchasedPrice");
                NotifyPropertyChanged("PurchasedUnit");
            }
        }
    }
}
