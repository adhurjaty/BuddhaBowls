using BuddhaBowls.Messengers;
using BuddhaBowls.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Services
{
    public class PrepItemsContainer : ModelContainer<PrepItem>
    {
        private VendorInvItemsContainer _viContainer;
        private RecipesContainer _rContainer;

        public PrepItemsContainer(List<PrepItem> items, VendorInvItemsContainer vic, RecipesContainer rc, bool isMaster = false) : base(items, isMaster)
        {
            _viContainer = vic;
            _rContainer = rc;

            foreach (PrepItem item in items)
            {
                if (item.InventoryItemId != null)
                    item.SetItem(_viContainer.Items.First(x => x.Id == item.InventoryItemId));
                else if (item.RecipeItemId != null)
                    item.SetItem(_rContainer.Items.First(x => x.Id == item.RecipeItemId));
            }
        }

        public override PrepItem AddItem(PrepItem item)
        {
            PrepItem outItem = base.AddItem(item);
            if (_isMaster)
                Messenger.Instance.NotifyColleagues(MessageTypes.PREP_ITEM_CHANGED);

            return outItem;
        }

        public override void RemoveItem(PrepItem item)
        {
            base.RemoveItem(item);
            if (_isMaster)
                Messenger.Instance.NotifyColleagues(MessageTypes.PREP_ITEM_CHANGED);
        }
    }
}
