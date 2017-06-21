using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Services
{
    public class ShowAllMarker : WeekMarker
    {

        public ShowAllMarker() : base(DateTime.MinValue, -1)
        {
            EndDate = DateTime.MaxValue;
        }

        public override string ToString()
        {
            return "Show All";
        }
    }
}
