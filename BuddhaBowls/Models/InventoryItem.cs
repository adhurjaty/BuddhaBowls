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
        /// Get the cost per Recipe Unit
        /// </summary>
        /// <returns></returns>
        public float GetCost()
        {
            if (RecipeUnitConversion == null || RecipeUnitConversion == 0 || Yield == null || Yield == 0)
                return 0;
            return CountPrice / ((float)RecipeUnitConversion * (float)Yield);
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
            string[] theseOmissions = new string[] { "PriceExtension", "CountPrice", "PurchaseExtension" };
            return base.GetPropertiesDB(ModelHelper.CombineArrays(omit, theseOmissions));
        }

        public IItem Copy()
        {
            return Copy<InventoryItem>();
        }
    }
}
