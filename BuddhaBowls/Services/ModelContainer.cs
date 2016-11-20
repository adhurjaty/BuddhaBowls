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
    }
}
