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
    public class InventoryItem : Model, IItem
    {
        private string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                NotifyPropertyChanged("Name");
            }
        }

        private string _category;
        public string Category
        {
            get
            {
                return _category;
            }
            set
            {
                _category = value;
                NotifyPropertyChanged("Category");
            }
        }

        private string _purchasedUnit;
        public string PurchasedUnit
        {
            get
            {
                return _purchasedUnit;
            }
            set
            {
                _purchasedUnit = value;
                NotifyPropertyChanged("PurchasedUnit");
            }
        }

        private float _count;
        public float Count
        {
            get
            {
                return _count;
            }
            set
            {
                _count = value;
                NotifyPropertyChanged("Count");
                NotifyPropertyChanged("PriceExtension");
                NotifyPropertyChanged("RecipeCost");
            }
        }

        private string _countUnit;
        public string CountUnit
        {
            get
            {
                return _countUnit;
            }
            set
            {
                _countUnit = value;
                NotifyPropertyChanged("CountUnit");
            }
        }

        private float _conversion;
        public float Conversion
        {
            get
            {
                return _conversion;
            }
            set
            {
                _conversion = value;
                NotifyPropertyChanged("Conversion");
                NotifyPropertyChanged("PriceExtension");
                NotifyPropertyChanged("CountPrice");
            }
        }

        private string _recipeUnit;
        public string RecipeUnit
        {
            get
            {
                return _recipeUnit;
            }
            set
            {
                _recipeUnit = value;
                NotifyPropertyChanged("RecipeUnit");
            }
        }

        private float? _recipeUnitConversion;
        public float? RecipeUnitConversion
        {
            get
            {
                return _recipeUnitConversion;
            }
            set
            {
                _recipeUnitConversion = value;
                NotifyPropertyChanged("RecipeUnitConversion");
                NotifyPropertyChanged("RecipeCost");
            }
        }

        private float? _yield;
        public float? Yield
        {
            get
            {
                return _yield;
            }
            set
            {
                _yield = value;
                NotifyPropertyChanged("Yield");
                NotifyPropertyChanged("CostPerRU");
            }
        }

        private float _lastPurchasedPrice;
        public float LastPurchasedPrice
        {
            get
            {
                return _lastPurchasedPrice;
            }
            set
            {
                _lastPurchasedPrice = value;
                NotifyPropertyChanged("LastPurchasedPrice");
                NotifyPropertyChanged("PriceExtension");
                NotifyPropertyChanged("CountPrice");
                NotifyPropertyChanged("PurchaseExtension");
            }
        }

        private float _lastOrderAmount;
        public float LastOrderAmount
        {
            get
            {
                return _lastOrderAmount;
            }
            set
            {
                _lastOrderAmount = value;
                NotifyPropertyChanged("LastOrderAmount");
                NotifyPropertyChanged("PurchaseExtension");
            }
        }

        private DateTime? _lastPurchasedDate;
        public DateTime? LastPurchasedDate
        {
            get
            {
                return _lastPurchasedDate;
            }
            set
            {
                _lastPurchasedDate = value;
                NotifyPropertyChanged("LastPurchasedDate");
            }
        }

        private int? _lastVendorId;
        public int? LastVendorId
        {
            get
            {
                return _lastVendorId;
            }
            set
            {
                _lastVendorId = value;
                NotifyPropertyChanged("LastVendorId");
            }
        }

        private string _measure;
        public string Measure
        {
            get
            {
                return _measure;
            }
            set
            {
                _measure = value;
                NotifyPropertyChanged("Measure");
            }
        }

        /// <summary>
        /// Total price of inventory on-hand
        /// </summary>
        public float PriceExtension
        {
            get
            {
                return Count * CountPrice;
            }
        }

        /// <summary>
        /// Price per Count Unit
        /// </summary>
        public float CountPrice
        {
            get
            {
                if (Conversion == 0)
                    return 0;
                return LastPurchasedPrice / Conversion;
            }
        }

        /// <summary>
        /// Total cost of a purchase of this item
        /// </summary>
        public float PurchaseExtension
        {
            get
            {
                return LastPurchasedPrice * LastOrderAmount;
            }
        }

        public float RecipeCost
        {
            get
            {
                return CostPerRU * Count;
            }
        }

        public float CostPerRU
        {
            get
            {
                if (RecipeUnitConversion == null || RecipeUnitConversion == 0)
                    return 0;
                return (float)(CountPrice / (RecipeUnitConversion * Yield));
            }
        }

        public InventoryItem() : base()
        {
            _tableName = "InventoryItem";
        }

        public InventoryItem(Dictionary<string, string> searchParams) : this()
        {
            string[] record = _dbInt.GetRecord(_tableName, searchParams);

            if(record != null)
            {
                InitializeObject(record);
            }
        }

        /// <summary>
        /// Get the recipe cost for this item
        /// </summary>
        /// <returns></returns>
        public float GetCost()
        {
            if (RecipeUnitConversion == null || RecipeUnitConversion == 0 || Yield == null || Yield == 0)
                return 0;
            return CountPrice / ((float)RecipeUnitConversion * (float)Yield) * Count;
        }

        /// <summary>
        /// Get count stored in the DB
        /// </summary>
        /// <returns></returns>
        public float GetLastCount()
        {
            return new InventoryItem(new Dictionary<string, string>() { { "Id", Id.ToString() } }).Count;
        }

        /// <summary>
        /// Get order amount stored in DB
        /// </summary>
        /// <returns></returns>
        public float GetPrevOrderAmount()
        {
            return new InventoryItem(new Dictionary<string, string>() { { "Id", Id.ToString() } }).LastOrderAmount;
        }

        /// <summary>
        /// Returns properties that are stored in the DB
        /// </summary>
        /// <param name="omit"></param>
        /// <returns></returns>
        public override string[] GetPropertiesDB(string[] omit = null)
        {
            string[] theseOmissions = new string[] { "PriceExtension", "CountPrice", "PurchaseExtension", "Measure" };
            return base.GetPropertiesDB(ModelHelper.CombineArrays(omit, theseOmissions));
        }

        public IItem Copy()
        {
            return Copy<InventoryItem>();
        }

        public Dictionary<string, float> GetCategoryCosts()
        {
            return new Dictionary<string, float>() { { Category, RecipeCost } };
        }

        public RecipeItem ToRecipeItem()
        {
            return new RecipeItem() { InventoryItemId = Id, Name = Name, Quantity = Count, Measure = Measure };
        }
    }
}
