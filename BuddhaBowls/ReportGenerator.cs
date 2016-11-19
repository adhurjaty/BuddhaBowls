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
        List<InventoryItem> _inventoryItems;

        public ReportGenerator()
        {
            _inventoryItems = ModelHelper.InstantiateList<InventoryItem>("InventoryItem");
        }

        public void Generate()
        {
            
        }

        public List<RecipeItem> GetRecipe(string recipeName)
        {
            string tableName = Path.Combine(Properties.Resources.RecipeFolder, recipeName);

            return ModelHelper.InstantiateList<RecipeItem>(tableName, false);
        }

        public List<VendorItem> GetVendorPrices(string vendorName)
        {
            string tableName = Path.Combine(Properties.Resources.VendorFolder, vendorName);

            return ModelHelper.InstantiateList<VendorItem>(tableName, false);
        }

        public void FillInventoryId(string recipeName)
        {
            List<RecipeItem> recipe = GetRecipe(recipeName);

            foreach(RecipeItem ri in recipe)
            {
                ri.InventoryItemId = _inventoryItems.First(x => x.Name == ri.Name).Id;
                ri.Update(recipeName);
            }
        }

        public void MakeRecipeTable(string recipeName, string outputFilePath)
        {
            List<RecipeItem> recipe = GetRecipe(recipeName);
            List<string[]> outList = new List<string[]>();
            Dictionary<string, List<string[]>> categoryDict = new Dictionary<string, List<string[]>>();
            Dictionary<string, float> categoryCosts = new Dictionary<string, float>();

            string[] headers = new string[] { "NAME", "MEASURE", "RECIPE UNIT", "# RU", "RU COST", "COST" };

            foreach(RecipeItem item in recipe)
            {
                InventoryItem inv = _inventoryItems[(int)item.InventoryItemId];
                if(!categoryDict.Keys.Contains(inv.Category))
                {
                    categoryDict[inv.Category] = new List<string[]>();
                    categoryCosts[inv.Category] = 0;
                }

                float cost = GetCost(inv);
                float lineCost = cost * item.Quantity;
                categoryDict[inv.Category].Add(new string[] { inv.Name, item.Measure, inv.RecipeUnit,
                                                item.Quantity.ToString(), cost.ToString(), lineCost.ToString() });
                categoryCosts[inv.Category] += lineCost;
            }

            float batchCost = categoryCosts.Keys.Sum(x => categoryCosts[x]);

            string[] sortedKeys = categoryDict.Keys.ToArray();
            Array.Sort(sortedKeys);
            foreach (string key in sortedKeys)
            {
                string[] headerCopy = new string[headers.Length];
                Array.Copy(headers, headerCopy, headers.Length);
                headerCopy[0] = key;
                outList.Add(headerCopy);

                foreach(string[] row in categoryDict[key])
                {
                    outList.Add(row);
                }

                outList.Add(new string[] { key.ToUpper() + " TOTAL", categoryCosts[key].ToString("c") });
                outList.Add(new string[] { "%", (categoryCosts[key] / batchCost).ToString("p1") });
            }

            outList.Add(new string[] { "BATCH TOTAL", batchCost.ToString("c") });

            string fileContents = string.Join("\n", outList.Select(x => string.Join(",", x)));
            File.WriteAllText(outputFilePath, fileContents);
        }

        private float GetCost(InventoryItem item)
        {
            return item.LastPurchasedPrice / ((float)item.RecipeUnitConversion * (float)item.Yield);
        }
    }
}
