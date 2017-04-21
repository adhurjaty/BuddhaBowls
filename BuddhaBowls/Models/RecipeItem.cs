﻿using System;
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

        public RecipeItem() : base()
        {

        }

        public IItem GetIItem()
        {
            IItem item;
            if (InventoryItemId != null)
                item = new InventoryItem(new Dictionary<string, string>() { { "Id", InventoryItemId.ToString() } });
            else
                item = new Recipe(new Dictionary<string, string>() { { "Name", Name } });
            item.Count = Quantity;
            return item;
        }
    }
}
