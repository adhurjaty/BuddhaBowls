using BuddhaBowls.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Models
{
    public class InventoryItem : Model, IItem
    {
        private float _lastCount;
        private float _prevOrderAmount;

        public string Name { get; set; }
        public string Category { get; set; }
        public string PurchasedUnit { get; set; }
        public string CountUnit { get; set; }
        public float Conversion { get; set; }
        public string RecipeUnit { get; set; }
        public float? RecipeUnitConversion { get; set; }
        public float? Yield { get; set; }
        public float LastPurchasedPrice { get; set; }
        public DateTime? LastPurchasedDate { get; set; }

        public bool countUpdated = false;
        private float _count;
        public float Count
        {
            get
            {
                return _count;
            }
            set
            {
                if (!countUpdated)
                {
                    _lastCount = _count;
                    if (CountChanged != null)
                        CountChanged(this);
                }
                _count = value;
                countUpdated = true;
            }
        }

        public bool orderAmountUpdated = false;
        private float _lastOrderAmount;
        public float LastOrderAmount
        {
            get
            {
                return _lastOrderAmount;
            }
            set
            {
                if(!orderAmountUpdated)
                {
                    _prevOrderAmount = _lastOrderAmount;
                }
                _lastOrderAmount = value;
                if(OrderAmountChanged != null)
                    OrderAmountChanged(this);
                orderAmountUpdated = true;
            }
        }


        public float PriceExtension
        {
            get
            {
                return LastOrderAmount * LastPurchasedPrice;
            }
        }

        public ModelPropertyChanged CountChanged;
        public ModelPropertyChanged OrderAmountChanged;

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

        public float GetCost()
        {
            if (RecipeUnitConversion == null)
                return 0;
            return LastPurchasedPrice / ((float)RecipeUnitConversion * (float)Yield);
        }

        public float GetLastCount()
        {
            return _lastCount;
        }

        public float GetPrevOrderAmount()
        {
            return _prevOrderAmount;
        }

        public override string[] GetPropertiesDB(string[] omit)
        {
            return base.GetPropertiesDB(new string[] { "PriceExtension" });
        }
    }
}
