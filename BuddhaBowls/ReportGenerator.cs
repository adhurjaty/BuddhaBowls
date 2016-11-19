using BuddhaBowls.Helpers;
using BuddhaBowls.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls
{
    public class ReportGenerator
    {
        public ReportGenerator()
        {

        }

        public List<RecipeItem> GetRecipe(string recipeName)
        {
            string tableName = Path.Combine(Properties.Resources.RecipeFolder, recipeName);

            return ModelHelper.InstantiateList<RecipeItem>(tableName);
        }

        public List<VendorItem> GetVendorPrices(string vendorName)
        {
            string tableName = Path.Combine(Properties.Resources.VendorFolder, vendorName);

            return ModelHelper.InstantiateList<VendorItem>(tableName);
        }
    }
}
