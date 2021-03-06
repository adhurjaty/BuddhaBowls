﻿using BuddhaBowls.Messengers;
using BuddhaBowls.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Services
{
    /// <summary>
    /// Container for holding inventories
    /// </summary>
    public class InventoriesContainer : ModelContainer<Inventory>
    {
        /// <summary>
        /// Instantiate the container
        /// </summary>
        /// <param name="items"></param>
        public InventoriesContainer(List<Inventory> items, bool isMaster = false) : base(items, isMaster)
        {

        }

        /// <summary>
        /// Adds or overwrites inventory based on date
        /// </summary>
        /// <param name="inv"></param>
        public override Inventory AddItem(Inventory inv)
        {
            int idx = Items.FindIndex(x => x.Date.Date == inv.Date.Date);

            if (idx != -1)
            {
                inv.Id = Items[idx].Id;
                base.Update(inv);
            }
            else
            {
                inv = base.AddItem(inv);
            }
            if (_isMaster)
                Messenger.Instance.NotifyColleagues(MessageTypes.INVENTORY_CHANGED);
            return inv;
        }

        public override void RemoveItem(Inventory item)
        {
            base.RemoveItem(item);
            if (_isMaster)
                Messenger.Instance.NotifyColleagues(MessageTypes.INVENTORY_CHANGED);
        }

        /// <summary>
        /// Updates the inventory
        /// </summary>
        /// <param name="inv"></param>
        //public override void Update(Inventory inv)
        //{
        //    int idx = _items.FindIndex(x => x.Id == inv.Id);
        //    _items[idx].Date = inv.Date;
        //    base.Update(inv);
        //}
    }
}
