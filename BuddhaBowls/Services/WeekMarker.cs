using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Services
{
    public class WeekMarker : PeriodMarker
    {
        public WeekMarker(DateTime startDate, int period) : base(startDate, period, 7)
        {
        }

        public override string ToString()
        {
            return ToString("WK");
        }
    }
}
