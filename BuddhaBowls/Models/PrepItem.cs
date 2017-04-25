using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Models
{
    public class PrepItem : Model
    {
        public int WillNeed { get; set; }
        public int MayNeed { get; set; }
        public int NotNeed { get; set; }
        public string Name { get; set; }
        public string CountUnit { get; set; }
        public int LineCount { get; set; }
        public int LineCountPar { get; set; }
        public int WalkInCount { get; set; }
        public int WalkInCountPar { get; set; }
        public float Threshold { get; set; }
        public int PrepQty { get; set; }

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
