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
    public class VendorInventoryItem : Model, IItem
    {

        // dictionary relating the vendor to the inventory item (differ in conversion, price, and purchased unit)
        private Dictionary<Vendor, InventoryItem> _vendorDict;

        private InventoryItem _invItem;

        public string Name
        {
            get
            {
                return _invItem.Name;
            }
            set
            {
                _invItem.Name = value;
                NotifyPropertyChanged("Name");
            }
        }

        public string Category
        {
            get
            {
                return _invItem.Category;
            }
            set
            {
                _invItem.Category = value;
                NotifyPropertyChanged("Category");
            }
        }

        public float Count
        {
            get
            {
                return _invItem.Count;
            }
            set
            {
                _invItem.Count = value;
                NotifyPropertyChanged("Count");
                NotifyPropertyChanged("PriceExtension");
                NotifyPropertyChanged("RecipeCost");
            }
        }

        public string CountUnit
        {
            get { return _invItem.CountUnit ; }
            set
            {
                _invItem.CountUnit = value;
                NotifyPropertyChanged("CountUnit");
            }
        }

        public string RecipeUnit
        {
            get { return _invItem.RecipeUnit; }
            set { _invItem.RecipeUnit = value;
                NotifyPropertyChanged("RecipeUnit");
            }
        }

        public float? RecipeUnitConversion
        {
            get { return _invItem.RecipeUnitConversion; }
            set { _invItem.RecipeUnitConversion = value;
                NotifyPropertyChanged("RecipeUnitConversion");
                NotifyPropertyChanged("RecipeCost");
            }
        }

        public float? Yield
        {
            get { return _invItem.Yield; }
            set { _invItem.Yield = value;
                NotifyPropertyChanged("Yield");
                NotifyPropertyChanged("CostPerRU");
            }
        }

        public float Conversion
        {
            get { return _invItem.Conversion; }
            set { _invItem.Conversion = value;
                NotifyPropertyChanged("Conversion");
                NotifyPropertyChanged("PriceExtension");
                NotifyPropertyChanged("CountPrice");
            }
        }

        public int? LastVendorId { get; set; }

        public float LastPurchasedPrice
        {
            get
            {
                return _invItem.LastPurchasedPrice;
            }
            set { _invItem.LastPurchasedPrice = value;
                NotifyPropertyChanged("LastPurchasedPrice");
                NotifyPropertyChanged("PriceExtension");
                NotifyPropertyChanged("CountPrice");
                NotifyPropertyChanged("PurchaseExtension");
            }
        }

        public DateTime? LastPurchasedDate
        {
            get
            {
                if (_vendorDict != null && _vendorDict.Keys.Count > 0)
                    return _vendorDict.Values.Max(x => x.LastPurchasedDate);
                return _invItem.LastPurchasedDate;
            }
            set
            {
                _invItem.LastPurchasedDate = value;
                NotifyPropertyChanged("LastPurchasedDate");
            }
        }

        public float LastOrderAmount
        {
            get
            {
                if(_vendorDict != null && _vendorDict.Keys.Count > 0)
                    return _vendorDict.Values.OrderByDescending(x => x.LastPurchasedDate).First().LastOrderAmount;
                return _invItem.LastOrderAmount;
            }
            set { _invItem.LastOrderAmount = value;
                NotifyPropertyChanged("LastOrderAmount");
                NotifyPropertyChanged("PurchaseExtension");
            }
        }

        public string PurchasedUnit
        {
            get { return _invItem.PurchasedUnit; }
            set { _invItem.PurchasedUnit = value;
                NotifyPropertyChanged("PurchasedUnit");
            }
        }

        public float PurchaseExtension
        {
            get
            {
                return _invItem.PurchaseExtension;
            }
        }

        public float PriceExtension
        {
            get
            {
                return _invItem.PriceExtension;
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
                    SelectedVendor = Vendors[0];
                return _selectedVendor;
            }
            set
            {
                _selectedVendor = value;
                if (value != null)
                {
                    _invItem = GetInvItemFromVendor(_selectedVendor);
                    NotifyAllChanges();
                }
            }
        }

        public float CostPerRU
        {
            get
            {
                return _invItem.CostPerRU;
            }
        }

        public float RecipeCost
        {
            get
            {
                return _invItem.RecipeCost;
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
            LastVendorId = v.Id;
            _vendorDict = new Dictionary<Vendor, InventoryItem>();
            if (v != null)
            {
                _vendorDict[v] = item;
                SelectedVendor = v;
            }
        }

        public VendorInventoryItem(Dictionary<Vendor, InventoryItem> vendorDict, InventoryItem backupItem = null)
        {
            _vendorDict = vendorDict;

            if (backupItem != null)
                LastVendorId = backupItem.LastVendorId;

            if (_vendorDict.Keys.Count > 0)
            {
                SelectedVendor = _vendorDict.Keys.FirstOrDefault();
                if (LastVendorId != null)
                    SelectedVendor = _vendorDict.Keys.FirstOrDefault(x => x.Id == LastVendorId);
            }
            else
            {
                _invItem = backupItem;
            }
            Id = _invItem.Id;
            LastVendorId = _invItem.LastVendorId;
        }

        public VendorInventoryItem(InventoryItem item, IEnumerable<VendorInfo> vInfo)
        {
            _invItem = item;
            Id = item.Id;
            LastVendorId = item.LastVendorId;
            _vendorDict = new Dictionary<Vendor, InventoryItem>();
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
            InventoryItem item = _invItem;
            _invItem.LastVendorId = LastVendorId;
            return item;
        }

        public void NotifyAllChanges()
        {
            foreach (string prop in GetProperties())
            {
                NotifyPropertyChanged(prop);
            }
        }

        public InventoryItem GetInvItemFromVendor(Vendor v)
        {
            Vendor matchingKey = _vendorDict.Keys.FirstOrDefault(x => x.Id == v.Id);
            if (matchingKey != null)
            {
                InventoryItem item = _vendorDict[matchingKey];
                //item.NotifyChanges();
                return item;
            }
            return null;
        }

        public void SetVendorItem(Vendor v, InventoryItem item)
        {
            if (v != null)
            {
                _vendorDict[v] = item;
                if (v.Id == SelectedVendor.Id)
                    _invItem = item;
            }
            
        }

        public void AddVendor(Vendor v, InventoryItem item)
        {
            Vendor existingVendor = Vendors.FirstOrDefault(x => x.Id == v.Id);
            if (existingVendor != null)
                _vendorDict.Remove(existingVendor);

            _vendorDict[v] = item;
            v.AddInvItem(item);
            //NotifyAllChanges();
        }

        public void DeleteVendor(Vendor vendor)
        {
            _vendorDict = _vendorDict.Where(x => x.Key.Id != vendor.Id).ToDictionary(x => x.Key, y => y.Value);
            vendor.RemoveInvItem(ToInventoryItem());
            if (SelectedVendor != null && SelectedVendor.Id == vendor.Id)
                SelectedVendor = Vendors.FirstOrDefault();
            //NotifyAllChanges();
        }

        public void SetVendorDict(Dictionary<Vendor, InventoryItem> vDict)
        {
            _vendorDict = vDict;
        }

        public override int Insert()
        {
            Id = _invItem.Insert();

            foreach (KeyValuePair<Vendor, InventoryItem> vi in _vendorDict)
            {
                vi.Value.Id = Id;
                vi.Key.Update();
            }

            return Id;
        }

        public override void Update()
        {
            //InventoryItem item = ToInventoryItem();

            //foreach (Vendor v in Vendors)
            //{
            //    v.Update();
            //}
            //if (SelectedVendor != null)
            //{
            //    SelectedVendor.Update(item);
            //    _vendorDict[SelectedVendor] = item;
            //}
            //NotifyAllChanges();
            //item.Update();
            _invItem.Update();
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

        public IItem Copy()
        {
            // copy the values of the dictionary and set as the vendor dict
            VendorInventoryItem cpy = new VendorInventoryItem(_vendorDict.ToDictionary(x => x.Key, x => (InventoryItem)x.Value.Copy()), _invItem);
            cpy.SelectedVendor = SelectedVendor;

            return cpy;
        }

        public void SetVendorDict(List<VendorInfo> vInfo)
        {
            List<Vendor> removedVendors = _vendorDict != null ? _vendorDict.Keys.Where(x => !vInfo.Select(y => y.Name).Contains(x.Name)).ToList() :
                                          new List<Vendor>();
            foreach (VendorInfo v in vInfo)
            {
                InventoryItem invItem = (InventoryItem)_invItem.Copy();
                invItem.LastPurchasedPrice = v.Price;
                invItem.Conversion = v.Conversion;
                invItem.PurchasedUnit = v.PurchasedUnit;
                invItem.Yield = Yield;
                v.Vend.Update(invItem);
                _vendorDict[v.Vend] = invItem;
            }

            foreach (Vendor remVend in removedVendors)
            {
                remVend.RemoveInvItem(_invItem);
                DeleteVendor(remVend);
            }

            //UpdateVendorParams();
        }

        public float GetCost()
        {
            return _invItem.GetCost();
        }

        public Dictionary<string, float> GetCategoryCosts()
        {
            return _invItem.GetCategoryCosts();
        }

        public float GetLastCount()
        {
            return _invItem.GetLastCount();
        }

        public float GetPrevOrderAmount()
        {
            return _invItem.GetPrevOrderAmount();
        }

        public RecipeItem ToRecipeItem()
        {
            return _invItem.ToRecipeItem();
        }
    }
}
