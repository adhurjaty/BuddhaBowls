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
    public delegate Dictionary<Vendor, InventoryItem> VendorDictDel(InventoryItem item);

    public class VendorInventoryItem : InventoryItem
    {
        // dictionary relating the vendor to the inventory item (differ in conversion, price, and purchased unit)
        private Dictionary<Vendor, InventoryItem> _vendorDict;

        public static VendorDictDel GetItemVendorDict;

        private InventoryItem _invItem;
        public InventoryItem InvItem
        {
            get
            {
                return _invItem;
            }
            set
            {
                _invItem = value;
                Name = _invItem.Name;
                Category = _invItem.Category;
                Count = _invItem.Count;
                CountUnit = _invItem.CountUnit;
                RecipeUnit = _invItem.RecipeUnit;
                RecipeUnitConversion = _invItem.RecipeUnitConversion;
                Yield = _invItem.Yield;
                LastVendorId = _invItem.LastVendorId;
                Conversion = _invItem.Conversion;
                LastPurchasedPrice = _invItem.LastPurchasedPrice;
                LastOrderAmount = _invItem.LastOrderAmount;
                PurchasedUnit = _invItem.PurchasedUnit;
                Id = _invItem.Id;
                NotifyAllChanges();
            }
        }

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
                    UpdateVendorParams();
                }
            }
        }

        public VendorInventoryItem()
        {
        }

        /// <summary>
        /// Constructor used to load old inventories - does not allow user to switch vendors
        /// </summary>
        /// <param name="item"></param>
        /// <param name="v"></param>
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

        public VendorInventoryItem(InventoryItem item)
        {
            InvItem = item;
            _vendorDict = GetItemVendorDict(item);

            if (LastVendorId != null)
                SelectedVendor = _vendorDict.Keys.FirstOrDefault(x => x.Id == LastVendorId);
            else
                SelectedVendor = _vendorDict.Keys.FirstOrDefault();

            //CopyInvItem(item);
        }

        /// <summary>
        /// Writes new price to DB when user has changed the price or conversion in the datagrid
        /// </summary>
        //public void UpdateVendorProps()
        //{
        //    NotifyPropertyChanged("LastPurchasedPrice");
        //    NotifyPropertyChanged("Conversion");
        //    if (SelectedVendor != null)
        //    {
        //        InventoryItem item = ToInventoryItem();
        //        _vendorDict[SelectedVendor] = item;
        //    }
        //}

        //public void CopyInvItem(InventoryItem item)
        //{
        //    foreach (string property in item.GetPropertiesDB())
        //    {
        //        SetProperty(property, item.GetPropertyValue(property));
        //    }
        //    Id = item.Id;
        //}

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
                _vendorDict[v] = item;
                UpdateVendorParams();
                //v.Update(item);
            }
            else
            {
                _invItem = item;
            }
            //Update();
        }

        public void NotifyAllChanges()
        {
            NotifyPropertyChanged("SelectedVendor");
            NotifyPropertyChanged("PriceExtension");
            NotifyPropertyChanged("CountPrice");
            NotifyPropertyChanged("Vendors");
            NotifyPropertyChanged("Name");
            NotifyPropertyChanged("Count");
            NotifyPropertyChanged("CountUnit");
            NotifyPropertyChanged("RecipeUnit");
            NotifyPropertyChanged("RecipeUnitConversion");
            NotifyPropertyChanged("Yield");
            NotifyPropertyChanged("LastPurchasedPrice");
            NotifyPropertyChanged("PurchasedUnit");
            NotifyPropertyChanged("Conversion");
        }

        /// <summary>
        /// Displays different property values to datagrid when the user changes the vendor on the datagrid
        /// </summary>
        private void UpdateVendorParams()
        {
            if (SelectedVendor != null)
            {
                InventoryItem item = _vendorDict[_vendorDict.Keys.First(x => x.Id == SelectedVendor.Id)];
                LastPurchasedPrice = item.LastPurchasedPrice;
                Conversion = item.Conversion;
                PurchasedUnit = item.PurchasedUnit;
                LastVendorId = SelectedVendor.Id;
            }

            NotifyAllChanges();
        }

        public void AddVendor(Vendor v, InventoryItem item)
        {
            Vendor existingVendor = Vendors.FirstOrDefault(x => x.Id == v.Id);
            if (existingVendor != null)
            {
                _vendorDict.Remove(existingVendor);
            }
            _vendorDict[v] = item;
            NotifyAllChanges();
        }

        public void DeleteVendor(Vendor vendor)
        {
            _vendorDict.Remove(vendor);
            vendor.RemoveInvItem(ToInventoryItem());
            NotifyAllChanges();
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

            if (SelectedVendor != null)
            {
                SelectedVendor.Update(item);
                _vendorDict[SelectedVendor] = item;
            }
            NotifyAllChanges();
            item.Update();
        }

        public void Update(List<VendorInfo> vInfoList)
        {
            InventoryItem invItem = ToInventoryItem();
            List<Vendor> removedVendors = _vendorDict.Keys.Where(x => !vInfoList.Select(y => y.Name).Contains(x.Name)).ToList();
            foreach (VendorInfo v in vInfoList)
            {
                invItem.LastPurchasedPrice = v.Price;
                invItem.Conversion = v.Conversion;
                invItem.PurchasedUnit = v.PurchasedUnit;
                invItem.Yield = Yield;
                v.Vend.Update(invItem);
                _vendorDict[v.Vend] = invItem;
            }

            foreach (Vendor remVend in removedVendors)
            {
                remVend.RemoveInvItem(invItem);
                DeleteVendor(remVend);
            }

            UpdateVendorParams();
            Update();
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
    }
}
