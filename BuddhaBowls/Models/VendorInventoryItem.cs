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
                if (_selectedVendor == null && _vendorDict != null && Vendors.Count > 0)
                    _selectedVendor = Vendors[0];
                return _selectedVendor;
            }
            set
            {
                _selectedVendor = value;
                if (value != null)
                {
                    UpdateVendorParams();
                }
            }
        }

        public VendorInventoryItem()
        {
            _vendorDict = new Dictionary<Vendor, InventoryItem>();
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

        public VendorInventoryItem(InventoryItem item, Dictionary<Vendor, InventoryItem> vendorDict = null)
        {
            InvItem = item;
            _vendorDict = vendorDict ?? new Dictionary<Vendor, InventoryItem>();

            if (LastVendorId != null)
                SelectedVendor = _vendorDict.Keys.FirstOrDefault(x => x.Id == LastVendorId);
            else
                SelectedVendor = _vendorDict.Keys.FirstOrDefault();

            //CopyInvItem(item);
        }

        public VendorInventoryItem(InventoryItem item, IEnumerable<VendorInfo> vInfo) : this(item)
        {
            SetVendorDict(vInfo.ToList());

            if (LastVendorId != null)
                SelectedVendor = _vendorDict.Keys.FirstOrDefault(x => x.Id == LastVendorId);
            else
                SelectedVendor = _vendorDict.Keys.FirstOrDefault();
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
            if (v != null)
            {
                _vendorDict[v] = item;
                UpdateVendorParams();
            }
            if(v.Id == SelectedVendor.Id)
                _invItem = item;
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
            NotifyPropertyChanged("LastOrderAmount");
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
                InventoryItem item = _vendorDict.First(x => x.Key.Id == SelectedVendor.Id).Value;
                LastPurchasedPrice = item.LastPurchasedPrice;
                LastOrderAmount = item.LastOrderAmount;
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
            _vendorDict = _vendorDict.Where(x => x.Key.Id != vendor.Id).ToDictionary(x => x.Key, y => y.Value);
            vendor.RemoveInvItem(ToInventoryItem());
            if (SelectedVendor != null && SelectedVendor.Id == vendor.Id)
                SelectedVendor = Vendors.FirstOrDefault();
            NotifyAllChanges();
        }

        public void SetVendorDict(Dictionary<Vendor, InventoryItem> vDict)
        {
            _vendorDict = vDict;
        }

        public override int Insert()
        {
            Id = ToInventoryItem().Insert();

            foreach (KeyValuePair<Vendor, InventoryItem> vi in _vendorDict)
            {
                vi.Value.Id = Id;
                vi.Key.Update();
            }

            return Id;
        }

        public override void Update()
        {
            InventoryItem item = ToInventoryItem();

            foreach (Vendor v in Vendors)
            {
                v.Update();
            }
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
            SetVendorDict(vInfoList);
            Update();
        }

        public override void Destroy()
        {
            InventoryItem invItem = ToInventoryItem();
            foreach (Vendor vend in Vendors)
            {
                vend.RemoveInvItem(invItem);
            }
            invItem.Destroy();
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

        public void SetVendorDict(List<VendorInfo> vInfo)
        {
            InventoryItem invItem = ToInventoryItem();
            List<Vendor> removedVendors = _vendorDict != null ? _vendorDict.Keys.Where(x => !vInfo.Select(y => y.Name).Contains(x.Name)).ToList() :
                                          new List<Vendor>();
            foreach (VendorInfo v in vInfo)
            {
                invItem = ToInventoryItem();
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
        }
    }
}
