using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Models
{
    public class InventoryItem : Model
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public string PurchasedUnit { get; set; }
        public string CountUnit { get; set; }
        public float Conversion { get; set; }
        public string RecipeUnit { get; set; }
        public float? RecipeUnitConversion { get; set; }
        public float Count { get; set; }
        public float? Yield { get; set; }
        public float LastPurchasedPrice { get; set; }
        public DateTime? LastPurchasedDate { get; set; }

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
    }
}
