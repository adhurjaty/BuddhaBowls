using BuddhaBowls.Helpers;
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

    public class TabVM : ObservableObject
    {
        protected static DBCache _models;

        public static MainViewModel ParentContext { get; set; }
        public static bool IsDBConnected = false;

        public bool DBConnection
        {
            get
            {
                return IsDBConnected;
            }
        }

        public TabVM()
        {
            //_models = ModelContainer.Instance();
            if (_models == null)
                _models = new DBCache();
        }

        public static void SetModelContainer(DBCache models)
        {
            _models = models;
        }

        public virtual void FilterItems(string text)
        {
            throw new NotImplementedException();
        }

        public virtual void Refresh()
        {

        }
    }
}
