using BuddhaBowls.Helpers;
using BuddhaBowls.Models;
using BuddhaBowls.Services;
using BuddhaBowls.UserControls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace BuddhaBowls
{
    public class ReportsTabVM : ChangeableTabVM
    {
        private CogsVM _cogsVM;
        private ProfitLossVM _plVM;

        #region Content Binders

        #endregion

        #region ICommand and CanExecute

        #endregion

        public ReportsTabVM() : base()
        {
            InitSwitchButtons(new string[] { "COGS", "P & L" });
            _plVM = new ProfitLossVM();
        }

        #region ICommand Helpers

        #endregion

        #region Initializers

        #endregion

        #region UI Updaters

        public override void Refresh()
        {
            _cogsVM.CogsUpdated();
            base.Refresh();
        }

        public void UpdatedCogs(DateTime inventoryDate)
        {
            if (_cogsVM.PeriodSelector.SelectedWeek.StartDate <= inventoryDate && inventoryDate <= _cogsVM.PeriodSelector.SelectedWeek.EndDate)
                Refresh();
        }

        #endregion

        protected override void ChangePageState(int pageIdx)
        {
            base.ChangePageState(pageIdx);

            switch (pageIdx)
            {
                case 0:
                    Header = "Cost of Goods Sold";
                    if(_cogsVM == null)
                        _cogsVM = new CogsVM();
                    TabControl = _tabCache[0] ?? new CogsControl(_cogsVM);
                    break;
                case 1:
                    Header = "Profit & Loss";
                    TabControl = _tabCache[1] ?? new PAndLControl(_plVM);
                    break;
                case -1:
                    //DisplayItemsNotFound();
                    break;
            }
        }
    }
}
