using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Helpers
{
    public static class MainHelper
    {
        public static string ListToCsv(List<string[]> rows)
        {
            return string.Join("\n", rows.Select(x => string.Join(",", x)));
        }
    }
}
