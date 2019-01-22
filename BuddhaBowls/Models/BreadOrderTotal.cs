using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Models
{
    public class BreadOrderTotal : BreadOrder
    {
        private BreadOrder[] _breadWeek;

        public override float GrossSales
        {
            get
            {
                return _breadWeek.Sum(x => x.GrossSales);
            }
        }

        public override int SalesForecast
        {
            get
            {
                return _breadWeek.Sum(x => x.SalesForecast);
            }
        }

        public override Dictionary<string, BreadDescriptor> BreadDescDict
        {
            get
            {
                if (_breadWeek == null || _breadWeek[0] == null || _breadWeek[0].BreadDescDict == null)
                    return null;
                return _breadWeek[0].BreadDescDict.Keys.ToDictionary(x => x, x =>
                        (BreadDescriptor)new BreadDescriptorTotal(_breadWeek.Where(y => y.BreadDescDict != null && y.BreadDescDict.ContainsKey(x))
                                                                            .Select(y => y.BreadDescDict[x]).ToArray()));
            }
        }

        public BreadOrderTotal(ref BreadOrder[] breadWeek) : base()
        {
            _breadWeek = breadWeek;
        }

        public void UpdateDetails()
        {
            NotifyPropertyChanged("GrossSales");
            NotifyPropertyChanged("SalesForecast");
            NotifyPropertyChanged("BreadDescDict");
        }
    }

    public class BreadDescriptorTotal : BreadDescriptor
    {
        private BreadDescriptor[] _descWeek;

        public override int Backup
        {
            get
            {
                return _descWeek.Sum(x => x.Backup);
            }
        }

        public override int BeginInventory
        {
            get
            {
                return _descWeek.Sum(x => x.BeginInventory);
            }
        }

        public override int Delivery
        {
            get
            {
                return _descWeek.Sum(x => x.Delivery);
            }
        }

        public override int FreezerCount
        {
            get
            {
                return _descWeek.Sum(x => x.FreezerCount);
            }
        }

        public override int ProjectedOrder
        {
            get
            {
                return _descWeek.Sum(x => x.ProjectedOrder);
            }
        }

        public override int Usage
        {
            get
            {
                return _descWeek.Sum(x => x.Usage);
            }
        }

        public BreadDescriptorTotal(BreadDescriptor[] descWeek) : base()
        {
            _descWeek = descWeek;
        }
    }
}
