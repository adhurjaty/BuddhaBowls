using BuddhaBowls.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls
{
    public delegate void RefreshDel();

    public class TabVM : INotifyPropertyChanged
    {
        protected ModelContainer _models;

        public static MainViewModel ParentContext { get; set; }
        public static bool IsDBConnected = false;

        public bool DBConnection
        {
            get
            {
                return IsDBConnected;
            }
        }

        // INotifyPropertyChanged event and method
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public TabVM()
        {
            _models = ModelContainer.Instance();
        }

        public virtual void FilterItems(string text)
        {
            throw new NotImplementedException();
        }
    }
}
