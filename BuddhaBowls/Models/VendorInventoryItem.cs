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

        public VendorInventoryItem(InventoryItem item, Vendor v)
        {
            foreach (string property in item.GetPropertiesDB())
            {
                SetProperty(property, item.GetPropertyValue(property));
            }
            Id = item.Id;
            _vendorDict = new Dictionary<Vendor, InventoryItem>();
            if (v != null)
            {
                _vendorDict[v] = item;
                SelectedVendor = v;
            }
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
        /// Writes new price to DB when user has changed the price or conversion in the datagrid
        /// </summary>
        public void UpdateVendorProps()
        {
            NotifyPropertyChanged("LastPurchasedPrice");
            NotifyPropertyChanged("Conversion");
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

        public void AddVendor(Vendor v, InventoryItem item)
        {
            Vendor existingVendor = Vendors.FirstOrDefault(x => x.Id == v.Id);
            if (existingVendor != null)
            {
                _vendorDict.Remove(existingVendor);
            }
            _vendorDict[v] = item;
            NotifyPropertyChanged("Vendors");
            UpdateProperties();
        }

        public void DeleteVendor(Vendor vendor)
        {
            _vendorDict.Remove(vendor);
            NotifyPropertyChanged("Vendors");
            UpdateProperties();
        }

        public void SetVendorDict(Dictionary<Vendor, InventoryItem> vDict)
        {
            _vendorDict = vDict;
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

        public new VendorInventoryItem Copy()
        {
            VendorInventoryItem cpy = base.Copy<VendorInventoryItem>();
            cpy.SetVendorDict(_vendorDict);
            cpy.SelectedVendor = SelectedVendor;
            return cpy;
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
                Conversion = item.Conversion;
                PurchasedUnit = item.PurchasedUnit;
                NotifyPropertyChanged("LastPurchasedPrice");
                NotifyPropertyChanged("PurchasedUnit");
                NotifyPropertyChanged("Conversion");
                LastVendorId = SelectedVendor.Id;
                Update();
            }
        }

    }
}
