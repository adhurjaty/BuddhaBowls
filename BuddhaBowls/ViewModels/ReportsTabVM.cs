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
        }

        #region ICommand Helpers

        #endregion

        #region Initializers

        #endregion

        #region UI Updaters

        #endregion

        protected override void ChangePageState(int pageIdx)
        {
            base.ChangePageState(pageIdx);

            switch (pageIdx)
            {
                case 0:
                    Header = "Cost of Goods Sold";
                    if (_cogsVM == null)
                        _cogsVM = new CogsVM();

                    TabControl = _tabCache[0] ?? new CogsControl(_cogsVM);
                    break;
                case 1:
                    Header = "Profit & Loss";
                    if (_plVM == null)
                        _plVM = new ProfitLossVM();

                    TabControl = _tabCache[1] ?? new PAndLControl(_plVM);
                    break;
                case -1:
                    //DisplayItemsNotFound();
                    break;
            }
        }

        /// <summary>
        /// Total cluge. Needed to update COGS when report tab is selected
        /// </summary>
        public void CalculateCogs()
        {
            _cogsVM.CalculateCogs(_cogsVM.PeriodSelector.SelectedWeek);
        }
    }
}
