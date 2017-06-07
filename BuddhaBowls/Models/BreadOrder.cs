using BuddhaBowls.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Models
{
    public class BreadOrder : Model, INotifyPropertyChanged
    {

        // INotifyPropertyChanged event and method
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public DateTime Date { get; set; }

        private int _grossSales;
        public int GrossSales
        {
            get
            {
                return _grossSales;
            }
            set
            {
                _grossSales = value;
                NotifyPropertyChanged("GrossSales");
            }
        }

        private int _salesForecast;
        public int SalesForecast
        {
            get
            {
                return _salesForecast;
            }
            set
            {
                _salesForecast = value;
                NotifyPropertyChanged("SalesForecast");

                UpdateProperties();
            }
        }

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

        public BreadOrder(DateTime date) : this()
        {
            Date = date;
        }

        public BreadOrder(Dictionary<string, string> searchParams) : this()
        {
            string[] record = _dbInt.GetRecord(_tableName, searchParams);

            if (record != null)
            {
                InitializeObject(record);

                SetNextBreadOrder();
                InitDescriptor();
            }
        }

        private void InitDescriptor()
        {
            if (BreadDescDict != null && NextBreadOrder != null)
            {
                //foreach (KeyValuePair<string, BreadDescriptor> kvp in BreadDescDict)
                //{
                //    BreadDescriptor desc = kvp.Value;
                //    int nextPar = 0;
                //    int nextBuffer = 0;
                //    int nextBegin = 0;
                //    int nextFreeze = 0;
                //    if (NextBreadOrder.BreadDescDict != null && NextBreadOrder.BreadDescDict.ContainsKey(kvp.Key))
                //    {
                //        BreadDescriptor nextDesc = NextBreadOrder.BreadDescDict[kvp.Key];
                //        nextPar = nextDesc.Par;
                //        nextBuffer = nextDesc.Buffer;
                //        nextBegin = nextDesc.BeginInventory;
                //        nextFreeze = nextDesc.FreezerCount;
                //    }
                //    desc.ProjectedOrder = (int)Math.Round((desc.Par + nextPar + nextBuffer + desc.Backup + desc.FreezerCount -
                //                                            desc.BeginInventory - desc.Delivery) / 8.0f) * 8;
                //    desc.Useage = desc.BeginInventory + desc.Delivery + desc.FreezerCount - nextBegin - nextFreeze;
                //}
            }
        }

        public string BreadDescToStr()
        {
            return string.Join("|", BreadDescDict.Select(x => x.Value.PropsToStr()));
        }

        public BreadDescriptor GetBreadDescriptor(string breadType)
        {
            if (BreadDescDict == null)
                BreadDescDict = new Dictionary<string, BreadDescriptor>();
            if (!BreadDescDict.ContainsKey(breadType))
                BreadDescDict[breadType] = new BreadDescriptor(this) { Name = breadType };
            return BreadDescDict[breadType];
        }

        public void UpdateProperties()
        {
            if (BreadDescDict != null)
            {
                foreach (KeyValuePair<string, BreadDescriptor> kvp in BreadDescDict)
                {
                    kvp.Value.UpdateProperties();
                }
            }
        }

        private Dictionary<string, BreadDescriptor> ReadDbDescriptors()
        {
            if (string.IsNullOrWhiteSpace(BreadDescDBString))
                return null;
            return BreadDescDBString.Trim().Split('|').Select(x => new BreadDescriptor(this, x)).ToDictionary(x => x.Name, x => x);
        }

        private void SetNextBreadOrder()
        {
            NextBreadOrder = GetOtherBreadOrder(1);
            if(NextBreadOrder == null)
            {
                NextBreadOrder = GetOtherBreadOrder(-6);
                if (NextBreadOrder == null)
                {
                    NextBreadOrder = new BreadOrder();

                }
                else
                {
                    foreach (KeyValuePair<string, BreadDescriptor> kvp in NextBreadOrder.BreadDescDict)
                    {
                        BreadDescriptor desc = kvp.Value;
                        desc.Clear();
                        //desc.Par = BreadDescDict[kvp.Key].Par;
                    }
                }
            }
        }

        private BreadOrder GetOtherBreadOrder(int dayDiff)
        {
            BreadOrder order = new BreadOrder(new Dictionary<string, string>() { { "Date", Date.AddDays(dayDiff).ToString() } });
            if (order.Date == default(DateTime))
                return null;

            return order;
        }

        #region Overrides

        public override string[] GetPropertiesDB(string[] omit = null)
        {
            string[] theseOmissions = new string[] { "BreadDescDict", "NextBreadOrder" };
            return base.GetPropertiesDB(ModelHelper.CombineArrays(omit, theseOmissions));
        }

        public override void Update()
        {
            if (BreadDescDict != null)
                BreadDescDBString = BreadDescToStr();
            base.Update();
        }

        public override int Insert()
        {
            if (BreadDescDict != null)
                BreadDescDBString = BreadDescToStr();
            return base.Insert();
        }
        
        #endregion
    }

    public class BreadDescriptor : Model, INotifyPropertyChanged
    {
        private BreadOrder _order;

        // INotifyPropertyChanged event and method
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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

        private int _beginInventory;
        public int BeginInventory
        {
            get
            {
                return _beginInventory;
            }
            set
            {
                _beginInventory = value;
                NotifyPropertyChanged("BeginInventory");
                NotifyPropertyChanged("WalkIn");
            }
        }

        public int _delivery;
        public int Delivery
        {
            get
            {
                return _delivery;
            }
            set
            {
                _delivery = value;
                NotifyPropertyChanged("Delivery");
                NotifyPropertyChanged("WalkIn");
            }
        }

        private int _backup;
        public int Backup
        {
            get
            {
                return _backup;
            }
            set
            {
                _backup = value;
                if (_order != null && _order.NextBreadOrder != null && _order.NextBreadOrder.BreadDescDict != null)
                {
                    _order.NextBreadOrder.BreadDescDict[Name].Backup = _backup;
                }
                NotifyPropertyChanged("Backup");
            }
        }

        private int _freezerCount;
        public int FreezerCount
        {
            get
            {
                return _freezerCount;
            }
            set
            {
                _freezerCount = value;
                NotifyPropertyChanged("FreezerCount");
            }
        }

        private int _parFactor;
        public int ParFactor
        {
            get
            {
                if(_parFactor > 0)
                    return _parFactor;
                return 1;
            }
            set
            {
                _parFactor = value;
                if (_order.NextBreadOrder != null && _order.NextBreadOrder.BreadDescDict != null &&
                    _order.NextBreadOrder.BreadDescDict.ContainsKey(Name))
                {
                    _order.NextBreadOrder.BreadDescDict[Name].ParFactor = _parFactor;
                }

                NotifyPropertyChanged("ParFactor");
            }
        }

        public int ProjectedOrder
        {
            get
            {
                if (_order != null && _order.NextBreadOrder != null && _order.NextBreadOrder.BreadDescDict != null)
                {
                    return (int)Math.Round((Par + _order.NextBreadOrder.BreadDescDict[Name].Par + _order.NextBreadOrder.BreadDescDict[Name].Buffer +
                                            Backup - FreezerCount - BeginInventory - Delivery) / 8.0f) * 8;
                }
                return 0;
            }
        }

        public int Useage
        {
            get
            {
                if (_order != null && _order.NextBreadOrder != null && _order.NextBreadOrder.BreadDescDict != null)
                {
                    return BeginInventory + Delivery + FreezerCount - _order.NextBreadOrder.BreadDescDict[Name].BeginInventory -
                            _order.NextBreadOrder.BreadDescDict[Name].FreezerCount;
                }
                return 0;
            }
        }

        public int Par
        {
            get
            {
                if(_order != null)
                    return _order.SalesForecast / ParFactor;
                return 1;
            }
        }

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
            _order = order;
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

        public void Clear()
        {
            BeginInventory = 0;
            Delivery = 0;
            Backup = 0;
            FreezerCount = 0;
        }

        public void UpdateProperties()
        {
            NotifyPropertyChanged("ProjectedOrder");
            NotifyPropertyChanged("Useage");
            NotifyPropertyChanged("Par");
            NotifyPropertyChanged("Buffer");
            NotifyPropertyChanged("WalkIn");
        }

        public override string[] GetPropertiesDB(string[] omit = null)
        {
            string[] theseOmissions = new string[] { "Par", "ProjectedOrder", "Usage", "NextBreadOrder" };
            return base.GetPropertiesDB(ModelHelper.CombineArrays(omit, theseOmissions));
        }
    }
}
