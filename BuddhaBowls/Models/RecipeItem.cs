using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Models
{
    public class RecipeItem : Model
    {
        public string Name { get; set; }
        public int? InventoryItemId { get; set; }
        public string Measure { get; set; }
        public float Quantity { get; set; }
        public bool IsBatch { get; set; }

        public RecipeItem() : base()
        {
        }

        public void Update(string recipeName)
        {
            _tableName = Path.Combine(Properties.Resources.RecipeFolder, recipeName);
            Update();
        }
    }
}
