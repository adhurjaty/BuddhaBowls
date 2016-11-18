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
        public string Unit { get; set; }
        public string CountUnit { get; set; }
        public int Conversion { get; set; }
        public string RecipeUnit { get; set; }
        public int RecipeUnitConversion { get; set; }
        public int Count { get; set; }
        public float LastUnitPrice { get; set; }
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
    }
}
