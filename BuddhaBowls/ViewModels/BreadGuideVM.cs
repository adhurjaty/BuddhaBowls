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
using BuddhaBowls.Square;
using BuddhaBowls.Services;

namespace BuddhaBowls
{
    public class BreadGuideVM : TabVM
    {
        private BreadGuideControl _control;
        private BackgroundWorker _worker;
        private BreadWeekContainer _breadWeek;

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

        private void InitSquareSales()
        {
            // TODO: Change to Async pattern
            _worker = new BackgroundWorker();
            _worker.DoWork += _worker_DoWork;
            _worker.RunWorkerCompleted += _worker_RunWorkerCompleted;
            _worker.WorkerSupportsCancellation = true;

            SquareProgMessage = "Updating from Square...";
            _worker.RunWorkerAsync();
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
                BreadOrder bo = _breadWeek.Week.FirstOrDefault(x => x.Date == DateTime.Today);
                if (bo != null && bo.BreadDescDict.ContainsKey(item.Name))
                {
                    BreadDescriptor bread = bo.BreadDescDict[item.Name];
                    VendorInventoryItem newItem = item;
                    newItem.Count = bread.BeginInventory + bread.FreezerCount;
                    //_models.AddUpdateInventoryItem(ref newItem);
                }
            }

            //ParentContext.ReportTab.UpdateBreadOrder();
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

        private void _worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SquareProgMessage = "";
        }

        private void _worker_DoWork(object sender, DoWorkEventArgs e)
        {
            SquareService ss = new SquareService();
            Parallel.ForEach(BreadOrderList.Take(7).Where(x => x.Date < DateTime.Now), order =>
            {
                try
                {
                    order.GrossSales = ss.ListTransactions(order.Date, order.Date.AddDays(1)).Sum(x => x.GrossSales);
                    order.Update();
                }
                catch (Exception ex)
                {
                    order.GrossSales = 0;
                }
            });
            ((BreadOrderTotal)BreadOrderList[7]).UpdateDetails();
        }

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
