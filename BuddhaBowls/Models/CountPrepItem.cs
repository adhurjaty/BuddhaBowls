using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Models
{
    public class CountPrepItem : Model
    {

        private float _lineCount;
        public float LineCount
        {
            get
            {
                return _lineCount;
            }
            set
            {
                _lineCount = value;
                NotifyPropertyChanged("LineCount");
            }
        }

        private float _walkInCount;
        public float WalkInCount
        {
            get
            {
                return _walkInCount;
            }
            set
            {
                _walkInCount = value;
                NotifyPropertyChanged("WalkInCount");
            }
        }

        public CountPrepItem()
        {
        }

        public CountPrepItem(PrepItem item)
        {
            Id = item.Id;
            LineCount = item.LineCount;
            WalkInCount = item.WalkInCount;
        }
    }
}
