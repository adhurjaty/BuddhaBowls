using BuddhaBowls.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls
{
    public class OrderBreakdownVM : INotifyPropertyChanged
    {
        // INotifyPropertyChanged event and method
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private ObservableCollection<BreakdownCategoryItem> _breakdownList;
        public ObservableCollection<BreakdownCategoryItem> BreakdownList
        {
            get
            {
                return _breakdownList;
            }
            set
            {
                _breakdownList = value;
                NotifyPropertyChanged("BreakdownList");
            }
        }

        private float _orderCost;
        public float OrderTotal
        {
            get
            {
                return _orderCost;
            }
            set
            {
                _orderCost = value;
                NotifyPropertyChanged("OrderCost");
            }
        }
    }

    public class BreakdownCategoryItem
    {
        public string Background { get; set; }
        public string Category { get; set; }
        public float TotalAmount { get; set; }
        public ObservableCollection<InventoryItem> Items { get; set; }

        public BreakdownCategoryItem(IEnumerable<InventoryItem> items)
        {
            Items = new ObservableCollection<InventoryItem>(items);
            Category = Items.First().Category;

            TotalAmount = Items.Sum(x => x.PriceExtension);
        }
    }
}
