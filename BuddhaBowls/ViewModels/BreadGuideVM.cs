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
using System.Windows.Input;
using System.Threading;
using BuddhaBowls.Helpers;
using System.Windows;
using System.IO;

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

        private DateTime _breadOrderDate;
        public DateTime BreadOrderDate
        {
            get
            {
                return _breadOrderDate;
            }
            set
            {
                int dayDiff = -(((int)value.DayOfWeek - 1) & 7);
                DateTime breadDate = value.AddDays(dayDiff);
                DateErrorMessage = "";
                if (DBConnection && breadDate != _breadOrderDate)
                {
                    BreadOrder[] bWeek = _models.GetBreadWeek(value);
                    if (bWeek.Contains(null))
                    {
                        DateErrorMessage = "No records exist for that week";
                    }
                    else
                    {
                        _breadOrderDate = breadDate;
                        BreadOrderList = new ObservableCollection<BreadOrder>(bWeek);
                        if (_control != null)
                            _control.SetBreadGrid(BreadOrderList.ToArray(), _models.GetBreadTypes());
                        NotifyPropertyChanged("BreadOrderDate");
                        NotifyPropertyChanged("BreadOrderList");
                    }
                }
            }
        }

        private string _dateErrorMessage;
        public string DateErrorMessage
        {
            get
            {
                return _dateErrorMessage;
            }
            set
            {
                _dateErrorMessage = value;
                NotifyPropertyChanged("DateErrorMessage");
            }
        }
        #endregion

        #region ICommand and CanExecute

        #endregion

        public BreadGuideVM() : base()
        {
            BreadOrderDate = DateTime.Today;
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
            if (_control == null && DBConnection)
            {
                breadGuideControl.SetBreadGrid(BreadOrderList.ToArray(), _models.GetBreadTypes());
                NotifyPropertyChanged("BreadOrderList");
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
            BreadOrderList[idx].UpdateProperties();
            if (idx > 0)
                BreadOrderList[idx - 1].UpdateProperties();
        }

        #endregion
    }
}
