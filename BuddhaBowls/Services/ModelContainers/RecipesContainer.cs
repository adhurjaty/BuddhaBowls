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
        public RecipesContainer(List<Recipe> items, bool isMaster = false) : base(items, isMaster)
        {

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
            RecipesContainer rc = new RecipesContainer(_items.Select(x => (Recipe)x.Copy()).ToList());
            _copies.Add(rc);
            return rc;
        }

        public override Recipe AddItem(Recipe item)
        {
            Recipe rec = base.AddItem(item);
            if (_isMaster)
                Messenger.Instance.NotifyColleagues(MessageTypes.RECIPE_CHANGED);
            return rec;
        }

        public override void RemoveItem(Recipe item)
        {
            base.RemoveItem(item);
            if (_isMaster)
                Messenger.Instance.NotifyColleagues(MessageTypes.RECIPE_CHANGED);
        }
    }
}
