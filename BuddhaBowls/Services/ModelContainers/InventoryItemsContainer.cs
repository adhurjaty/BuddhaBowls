using BuddhaBowls.Helpers;
using BuddhaBowls.Messengers;
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

        public override InventoryItem AddItem(InventoryItem item)
        {
            InventoryItem retItem = base.AddItem(item);
            _items = MainHelper.SortItems(_items).ToList();
            if(_isMaster)
                Messenger.Instance.NotifyColleagues(MessageTypes.INVENTORY_ITEM_ADDED, item);
            return retItem;
        }

        public override void RemoveItem(InventoryItem item)
        {
            base.RemoveItem(item);
            if (_isMaster)
            {
                Properties.Settings.Default.InventoryOrder.Remove(item.Name);
                Messenger.Instance.NotifyColleagues(MessageTypes.INVENTORY_ITEM_REMOVED, item);
            }
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
