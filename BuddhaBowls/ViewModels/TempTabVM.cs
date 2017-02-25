using BuddhaBowls.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls
{
    public class TempTabVM : TabVM
    {

        public TempTabVM() : base()
        {

        }

        protected void Close()
        {
            MainViewModel.Instance().DeleteTempTab();
        }
    }
}
