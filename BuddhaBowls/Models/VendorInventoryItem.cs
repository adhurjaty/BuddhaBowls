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
    public class VendorInventoryItem : InventoryItem
    {
        // dictionary relating the vendor to the inventory item (differ in conversion, price, and purchased unit)
        private Dictionary<Vendor, InventoryItem> _vendorDict;

        public List<Vendor> Vendors
        {
            get
            {
                return _vendorDict.Keys.OrderBy(x => x.Name).ToList();
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

        public VendorInventoryItem(Dictionary<Vendor, InventoryItem> vendorDict)
        {
            _vendorDict = vendorDict;

            if (LastVendorId != null)
                SelectedVendor = vendorDict.Keys.FirstOrDefault(x => x.Id == LastVendorId);
            else
                SelectedVendor = vendorDict.Keys.FirstOrDefault();

            InventoryItem item = _vendorDict[SelectedVendor];
            CopyInvItem(item);

            if(string.IsNullOrEmpty(SelectedVendor.Name))
            {
                SelectedVendor = null;
                _vendorDict = new Dictionary<Vendor, InventoryItem>();
            }
        }

        /// <summary>
        /// Writes new price to DB when user has changed the price or conversion in the datagrid
        /// </summary>
        public void UpdateVendorProps()
        {
            NotifyPropertyChanged("LastPurchasedPrice");
            NotifyPropertyChanged("Conversion");
            if (SelectedVendor != null)
            {
                InventoryItem item = ToInventoryItem();
                _vendorDict[SelectedVendor] = item;
            }
        }

        public void CopyInvItem(InventoryItem item)
        {
            foreach (string property in item.GetPropertiesDB())
            {
                SetProperty(property, item.GetPropertyValue(property));
            }
            Id = item.Id;
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

        public InventoryItem GetInvItemFromVendor(Vendor v)
        {
            if (_vendorDict.Keys.Contains(v))
            {
                InventoryItem item = _vendorDict[v];
                item.NotifyChanges();
                return item;
            }
            return null;
        }

        public void SetVendorItem(Vendor v, InventoryItem item)
        {
            if (v != null && _vendorDict.Keys.Contains(v))
            {
                SelectedVendor = v;
                _vendorDict[v] = item;
                UpdateVendorParams();
            }
            else
            {
                CopyInvItem(item);
            }
            Update();
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
            vendor.RemoveInvItem(ToInventoryItem());
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
            InventoryItem item = ToInventoryItem();

            foreach (Vendor vendor in Vendors)
            {
                vendor.Update(GetInvItemFromVendor(vendor));
            }

            item.Update();
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
            if (_vendorDict.Count > 0)
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
            }
        }
    }
}
