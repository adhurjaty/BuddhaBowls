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

        public BreadWeekContainer(List<BreadOrder> items) : base(items)
        {

        }
    }
}
