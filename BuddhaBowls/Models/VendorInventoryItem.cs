using BuddhaBowls.Helpers;
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
        // dictionary relating the vendor to the inventory item (differ in conversion, price, and purchased unit)
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

        /// <summary>
        /// Writes new price to DB when user has changed the price in the datagrid
        /// </summary>
        public void UpdateVendorPrice()
        {
            NotifyPropertyChanged("LastPurchasedPrice");
            InventoryItem item = ToInventoryItem();
            if (SelectedVendor != null)
            {
                SelectedVendor.Update(item);
                _vendorDict[SelectedVendor] = item;
            }
        }

        /// <summary>
        /// Convert to a plain inventory item
        /// </summary>
        /// <returns></returns>
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

        public override int Insert()
        {
            return ToInventoryItem().Insert();
        }

        public override void Update()
        {
            ToInventoryItem().Update();
        }

        public override void Destroy()
        {
            ToInventoryItem().Destroy();
        }

        public override string[] GetPropertiesDB(string[] omit = null)
        {
            return ToInventoryItem().GetPropertiesDB();
        }

        /// <summary>
        /// Displays different property values to datagrid when the user changes the vendor on the datagrid
        /// </summary>
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
            }
        }
    }
}
