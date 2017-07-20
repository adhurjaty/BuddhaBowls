using BuddhaBowls.Models;
using BuddhaBowls.Services;
using BuddhaBowls.Square;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace BuddhaBowls
{
    public class ProfitLossVM : TabVM
    {
        private BackgroundWorker _worker;

        #region Content Binders

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

        private ObservableCollection<PAndLSummarySection> _summarySections;
        public ObservableCollection<PAndLSummarySection> SummarySections
        {
            get
            {
                return _summarySections;
            }
            set
            {
                _summarySections = value;
                NotifyPropertyChanged("SummarySections");
            }
        }
        #endregion

        #region ICommand and CanExecute

        public ICommand SquareCommand { get; set; }

        #endregion

        public ProfitLossVM()
        {
            PeriodSelector = new PeriodSelectorVM(_models, SwitchedPeriod, hasShowAll: false);
            SquareCommand = new RelayCommand(UpdateSquare);

            SummarySections = new ObservableCollection<PAndLSummarySection>(new PAndLSummarySection[5]);
        }

        #region ICommand Helpers

        private void UpdateSquare(object obj)
        {
            CalculatePAndL();
        }

        #endregion

        #region Initializers

        #endregion

        #region UI Updaters

        public void SwitchedPeriod(PeriodMarker period, WeekMarker week)
        {
            CalculatePAndL();
        }

        private void CalculatePAndL()
        {
            _worker = new BackgroundWorker();
            _worker.DoWork += _worker_DoWork;
            _worker.RunWorkerCompleted += _worker_RunWorkerCompleted;
            _worker.WorkerSupportsCancellation = true;

            SquareProgMessage = "Updating from Square...";
            _worker.RunWorkerAsync();

        }
        #endregion

        private void _worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            PAndLSummarySection section = (PAndLSummarySection)e.Result;

            SummarySections[0] = section;
            FillSummarySections();
            SquareProgMessage = "";
        }

        private void FillSummarySections()
        {
            NotifyPropertyChanged("SummarySections");
        }

        private void _worker_DoWork(object sender, DoWorkEventArgs e)
        {
            SquareService ss = new SquareService();
            List<SquareSale>[] weeklySales = new List<SquareSale>[PeriodSelector.SelectedWeek.Period];
            List<WeekMarker> weeksInPTD = PeriodSelector.WeekList.Take(PeriodSelector.SelectedWeek.Period).ToList();
            Parallel.For(0, weeksInPTD.Count, i =>
            {
                try
                {
                    WeekMarker week = weeksInPTD[i];
                    weeklySales[i] = ss.ListTransactions(week.StartDate, week.EndDate).ToList();
                }
                catch (Exception ex)
                {
                    weeklySales[i] = null;
                }
            });

            Dictionary<string, float> periodRevenueDict = new Dictionary<string, float>();
            Dictionary<string, float> weekRevenueDict = new Dictionary<string, float>();

            Dictionary<string, string> itemCategoryCache = new Dictionary<string, string>();

            List<Recipe> soldItems = _models.Recipes.Where(x => !x.IsBatch).ToList();

            for (int i = 0; i < weeklySales.Length; i++)
            {
                if (weeklySales[i] != null)
                {
                    foreach (SquareSale sale in weeklySales[i])
                    {
                        foreach (SquareItemization itemization in sale.Itemizations)
                        {
                            if (!itemCategoryCache.ContainsKey(itemization.Name))
                            {
                                Recipe matchingRec = soldItems.FirstOrDefault(x => x.Name == itemization.Name);
                                if (matchingRec != null)
                                    itemCategoryCache[itemization.Name] = matchingRec.Category;
                                else
                                    itemCategoryCache[itemization.Name] = "Other";
                            }
                            if (!periodRevenueDict.ContainsKey(itemCategoryCache[itemization.Name]))
                                periodRevenueDict[itemCategoryCache[itemization.Name]] = 0;
                            periodRevenueDict[itemCategoryCache[itemization.Name]] += itemization.NetTotal;

                            if (i == PeriodSelector.SelectedWeek.Period - 1)
                            {
                                if (!weekRevenueDict.ContainsKey(itemCategoryCache[itemization.Name]))
                                    weekRevenueDict[itemCategoryCache[itemization.Name]] = 0;
                                weekRevenueDict[itemCategoryCache[itemization.Name]] += itemization.NetTotal;
                            }
                        }
                    }
                }
            }

            float weekTotal = weekRevenueDict.Sum(x => x.Value);
            float periodTotal = periodRevenueDict.Sum(x => x.Value);

            List<PAndLSummaryItem> revenueItems = new List<PAndLSummaryItem>();
            foreach (string key in periodRevenueDict.Keys)
            {
                revenueItems.Add(new PAndLSummaryItem()
                {
                    Name = key,
                    WeekSales = weekRevenueDict[key],
                    WeekPSales = weekRevenueDict[key] / weekTotal,
                    PeriodSales = periodRevenueDict[key],
                    PeriodPSales = periodRevenueDict[key] / weekTotal
                });
            }

            e.Result = new PAndLSummarySection("Sales", PeriodSelector.SelectedWeek.Period, revenueItems);
        }
    }

    public class PAndLSummarySection
    {
        public string SummaryType { get; set; }
        public int WeekNumber { get; set; }
        public ObservableCollection<PAndLSummaryItem> Summaries { get; set; }

        public PAndLSummarySection(string sumType, int weekNum, IEnumerable<PAndLSummaryItem> items)
        {
            SummaryType = sumType;
            WeekNumber = weekNum;
            Summaries = new ObservableCollection<PAndLSummaryItem>(items);
        }
    }

    public class PAndLSummaryItem
    {
        public string Name { get; set; }
        public float WeekPSales { get; set; }
        public float WeekSales { get; set; }
        public float WeekPBudget { get; set; }
        public float WeekBudget { get; set; }
        public float WeekVar { get; set; }
        public float WeekPVar { get; set; }
        public float PeriodPSales { get; set; }
        public float PeriodSales { get; set; }
        public float PeriodPBudget { get; set; }
        public float PeriodBudget { get; set; }
        public float PeriodVar { get; set; }
        public float PeriodPVar { get; set; }
    }
}
