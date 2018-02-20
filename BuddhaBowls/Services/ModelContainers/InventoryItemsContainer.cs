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
            InventoryItemsContainer iic = CleanCopy();
            _copies.Add(iic);
            return iic;
        }

        public InventoryItemsContainer CleanCopy()
        {
            return new InventoryItemsContainer(_items.Select(x => x.Copy<InventoryItem>()).ToList());
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

        /// <summary>
        /// Get the PriceExtension value of each category of items
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, float> GetCategoryValues()
        {
            Dictionary<string, float> costDict = new Dictionary<string, float>();

            foreach (InventoryItem item in _items)
            {
                if (!costDict.Keys.Contains(item.Category))
                    costDict[item.Category] = 0;
                costDict[item.Category] += item.PriceExtension;
            }

            return costDict;
        }

        protected override void UpdateCopies()
        {
            for (int i = 0; i < _copies.Count; i++)
            {
                _copies[i] = CleanCopy();
            }
        }


    }
}
