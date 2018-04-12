using BuddhaBowls.Helpers;
using BuddhaBowls.Messengers;
using BuddhaBowls.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.IO;

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

        public override void Update(PrepItem item)
        {
            base.Update(item);
            if (_isMaster)
                Messenger.Instance.NotifyColleagues(MessageTypes.PREP_ITEM_CHANGED);
        }

        public Dictionary<string, float> GetCategoryValues()
        {
            Dictionary<string, float> costDict = new Dictionary<string, float>();

            foreach (PrepItem item in Items)
            {
                costDict = MainHelper.MergeDicts(costDict, item.GetCategoryCosts(), (x, y) => x + y);
            }

            return costDict;
        }

        public void UpdateCounts(IEnumerable<PrepItem> prepItems)
        {
            foreach (PrepItem item in prepItems)
            {
                PrepItem masterItem = Items.First(x => x.Id == item.Id);
                masterItem.LineCount = item.LineCount;
                masterItem.WalkInCount = item.WalkInCount;
                if (_isMaster)
                    masterItem.Update();
            }

            Messenger.Instance.NotifyColleagues(MessageTypes.PREP_ITEM_CHANGED);
        }

        public List<PrepItem> GetInvPrepItems(Inventory inv)
        {
            if (File.Exists(Path.Combine(Properties.Settings.Default.DBLocation, inv.GetPrepTableName() + ".csv")))
            {
                List<CountPrepItem> countPis = ModelHelper.InstantiateList<CountPrepItem>(inv.GetPrepTableName(), false);
                return Items.Select(x => x.FromCountPrep(countPis.First(y => y.Id == x.Id))).ToList();
            }
            return new List<PrepItem>();
        }
    }
}
