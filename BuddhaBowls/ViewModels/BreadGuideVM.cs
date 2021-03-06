﻿using BuddhaBowls.Models;
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
using BuddhaBowls.Square;
using BuddhaBowls.Services;
using BuddhaBowls.Messengers;

namespace BuddhaBowls
{
    public class BreadGuideVM : TabVM
    {
        private BreadGuideControl _control;
        private BreadWeekContainer _breadWeek;
        private readonly int OFFSET_HOURS = 4;

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

        private PeriodSelectorVM _periodSelector;
        public PeriodSelectorVM PeriodSelector
        {
            get
            {
                return _periodSelector;
            }
            set
            {
                _periodSelector = value;
                NotifyPropertyChanged("PeriodSelector");
            }
        }

        private string _squareProgMessage;
        public string SquareProgMessage
        {
            get
            {
                return _squareProgMessage;
            }
            set
            {
                _squareProgMessage = value;
                NotifyPropertyChanged("SquareProgMessage");
            }
        }

        public int Backup
        {
            get
            {
                if (BreadOrderList != null)
                    return BreadOrderList[0].BreadDescDict.First().Value.Backup;
                return 0;
            }
            set
            {
                if(BreadOrderList != null)
                {
                    foreach (KeyValuePair<string, BreadDescriptor> kvp in BreadOrderList[0].BreadDescDict)
                    {
                        kvp.Value.Backup = value;
                    }

                    for (int i = 0; i < 7; i++)
                    {
                        BreadOrderList[i].Update();
                        UpdateValue(i);
                    }
                }
                NotifyPropertyChanged("Backup");
            }
        }
        #endregion

        #region ICommand and CanExecute

        public ICommand SquareCommand { get; set; }

        #endregion

        public BreadGuideVM() : base()
        {
            if(DBConnection)
            {
                PeriodSelector = new PeriodSelectorVM(_models, ChangeBreadWeek, hasShowAll: false);
                InitSquareSales();
                SquareCommand = new RelayCommand(UpdateSquare);
            }
        }

        #region ICommand Helpers

        private void UpdateSquare(object obj)
        {
            InitSquareSales();
        }

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

        private async void InitSquareSales()
        {
            SquareProgMessage = "Updating from Square...";
            await Task.Run(() =>
            {
                SquareService ss = new SquareService();
                Parallel.ForEach(BreadOrderList.Take(7).Where(x => x.Date < DateTime.Now), order =>
                {
                    try
                    {
                        DateTime startTime = order.Date.AddHours(OFFSET_HOURS);
                        order.GrossSales = ss.ListTransactions(startTime, startTime.AddDays(1)).Sum(x => x.GrossSales);
                        order.Update();
                    }
                    catch (Exception ex)
                    {
                        order.GrossSales = 0;
                    }
                });
                ((BreadOrderTotal)BreadOrderList[7]).UpdateDetails();
            });
            SquareProgMessage = "";
        }

        #endregion

        #region UI Updaters

        public void UpdateValue(int idx)
        {
            _breadWeek.Week[idx].Update();
            _breadWeek.Week[idx].UpdateProperties();

            ((BreadOrderTotal)_breadWeek.Week[7]).UpdateDetails();
            _breadWeek.Week[7].UpdateProperties();

            if (idx > 0)
                _breadWeek.Week[idx - 1].UpdateProperties();

            List<VendorInventoryItem> breads = _models.VIContainer.Items.Where(x => x.Category == "Bread").ToList();
            foreach (VendorInventoryItem item in breads)
            {
                BreadOrder bo = _breadWeek.Week.FirstOrDefault(x => x.Date.Date == DateTime.Today);
                if (bo != null && bo.BreadDescDict.ContainsKey(item.Name))
                {
                    BreadDescriptor bread = bo.BreadDescDict[item.Name];
                    item.Count = bread.BeginInventory + bread.FreezerCount;
                    item.Update();
                }
            }
            Messenger.Instance.NotifyColleagues(MessageTypes.VENDOR_INV_ITEMS_CHANGED);
            Messenger.Instance.NotifyColleagues(MessageTypes.BREAD_CHANGED, _breadWeek);
        }

        public void ChangeBreadWeek(PeriodMarker period, WeekMarker week)
        {
            _breadWeek = _models.GetBreadWeek(week);
            BreadOrderList = new ObservableCollection<BreadOrder>(_breadWeek.Week);
            if (_control != null)
                _control.SetBreadGrid(BreadOrderList.ToArray(), _models.GetBreadTypes());
            NotifyPropertyChanged("BreadOrderList");
        }
        #endregion

        public void UpdateBackup()
        {
            for (int i = 0; i < 7; i++)
            {
                BreadOrderList[i].Update();
                UpdateValue(i);
            }
        }
    }
}
