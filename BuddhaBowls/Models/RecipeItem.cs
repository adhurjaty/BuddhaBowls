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
        public int InventoryItemId { get; set; }
        public int Quantity { get; set; }
        public int Yield { get; set; }

        public RecipeItem()
        {
            _dbInt = null;
        }
    }
}
