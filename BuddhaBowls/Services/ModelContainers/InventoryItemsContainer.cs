using BuddhaBowls.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Services
{
    public class InventoryItemsContainer : ModelContainer<InventoryItem>
    {
        public InventoryItemsContainer(List<InventoryItem> items, bool isMaster = false) : base(items, isMaster)
        {

        }

        public InventoryItemsContainer Copy()
        {
            InventoryItemsContainer iic = new InventoryItemsContainer(_items.Select(x => x.Copy<InventoryItem>()).ToList());
            _copies.Add(iic);
            return iic;
        }

        protected override void UpdateCopies()
        {
            for (int i = 0; i < _copies.Count; i++)
            {
                _copies[i] = Copy();
            }
        }
    }
}
