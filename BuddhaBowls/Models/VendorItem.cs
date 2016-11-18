using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Models
{
    public class VendorItem : Model
    {
        public string Name { get; set; }
        public int InventoryItemId { get; set; }
        public float price { get; set; }
        public string PurchasedUnit { get; set; }
        public int Conversion { get; set; }
    }
}
