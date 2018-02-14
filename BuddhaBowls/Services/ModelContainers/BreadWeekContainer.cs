using BuddhaBowls.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Services
{
    public class BreadWeekContainer : ModelContainer<BreadOrder>
    {
        private List<VendorInventoryItem> _breadInvItems;

        public BreadOrder[] Week
        {
            get
            {
                return Items.ToArray();
            }
        }

        public BreadOrder[] WeekNoTotal
        {
            get
            {
                return Week.Take(7).ToArray();
            }
        }

        public BreadWeekContainer(List<BreadOrder> items, List<VendorInventoryItem> breadInvItems) : base(items, true)
        {
            _breadInvItems = breadInvItems;
        }

        public IEnumerable<InventoryItem> GetWeekAsInvItems()
        {
            foreach (KeyValuePair<string, BreadDescriptor> descKvp in WeekNoTotal.Where(x => x.BreadDescDict != null && x.Date <= DateTime.Today)
                                                                                            .SelectMany(x => x.BreadDescDict.ToList()))
            {
                InventoryItem item = _breadInvItems.First(x => x.Name == descKvp.Key).Copy<InventoryItem>();
                item.LastOrderAmount = descKvp.Value.Delivery;
                item.Count = descKvp.Value.FreezerCount + descKvp.Value.BeginInventory;
                yield return item;
            }
        }

        public BreadOrder BreadOrderFromDate(DateTime date)
        {
            int idx = (int)(date.Date - Week[0].Date).TotalDays;
            if (idx > 0 && idx < 7)
                return Week[idx];
            return null;
        }
    }
}
