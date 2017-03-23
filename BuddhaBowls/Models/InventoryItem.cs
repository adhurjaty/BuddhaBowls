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
        public string Name { get; set; }
        public string Category { get; set; }
        public string PurchasedUnit { get; set; }
        public float Count { get; set; }
        public string CountUnit { get; set; }
        public float Conversion { get; set; }
        public string RecipeUnit { get; set; }
        public float? RecipeUnitConversion { get; set; }
        public float? Yield { get; set; }
        public float LastPurchasedPrice { get; set; }
        public float LastOrderAmount { get; set; }
        public DateTime? LastPurchasedDate { get; set; }
        public int? LastVendorId { get; set; }

        public float PriceExtension
        {
            get
            {
                return Count * CountPrice;
            }
        }

        public float CountPrice
        {
            get
            {
                if (Conversion == 0)
                    return 0;
                return LastPurchasedPrice / Conversion;
            }
        }

        public float CostExtension
        {
            get
            {
                return GetCost() * Count;
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

        public float GetCost()
        {
            if (RecipeUnitConversion == null)
                return 0;
            return LastPurchasedPrice / ((float)RecipeUnitConversion * (float)Yield);
        }

        public float GetLastCount()
        {
            return new InventoryItem(new Dictionary<string, string>() { { "Id", Id.ToString() } }).Count;
        }

        public float GetPrevOrderAmount()
        {
            return new InventoryItem(new Dictionary<string, string>() { { "Id", Id.ToString() } }).LastOrderAmount;
        }

        public override string[] GetPropertiesDB(string[] omit = null)
        {
            string[] theseOmissions = new string[] { "PriceExtension", "CostExtension", "CountPrice" };
            return base.GetPropertiesDB(ModelHelper.CombineArrays(omit, theseOmissions));
        }

        public IItem Copy()
        {
            return Copy<InventoryItem>();
        }
    }
}
