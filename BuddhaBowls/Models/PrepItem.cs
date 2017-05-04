using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Models
{
    public class PrepItem : Model
    {
        public string Name { get; set; }
        public string CountUnit { get; set; }
        public float Cost { get; set; }
        public int LineCount { get; set; }
        public int WalkInCount { get; set; }

        public int TotalCount
        {
            get
            {
                return LineCount + WalkInCount;
            }
        }

        public float Extension
        {
            get
            {
                return Cost * TotalCount;
            }
        }

        public PrepItem() : base()
        {
            _tableName = "PrepItem";
        }

        public PrepItem(Dictionary<string, string> searchParams) : this()
        {
            string[] record = _dbInt.GetRecord(_tableName, searchParams);

            if (record != null)
            {
                InitializeObject(record);
            }
        }

        public PrepItem(IItem item) : this()
        {
            Name = item.Name;
        }

    }
}
