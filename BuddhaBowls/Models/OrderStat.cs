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
    public class OrderStat : ObservableObject
    {
        private string _label;
        public string Label
        {
            get
            {
                return _label + ":";
            }
            set
            {
                _label = value;
                NotifyPropertyChanged("Label");
            }
        }

        private float _value;
        public float Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
                NotifyPropertyChanged("Value");
            }
        }
    }
}
