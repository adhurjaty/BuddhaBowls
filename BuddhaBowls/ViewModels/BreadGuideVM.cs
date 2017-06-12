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

namespace BuddhaBowls
{
    public class BreadGuideVM : TabVM
    {
        private BreadGuideControl _control;
        private BackgroundWorker _worker;

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
                int dayDiff = -MainHelper.Mod((int)value.DayOfWeek - 1, 7);
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

        #endregion

        #region ICommand and CanExecute

        public ICommand SquareCommand { get; set; }

        #endregion

        public BreadGuideVM() : base()
        {
            BreadOrderDate = DateTime.Today;
            SquareCommand = new RelayCommand(UpdateSquare);
            InitSquareSales();
        }

        #region ICommand Helpers

        private void UpdateSquare(object obj)
        {
            InitSquareSales();
            //SquareService service = new SquareService();
            //service.ListTransactions(DateTime.Now.AddDays(-1), DateTime.Now);
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
            BreadOrderList[idx].Update();
            BreadOrderList[idx].UpdateProperties();
            if (idx > 0)
                BreadOrderList[idx - 1].UpdateProperties();
        }

        #endregion

        private void _worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SquareProgMessage = "";
        }

        private void _worker_DoWork(object sender, DoWorkEventArgs e)
        {
            SquareService ss = new SquareService();
            Parallel.ForEach(BreadOrderList.Where(x => x.Date < DateTime.Now), order =>
            {
                try
                {
                    order.GrossSales = ss.ListTransactions(order.Date, order.Date.AddDays(1)).Sum(x => x.TotalCollected);
                    order.Update();
                }
                catch (Exception ex)
                {
                    order.GrossSales = 0;
                }
            });
        }
    }
}
