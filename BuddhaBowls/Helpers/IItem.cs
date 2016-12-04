using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Helpers
{
    public interface IItem
    {
        string Name { get; set; }
        string Category { get; set; }
        string PurchasedUnit { get; set; }
        string CountUnit { get; set; }
        float Conversion { get; set; }
        string RecipeUnit { get; set; }
        float? RecipeUnitConversion { get; set; }
        float? Yield { get; set; }
        float LastPurchasedPrice { get; set; }
        float LastOrderAmount { get; set; }
        DateTime? LastPurchasedDate { get; set; }
        float Count { get; set; }
    }
}
