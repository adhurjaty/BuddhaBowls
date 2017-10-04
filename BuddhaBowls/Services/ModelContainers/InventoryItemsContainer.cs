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
        public InventoryItemsContainer(List<InventoryItem> items) : base(items)
        {

        }
    }
}
