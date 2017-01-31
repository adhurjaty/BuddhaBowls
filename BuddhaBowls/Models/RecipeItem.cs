using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Models
{
    public class RecipeItem : Model
    {
        public string Name { get; set; }
        public int? InventoryItemId { get; set; }
        public float Quantity { get; set; }
    }
}
