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
    public class RecipesContainer : ModelContainer<Recipe>
    {
        private VendorInvItemsContainer _vendInvContainer;

        public RecipesContainer(List<Recipe> items, VendorInvItemsContainer container, bool isMaster = false) : base(items, isMaster)
        {
            _vendInvContainer = container;
            foreach (Recipe item in items)
            {
                item.SetContainer(this);
            }
        }

        /// <summary>
        /// Gets a list of the currently existing recipe categories
        /// </summary>
        /// <returns></returns>
        public List<string> GetRecipeCategories()
        {
            HashSet<string> categories = new HashSet<string>();
            foreach (Recipe rec in Items)
            {
                if (!string.IsNullOrWhiteSpace(rec.Category))
                    categories.Add(rec.Category);
            }

            return categories.ToList();
        }

        public RecipesContainer Copy()
        {
            RecipesContainer rc = new RecipesContainer(_items.Select(x => (Recipe)x.Copy()).ToList(), _vendInvContainer);
            _copies.Add(rc);
            return rc;
        }

        public override Recipe AddItem(Recipe item)
        {
            Recipe rec = base.AddItem(item);
            if (_isMaster)
                Messenger.Instance.NotifyColleagues(MessageTypes.RECIPE_CHANGED, item);
            return rec;
        }

        public override void RemoveItem(Recipe item)
        {
            base.RemoveItem(item);
            if (_isMaster)
                Messenger.Instance.NotifyColleagues(MessageTypes.RECIPE_CHANGED, item);
        }

        public List<IItem> GetRecipeInvItems(List<RecipeItem> items)
        {
            List<IItem> outItems = items.Select(x => x.InventoryItemId != null ?
                                                        _vendInvContainer.Items.First(y => y.Id == x.InventoryItemId).ToInventoryItem().Copy() :
                                                        Items.First(y => y.Name == x.Name).Copy()).ToList();
            for (int i = 0; i < items.Count; i++)
            {
                outItems[i].Count = items[i].Quantity;
                outItems[i].Measure = items[i].Measure;
            }

            return MainHelper.SortItems(outItems).ToList();
        }
    }
}
