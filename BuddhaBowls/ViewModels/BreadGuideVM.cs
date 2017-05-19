using BuddhaBowls.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls
{
    public class BreadGuideVM : TabVM
    {
        #region Content Binders

        private ObservableCollection<BreadOrder> _breadOrderList;
        public ObservableCollection<BreadOrder> BreadOrderList
        {
            get
            {
                return _breadOrderList;
            }
            set
            {
                _breadOrderList = value;
                NotifyPropertyChanged("BreadOrderList");
            }
        }

        #endregion

        #region ICommand and CanExecute

        #endregion

        public BreadGuideVM() : base()
        {

        }

        #region ICommand Helpers

        #endregion

        #region Initializers

        #endregion

        #region UI Updaters

        #endregion
    }
}
