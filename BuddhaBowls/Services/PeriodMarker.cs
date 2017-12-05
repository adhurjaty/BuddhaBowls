using BuddhaBowls.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Services
{
    public class PeriodMarker
    {
        protected int _duration;

        public int Period { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public PeriodMarker(DateTime startDate, int period, int duration = 28)
        {
            _duration = duration;
            Period = period;
            if (Period == 0)
            {
                DateTime theFirst = new DateTime(DateTime.Today.Year, 1, 1);
                DateTime firstMonday = theFirst.AddDays(MainHelper.Mod(8 - (int)theFirst.DayOfWeek, 7));
                StartDate = theFirst;
                EndDate = firstMonday.AddSeconds(-1);
            }
            else
            {
                StartDate = startDate.AddHours(4);
                EndDate = StartDate.AddDays(duration).AddSeconds(-1);
                if (EndDate.Year > StartDate.Year)
                    EndDate = new DateTime(EndDate.Year, 1, 1).AddSeconds(-1);
            }
        }

        public PeriodMarker GetPrevPeriod()
        {
            if (Period == 0)
                return null;

            return new PeriodMarker(StartDate.AddDays(-_duration), Period - 1, _duration);
        }

        public override string ToString()
        {
            return ToString("P");
        }

        protected string ToString(string prefix)
        {
            return prefix + Period + " " + StartDate.ToString("M/d") + "-" + EndDate.ToString("M/d");
        }
    }
}
