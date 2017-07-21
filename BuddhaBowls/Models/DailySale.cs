using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Models
{
    public class DailySale : Model
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public int Quantity { get; set; }
        public float NetTotal { get; set; }
        public DateTime Date { get; set; }
        public DateTime LastUpdated { get; set; }

        public DailySale() : base()
        {
            _tableName = "DailySale";
        }

        public DailySale(Dictionary<string, string> searchParams) : this()
        {
            string[] record = _dbInt.GetRecord(_tableName, searchParams);

            if (record != null)
            {
                InitializeObject(record);
            }
        }

        public override int Insert()
        {
            LastUpdated = DateTime.Now;
            return base.Insert();
        }

        public override void Update()
        {
            LastUpdated = DateTime.Now;
            base.Update();
        }
    }
}
