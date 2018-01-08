using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Services
{
    public class WeekMarker : PeriodMarker
    {
        public WeekMarker(DateTime startDate, int period, int duration = 7) : base(startDate, period, duration)
        {
        }

        public WeekMarker GetPrevWeek()
        {
            int newWeek = Period - 1;
            if (newWeek < 1)
                newWeek = 4;
            return new WeekMarker(StartDate.AddDays(-7), newWeek);
        }

        public override string ToString()
        {
            return ToString("WK");
        }
    }
}
