using BuddhaBowls.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Models
{
    public class BreadOrder : Model
    {
        public DateTime Date { get; set; }
        public int GrossSales { get; set; }
        public int SalesForecast { get; set; }
        public string BreadDescDBString { get; set; }

        public BreadOrder NextBreadOrder { get; set; }

        public string Day
        {
            get
            {
                return Date.DayOfWeek.ToString();
            }
        }

        private Dictionary<string, BreadDescriptor> _breadDescDict;
        public Dictionary<string, BreadDescriptor> BreadDescDict
        {
            get
            {
                if (_breadDescDict == null)
                    _breadDescDict = ReadDbDescriptors();
                return _breadDescDict;
            }
            set
            {
                _breadDescDict = value;
            }
        }

        public BreadOrder() : base()
        {
            _tableName = "BreadOrder";
        }

        public BreadOrder(Dictionary<string, string> searchParams) : this()
        {
            string[] record = _dbInt.GetRecord(_tableName, searchParams);

            if (record != null)
            {
                InitializeObject(record);
            }
        }

        public override string[] GetPropertiesDB(string[] omit = null)
        {
            string[] theseOmissions = new string[] { "BreadDescDict" };
            return base.GetPropertiesDB(ModelHelper.CombineArrays(omit, theseOmissions));
        }

        public string BreadDescToStr()
        {
            return string.Join("|", BreadDescDict.Select(x => x.Value.PropsToStr()));
        }

        private Dictionary<string, BreadDescriptor> ReadDbDescriptors()
        {
            if (string.IsNullOrWhiteSpace(BreadDescDBString))
                return null;
            return BreadDescDBString.Trim().Split('|').Select(x => new BreadDescriptor(this, x)).ToDictionary(x => x.Name, x => x);
        }
    }

    public class BreadDescriptor : Model
    {
        private string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (value != null)
                    _name = value.Replace(";", "").Replace("|", "").Replace("=", "");
                else
                    _name = null;
            }
        }
        public int BeginInventory { get; set; }
        public int Delivery { get; set; }
        public int Backup { get; set; }
        public int FreezerCount { get; set; }

        // not set by user, but must be set in BreadOrderWeek
        public int ProjectedOrder { get; set; }
        public int Usage { get; set; }

        // not set by user but must be set in constructor
        public int Par { get; private set; }

        public int Buffer
        {
            get
            {
                return Par / 5;
            }
        }
        public int WalkIn
        {
            get
            {
                return (BeginInventory + Delivery - (Par + Buffer)) / 8;
            }
        }

        public BreadDescriptor(BreadOrder order)
        {
            Par = order.SalesForecast / 10;
        }

        /// <summary>
        /// Construct BreadDescriptor from a string from the DB
        /// </summary>
        /// <param name="dbInString">String from DB in the form `Name=...;BreadId=...;etc</param>
        public BreadDescriptor(BreadOrder order, string dbInString) : this(order)
        {
            foreach (string keyValStr in dbInString.Split(';'))
            {
                string[] keyVal = keyValStr.Split('=');
                string key = keyVal[0];
                string val = keyVal[1];

                SetProperty(key, val);
            }
        }

        public string PropsToStr()
        {
            return string.Join(";", GetPropertiesDB().Select(x => x + "=" + GetPropertyValue(x).ToString()));
        }

        public override string[] GetPropertiesDB(string[] omit = null)
        {
            string[] theseOmissions = new string[] { "Par", "ProjectedOrder", "Usage", "NextBreadOrder" };
            return base.GetPropertiesDB(ModelHelper.CombineArrays(omit, theseOmissions));
        }
    }
}
