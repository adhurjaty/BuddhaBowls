using BuddhaBowls.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BuddhaBowls.UserControls;

namespace BuddhaBowls
{
    public class BreadGuideVM : TabVM
    {
        private BreadGuideControl _control;

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
            BreadOrderList = new ObservableCollection<BreadOrder>(_models.BreadWeek);
        }

        #region ICommand Helpers

        #endregion

        #region Initializers

        /// <summary>
        /// Sets up the data grid. Called from code behind
        /// </summary>
        /// <param name="breadGuideControl"></param>
        public void InitializeDataGrid(BreadGuideControl breadGuideControl)
        {
            if (_control == null)
            {
                breadGuideControl.SetBreadGrid(BreadOrderList.ToArray(),
                    _models.InventoryItems.Where(x => x.Category == "Bread").Select(x => x.Name).ToList());
            }
            _control = breadGuideControl;
        }

        //private void InitBreadDescriptors()
        //{
        //    _breadDescriptors = new ObservableCollection<BreadDescType>();
        //    foreach (string breadType in _models.InventoryItems.Where(x => x.Category == "Bread").Select(x => x.Name))
        //    {
        //        List<BreadDescriptor> descList = BreadOrderList.Where(x => x.BreadDescDict != null).Select(x => x.BreadDescDict[breadType]).ToList();
        //        for (int i = 0; i < 8 - descList.Count; i++)
        //        {
        //            descList.Add(new BreadDescriptor(BreadOrderList[i]));
        //        }
        //        _breadDescriptors.Add(new BreadDescType(breadType, descList));
        //    }
        //}

        #endregion

        #region UI Updaters

        public void UpdateValue(int idx)
        {
            BreadOrderList[idx].Update();
        }

        #endregion
    }
}
