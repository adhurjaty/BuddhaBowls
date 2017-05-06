using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Models
{
    public class PrepItem : Model, INotifyPropertyChanged
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

        // INotifyPropertyChanged event and method
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

        public void NotifyChanges()
        {
            NotifyPropertyChanged("TotalCount");
            NotifyPropertyChanged("Extension");
        }
    }
}
