using BuddhaBowls.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Models
{
    public class BatchItem : Model
    {
        public string Name { get; set; }
        public string RecipeUnit { get; set; }
        public float Yield { get; set; }
        public string Category { get; set; }

        public List<RecipeItem> recipe;

        public BatchItem() : base()
        {
            _tableName = "BatchItem";
        }

        public BatchItem(Dictionary<string, string> searchParams) : this()
        {
            string[] record = _dbInt.GetRecord(_tableName, searchParams);

            if (record != null)
            {
                InitializeObject(record);
            }

            recipe = MainHelper.GetRecipe(Name);
        }
    }
}
