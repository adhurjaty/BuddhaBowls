using BuddhaBowls.Helpers;
using BuddhaBowls.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Services
{
    public class ModelContainer
    {
        public List<InventoryItem> InventoryItems { get; set; }
        public List<BatchItem> BatchItems { get; set; }

        public ModelContainer()
        {
            InitializeModels();
        }

        public void InitializeModels()
        {
            InventoryItems = ModelHelper.InstantiateList<InventoryItem>("InventoryItem");
            BatchItems = ModelHelper.InstantiateList<BatchItem>("BatchItem");

            if (InventoryItems == null || BatchItems == null)
                return;

            foreach(BatchItem bi in BatchItems)
            {
                bi.recipe = MainHelper.GetRecipe(bi.Name);
            }
        }

        public float GetBatchItemCost(BatchItem item)
        {
            float cost = 0;
            foreach(RecipeItem ri in item.recipe)
            {
                InventoryItem invItem = InventoryItems[(int)ri.InventoryItemId];
                cost += invItem.GetCost() * ri.Quantity;
            }

            return cost;
        }

        public Dictionary<string, float> GetCategoryCosts(BatchItem item)
        {
            Dictionary<string, float> costDict = new Dictionary<string, float>();

            foreach(RecipeItem ri in item.recipe)
            {
                InventoryItem invItem = InventoryItems[(int)ri.InventoryItemId];
                if(!costDict.Keys.Contains(invItem.Category))
                    costDict[invItem.Category] = 0;
                costDict[invItem.Category] += invItem.GetCost() * ri.Quantity;
            }

            return costDict;
        }
    }
}
