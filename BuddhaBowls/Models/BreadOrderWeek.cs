using BuddhaBowls.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Models
{
    public class BreadOrderWeek
    {
        // index where the new records start
        private int _insertStartIdx;

        public BreadOrder[] BreadOrderDays { get; set; }

        public BreadOrderWeek()
        {
            BreadOrderDays = new BreadOrder[8];

            SetWeek();
            InitValues();
        }

        public void SetPrevWeek(DateTime date)
        {
            _insertStartIdx = BreadOrderDays.Length;
            DateTime startDate = GetStartDate(date);
            for (int i = 0; i < BreadOrderDays.Length; i++)
            {
                BreadOrder bo = new BreadOrder(new Dictionary<string, string>() { { "Date", startDate.AddDays(i).ToString() } });
                if(bo.Date == default(DateTime))
                {
                    bo = new BreadOrder() { Date = startDate.AddDays(i) };
                    if (_insertStartIdx == BreadOrderDays.Length)
                        _insertStartIdx = i;
                }
                BreadOrderDays[i] = bo;
            }
        }

        public void Save()
        {
            for (int i = 0; i < BreadOrderDays.Length; i++)
            {
                if (i < _insertStartIdx)
                    BreadOrderDays[i].Update();
                else
                    BreadOrderDays[i].Insert();
            }
        }

        private void SetWeek()
        {
            SetPrevWeek(DateTime.Today);
        }

        private void InitValues()
        {
            for (int i = 0; i < BreadOrderDays.Length - 1; i++)
            {
                BreadOrder order = BreadOrderDays[i];
                if(order.BreadDescDict!= null)
                {
                    foreach (KeyValuePair<string, BreadDescriptor> kvp in order.BreadDescDict)
                    {
                        BreadOrder nextOrder = BreadOrderDays[i + 1];
                        BreadDescriptor desc = kvp.Value;
                        int nextPar = 0;
                        int nextBuffer = 0;
                        int nextBegin = 0;
                        int nextFreeze = 0;
                        if(nextOrder.BreadDescDict != null && nextOrder.BreadDescDict.ContainsKey(kvp.Key))
                        {
                            BreadDescriptor nextDesc = nextOrder.BreadDescDict[kvp.Key];
                            nextPar = nextDesc.Par;
                            nextBuffer = nextDesc.Buffer;
                            nextBegin = nextDesc.BeginInventory;
                            nextFreeze = nextDesc.FreezerCount;
                        }
                        desc.ProjectedOrder = (int)Math.Round((desc.Par + nextPar + nextBuffer + desc.Backup + desc.FreezerCount -
                                                                desc.BeginInventory - desc.Delivery) / 8.0f) * 8;
                        desc.Usage = desc.BeginInventory + desc.Delivery + desc.FreezerCount - nextBegin - nextFreeze;
                    }
                }
            }
        }

        private DateTime GetStartDate(DateTime date)
        {
            int monday = 1;
            int diff = monday - (int)date.DayOfWeek;
            if (diff > 0)
                diff = -6;

            return date.AddDays(diff);
        }

        private DateTime GetStartDate()
        {
            return GetStartDate(DateTime.Today);
        }
    }
}
