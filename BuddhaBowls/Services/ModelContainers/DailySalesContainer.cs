using BuddhaBowls.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Services
{
    public class DailySalesContainer : ModelContainer<DailySale>
    {
        public DailySalesContainer(List<DailySale> items) : base(items, true)
        {

        }

        /// <summary>
        /// Clears all daily sale records before the startDate
        /// </summary>
        /// <param name="startDate"></param>
        public void ClearPrevDailySales(DateTime startDate)
        {
            // may need to speed this up
            foreach (DailySale sale in Items.Where(x => x.Date < startDate).ToList())
            {
                sale.Destroy();
            }
            SetItems(Items.Where(x => x.Date >= startDate).ToList());
        }

        public void DestroyPartialDays(DateTime lastUpdated)
        {
            // destroy period sale records that are partial days
            foreach (DailySale sale in Items.Where(x => x.Date.Date == lastUpdated))
            {
                sale.Destroy();
            }
        }

        public List<DailySale> GetSalesInPeriod(PeriodMarker period)
        {
            return Items.Where(x => period.StartDate <= x.Date && x.Date < period.EndDate).ToList();
        }
    }
}
