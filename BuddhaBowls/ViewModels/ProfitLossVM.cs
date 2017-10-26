using BuddhaBowls.Models;
using BuddhaBowls.Services;
using BuddhaBowls.Square;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace BuddhaBowls
{
    public class ProfitLossVM : TabVM
    {
        private ReportsTabVM _reportsTab;
        private CancellationTokenSource _cts;
        private object _lock = new object();

        #region Content Binders

        public PeriodSelectorVM PeriodSelector
        {
            get
            {
                return _reportsTab.PeriodSelector;
            }
            set
            {
                _reportsTab.PeriodSelector = value;
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

        public ProfitLossVM(ReportsTabVM tabContext)
        {
            _reportsTab = tabContext;

            SquareCommand = new RelayCommand(UpdateSquare);
            SummarySections = new ObservableCollection<PAndLSummarySection>(new PAndLSummarySection[5]);
        }

        #region ICommand Helpers

        private void UpdateSquare(object obj)
        {
            CalculatePAndL(PeriodSelector.SelectedPeriod, PeriodSelector.SelectedWeek);
        }

        #endregion

        #region Initializers

        #endregion

        #region UI Updaters

        //public void SwitchedPeriod(PeriodMarker period, WeekMarker week)
        //{
        //    CalculatePAndL(period, week);
        //}

        public async void CalculatePAndL(PeriodMarker period, WeekMarker week)
        {
            if (GetLastUpdated(period, _models.DailySales) < period.EndDate || 
                _models.ExpenseItems.FirstOrDefault(x => x.Date == week.StartDate) == null)
            {
                SquareProgMessage = "Updating from Square...";

                if (_cts != null)
                {
                    _cts.Cancel();
                }
                _cts = new CancellationTokenSource();

                try
                {
                    Task<PAndLSummarySection> revenueTask = new Task<PAndLSummarySection>(() => CalculateRevenue(period, week));
                    revenueTask.Start();

                    SummarySections[0] = await revenueTask;
                }
                catch (OperationCanceledException e)
                {
                    return;
                }
                finally
                {
                    _cts = null;
                }

                SummarySections[1] = new PAndLSummarySection("Cost of Sales", week.Period, GetCogsSummaryItems());
                SummarySections[2] = new PAndLSummarySection("Payroll", week.Period, GetPayrollSummaryItems(period, week), true);
                SummarySections[3] = new PAndLSummarySection("Overhead Expense", week.Period, GetOverheadSummaryItems(period, week), true);
                SquareProgMessage = "";
            }
            else
            {
                SectionsFromDB(week);
            }

            // TODO: populate payroll
            //_payrollWorker = new BackgroundWorker();
            //_payrollWorker.DoWork += _worker_DoWorkPayroll;
            //_payrollWorker.RunWorkerCompleted += _worker_RunWorkerCompletedPayroll;
            //_payrollWorker.WorkerSupportsCancellation = true;

            //SquareProgMessage = "Updating from Square...";
            //_payrollWorker.RunWorkerAsync(new object[] { period, week });

            //NotifyPropertyChanged("SummarySections");
        }

        #endregion

        private void SectionsFromDB(WeekMarker week)
        {
            Dictionary<string, List<ExpenseItem>> sectionDict = _models.ExpenseItems.GroupBy(x => x.ExpenseType)
                                                                       .ToDictionary(x => x.Key, x => x.OrderByDescending(y => y.WeekBudget).ToList());
            string[] labels = new string[] { "Sales", "Cost of Sales", "Payroll", "Overhead Expense" };
            for (int i = 0; i < labels.Length; i++)
            {
                SummarySections[i] = new PAndLSummarySection(labels[i], week.Period, sectionDict[labels[i]]);
            }
        }

        /// <summary>
        /// Gets the date when the last record was retrieved from API
        /// </summary>
        /// <param name="period">Time period to search</param>
        /// <param name="periodSales">Records of sales</param>
        /// <returns></returns>
        private DateTime GetLastUpdated(PeriodMarker period, List<DailySale> periodSales)
        {
            return periodSales.Count == 0 ? period.StartDate : periodSales.Max(x => x.LastUpdated).Date;
        }

        /// <summary>
        /// Gets an array of list of sales from Square API. Each array element corresponds to a day in the period
        /// </summary>
        /// <param name="period">Period of time to get sales</param>
        /// <param name="lastUpdated">Last full day of transaction records (partials do not count)</param>
        /// <returns></returns>
        private List<SquareSale>[] GetDailySales(PeriodMarker period, DateTime lastUpdated, CancellationToken token)
        {
            DateTime periodEndDate = new DateTime[] { DateTime.Now, period.EndDate }.Min();

            SquareService ss = new SquareService();
            // only call API for days where we have not already retrieved and saved a full day of sales
            int numDays = new int[] { (int)Math.Ceiling(periodEndDate.Subtract(lastUpdated).TotalDays), 0 }.Max();
            List<SquareSale>[] dailySales = new List<SquareSale>[numDays];

            try
            {
                Parallel.For(0, dailySales.Length, i =>
                {
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        DateTime startTime = lastUpdated.AddDays(i);
                        DateTime endTime = startTime.AddDays(1).AddSeconds(-1);
                        dailySales[i] = ss.ListTransactions(startTime, endTime).ToList();
                    }
                    catch (Exception ex)
                    {
                        dailySales[i] = null;
                    }
                });
            }
            catch(AggregateException e)
            {
                throw new OperationCanceledException(token);
            }

            return dailySales;
        }

        /// <summary>
        /// Calculates revenue from Square API and organizes to formatted display
        /// </summary>
        /// <param name="period">Selected period</param>
        /// <param name="week">Selected week</param>
        /// <returns>Object for displaying data</returns>
        private PAndLSummarySection CalculateRevenue(PeriodMarker period, WeekMarker week)
        {
            CancellationToken cancelToken = _cts.Token;
            //List<DailySale> periodSales = _models.DailySales.Where(x => period.StartDate <= x.Date && x.Date <= period.EndDate).ToList();

            List<ExpenseItem> revenueItems = new List<ExpenseItem>();

            DateTime lastUpdated = GetLastUpdated(period, _models.DailySales);

            List<SquareSale>[] dailySales = GetDailySales(period, lastUpdated, cancelToken);

            _models.ClearPrevDailySales(period.StartDate);

            // destroy period sale records that are partial days
            foreach (DailySale sale in _models.DailySales.Where(x => x.Date == lastUpdated))
            {
                sale.Destroy();
            }

            //periodSales = periodSales.Where(x => x.Date <= lastUpdated).ToList();
            //List<SquareSale> debugWeekSales = new SquareService().ListTransactions(week.StartDate, week.EndDate);
            //List<string> debugGroupedList = debugWeekSales.Where(x => x.TransactionTime.Date == DateTime.Today.AddDays(-1))
            //                                              .SelectMany(x => x.Itemizations)
            //                                              .GroupBy(x => x.Name)
            //                                              .Select(x => x.Key + " " + x.Sum(y => y.NetTotal).ToString()).ToList();

            // dictionary mapping sale category to revenue in that category for the period
            Dictionary<string, float> periodRevenueDict = new Dictionary<string, float>();
            // ditto the week
            Dictionary<string, float> weekRevenueDict = new Dictionary<string, float>();
            // dictionary mapping sale category to revenue in that category for period up to the week 
            Dictionary<string, float> prevPeriodRevenueDict = new Dictionary<string, float>();
            // dictionary mapping item name to category
            Dictionary<string, string> itemCategoryCache = new Dictionary<string, string>();

            // fill dictionaries with historical data from database
            foreach (DailySale sale in _models.DailySales.Where(x => x.Date < lastUpdated))
            {
                if (!itemCategoryCache.ContainsKey(sale.Name))
                    itemCategoryCache[sale.Name] = sale.Category;
                if (!periodRevenueDict.ContainsKey(sale.Category))
                {
                    periodRevenueDict[sale.Category] = 0;
                    weekRevenueDict[sale.Category] = 0;
                    prevPeriodRevenueDict[sale.Category] = 0;
                }
                periodRevenueDict[sale.Category] += sale.NetTotal;

                if (week.StartDate <= sale.Date && sale.Date <= week.EndDate)
                    weekRevenueDict[sale.Category] += sale.NetTotal;
                if (sale.Date < week.StartDate)
                    prevPeriodRevenueDict[sale.Category] += sale.NetTotal;
            }

            List<Recipe> soldItems = _models.RContainer.Items.Where(x => !x.IsBatch).ToList();
            List<DailySale>[] salesToSave = new List<DailySale>[dailySales.Length];

            for (int i = 0; i < dailySales.Length; i++)
            {
                cancelToken.ThrowIfCancellationRequested();
                salesToSave[i] = new List<DailySale>();

                if (dailySales[i] != null)
                {
                    foreach (SquareSale sale in dailySales[i])
                    {
                        foreach (SquareItemization itemization in sale.Itemizations)
                        {
                            cancelToken.ThrowIfCancellationRequested();
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

                            if (week.StartDate <= sale.TransactionTime && sale.TransactionTime <= week.EndDate)
                            {
                                if (!weekRevenueDict.ContainsKey(itemCategoryCache[itemization.Name]))
                                    weekRevenueDict[itemCategoryCache[itemization.Name]] = 0;
                                weekRevenueDict[itemCategoryCache[itemization.Name]] += itemization.NetTotal;
                            }
                            if (sale.TransactionTime < week.StartDate)
                            {
                                if (!prevPeriodRevenueDict.ContainsKey(itemCategoryCache[itemization.Name]))
                                    prevPeriodRevenueDict[itemCategoryCache[itemization.Name]] = 0;
                                prevPeriodRevenueDict[itemCategoryCache[itemization.Name]] += itemization.NetTotal;
                            }

                            DailySale existingSale = salesToSave[i].FirstOrDefault(x => x.Name == itemization.Name);
                            if (existingSale != null)
                            {
                                existingSale.Quantity += itemization.Quantity;
                                existingSale.NetTotal += itemization.NetTotal;
                                existingSale.Date = sale.TransactionTime;
                            }
                            else
                            {
                                salesToSave[i].Add(new DailySale()
                                {
                                    Name = itemization.Name,
                                    Category = itemCategoryCache[itemization.Name],
                                    Quantity = itemization.Quantity,
                                    NetTotal = itemization.NetTotal,
                                    Date = sale.TransactionTime
                                });
                            }
                        }
                    }
                }
            }

            // save the results
            foreach (List<DailySale> sales in salesToSave)
            {
                foreach (DailySale sale in sales)
                {
                    lock (_lock)
                        sale.Insert();
                }
            }

            //List<SquareSale> debugSales = new SquareService().ListTransactions(period.StartDate, period.EndDate);
            float weekTotal = weekRevenueDict.Sum(x => x.Value);
            float prevPeriodTotal = prevPeriodRevenueDict.Sum(x => x.Value);

            ExpenseItem ei;
            foreach (string key in periodRevenueDict.Keys)
            {
                ei = new ExpenseItem()
                {
                    Name = key,
                    WeekSales = weekRevenueDict[key],
                    WeekPSales = weekRevenueDict[key] / weekTotal,
                    PrevPeriodSales = prevPeriodRevenueDict.ContainsKey(key) ? prevPeriodRevenueDict[key] : 0,
                    PeriodPSales = periodRevenueDict[key] / weekTotal,
                    Date = week.StartDate
                };
                revenueItems.Add(ei);
                ei.Insert();
            }
            ei = new ExpenseItem()
            {
                Name = "Total",
                WeekSales = weekRevenueDict.Sum(x => x.Value),
                WeekPSales = 1,
                WeekPBudget = 1,
                PrevPeriodSales = prevPeriodRevenueDict.Sum(x => x.Value),
                PeriodPSales = 1,
                PeriodPBudget = 1,
                Date = week.StartDate
            };
            revenueItems.Add(ei);
            ei.Insert();

            return new PAndLSummarySection("Sales", week.Period, revenueItems);
        }

        //private void _worker_RunWorkerCompletedPayroll(object sender, RunWorkerCompletedEventArgs e)
        //{
        //    //SummarySections[2] = (PAndLSummarySection)e.Result;
        //}

        //private void _worker_DoWorkPayroll(object sender, DoWorkEventArgs e)
        //{
        //    PeriodMarker period = (PeriodMarker)((object[])e.Argument)[0];
        //    WeekMarker week = (WeekMarker)((object[])e.Argument)[1];

        //    //e.Result = new PAndLSummarySection("Payroll", PeriodSelector.SelectedWeek.Period, );
        //}


        public void FillEditableItem(PAndLSummarySection section, ExpenseItem item)
        {
            ExpenseItem totalSales = section.Summaries.Last();
            if (totalSales.WeekSales != 0)
                item.WeekPSales = item.WeekSales / totalSales.WeekSales;
            else
                item.WeekPSales = 0;

            if (totalSales.WeekBudget != 0)
                item.WeekPBudget = item.WeekBudget / totalSales.WeekBudget;
            else
                item.WeekPBudget = 0;

            if (totalSales.PeriodSales != 0)
                item.PeriodPSales = item.PeriodSales / totalSales.PeriodSales;
            else
                item.PeriodPSales = 0;

            if (totalSales.PeriodBudget != 0)
                item.PeriodPBudget = item.PeriodBudget / totalSales.PeriodBudget;
            else
                item.PeriodPBudget = 0;

            item.Update();
        }

        private List<ExpenseItem> GetCogsSummaryItems()
        {
            List<ExpenseItem> summary = new List<ExpenseItem>();

            float totalWeekCogs = _reportsTab.WeekCogs.Sum(x => x.CogsCost);
            float totalPerCogs = _reportsTab.PeriodCogs.Sum(x => x.CogsCost);

            float foodCogs = _reportsTab.WeekCogs.First(x => x.Name == "Food Total").CogsCost;
            float perFoodCogs = _reportsTab.PeriodCogs.First(x => x.Name == "Food Total").CogsCost;
            DateTime weekStart = _reportsTab.PeriodSelector.SelectedWeek.StartDate;
            summary.Add(new ExpenseItem()
            {
                Name = "Food",
                WeekPSales = foodCogs / totalWeekCogs,
                WeekSales = foodCogs,
                PeriodPSales = perFoodCogs / totalPerCogs,
                PrevPeriodSales = perFoodCogs - foodCogs,
                Date = weekStart
            });
            float bevCogs = _reportsTab.WeekCogs.First(x => x.Name == "Beverage").CogsCost;
            float perBevCogs = _reportsTab.PeriodCogs.First(x => x.Name == "Beverage").CogsCost;
            summary.Add(new ExpenseItem()
            {
                Name = "Beverage",
                WeekPSales = bevCogs / totalWeekCogs,
                WeekSales = bevCogs,
                PeriodPSales = perBevCogs / totalPerCogs,
                PrevPeriodSales = perBevCogs - bevCogs,
                Date = weekStart
            });
            float paperCogs = _reportsTab.WeekCogs.First(x => x.Name == "Paper Goods").CogsCost;
            float perPaperCogs = _reportsTab.PeriodCogs.First(x => x.Name == "Paper Goods").CogsCost;
            summary.Add(new ExpenseItem()
            {
                Name = "Paper Goods",
                WeekPSales = paperCogs / totalWeekCogs,
                WeekSales = paperCogs,
                PeriodPSales = perPaperCogs / totalPerCogs,
                PrevPeriodSales = perPaperCogs - paperCogs,
                Date = weekStart
            });

            return summary;
        }

        private IEnumerable<ExpenseItem> GetPayrollSummaryItems(PeriodMarker period, PeriodMarker week)
        {
            ExpenseItem totalSales = SummarySections[0].Summaries.Last();
            List<ExpenseItem> periodPayrolls = _models.ExpenseItems.Where(x => x.ExpenseType == "Payroll" &&
                                                                            PeriodSelector.SelectedPeriod.StartDate <= x.Date &&
                                                                            x.Date < week.EndDate).ToList();

            List<ExpenseItem> summary = new List<ExpenseItem>();

            string[] userDefFields = new string[] { "Payroll", "Payroll Taxes", "Benefits & Expenditures" };
            foreach (string key in userDefFields)
            {
                IEnumerable<ExpenseItem> pastItems = periodPayrolls.Where(x => x.Name == key).OrderByDescending(x => x.Date);
                ExpenseItem latestItem = pastItems.FirstOrDefault() ?? new ExpenseItem("Payroll", key, week.StartDate);
                IEnumerable<ExpenseItem> prevWeeks = pastItems.Skip(1);
                latestItem.PrevPeriodSales = prevWeeks.Sum(x => x.WeekSales);
                latestItem.PrevPeriodBudget = prevWeeks.Sum(x => x.WeekBudget);

                if (totalSales.PeriodPSales != 0)
                    latestItem.PeriodPSales = latestItem.PeriodSales / totalSales.PeriodSales;
                else
                    latestItem.PeriodPSales = 0;
                if (totalSales.PeriodPBudget != 0)
                    latestItem.PeriodPBudget = latestItem.PeriodBudget / totalSales.PeriodBudget;
                else
                    latestItem.PeriodPBudget = 0;

                summary.Add(latestItem);
            }

            summary.Add(new ExpenseItem("Payroll", "Total Payroll", week.StartDate)
            {
                WeekSales = summary.Sum(x => x.WeekSales),
                WeekBudget = summary.Sum(x => x.WeekBudget),
                PrevPeriodSales = summary.Sum(x => x.PrevPeriodSales),
                PrevPeriodBudget = summary.Sum(x => x.PrevPeriodBudget),
                PeriodPSales = summary.Sum(x => x.PeriodPSales),
                PeriodPBudget = summary.Sum(x => x.PeriodPBudget)
            });

            ExpenseItem totalCogs = SummarySections[1].Summaries.Last();
            ExpenseItem primeCost = new ExpenseItem("Payroll", "Prime Cost", week.StartDate)
            {
                WeekSales = summary.Last().WeekSales + totalCogs.WeekSales,
                WeekBudget = summary.Last().WeekBudget + totalCogs.WeekBudget,
                PrevPeriodSales = summary.Last().PrevPeriodSales + totalCogs.PrevPeriodSales,
                PrevPeriodBudget = summary.Last().PrevPeriodBudget + totalCogs.PrevPeriodBudget,
            };
            if (totalSales.PeriodPSales != 0)
                primeCost.PeriodPSales = primeCost.PeriodSales / totalSales.PeriodSales;
            else
                primeCost.PeriodPSales = 0;
            if (totalSales.PeriodPBudget != 0)
                primeCost.PeriodPBudget = primeCost.PeriodBudget / totalSales.PeriodBudget;
            else
                primeCost.PeriodPBudget = 0;

            summary.Add(primeCost);

            ExpenseItem profitAfter = new ExpenseItem("Payroll", "Profit after Prime Cost", week.StartDate)
            {
                WeekSales = totalSales.WeekSales - summary.Last().WeekSales,
                WeekBudget = totalSales.WeekBudget - summary.Last().WeekBudget,
                PrevPeriodSales = totalSales.PrevPeriodSales - summary.Last().PrevPeriodSales,
                PrevPeriodBudget = totalSales.PrevPeriodBudget - summary.Last().PrevPeriodBudget,
            };

            if (totalSales.PeriodPSales != 0)
                profitAfter.PeriodPSales = profitAfter.PeriodSales / totalSales.PeriodSales;
            else
                profitAfter.PeriodPSales = 0;
            if (totalSales.PeriodPBudget != 0)
                profitAfter.PeriodPBudget = profitAfter.PeriodBudget / totalSales.PeriodBudget;
            else
                profitAfter.PeriodPBudget = 0;

            summary.Add(profitAfter);

            return summary;
            //return new List<ExpenseItem>()
            //{
            //    new ExpenseItem("Payroll", PeriodSelector.Selec)
            //};
        }

        private IEnumerable<ExpenseItem> GetOverheadSummaryItems(PeriodMarker period, PeriodMarker week)
        {
            return new List<ExpenseItem>();
        }

    }

    public class PAndLSummarySection
    {
        public string SummaryType { get; set; }
        public int WeekNumber { get; set; }
        public bool CanEdit { get; set; }
        public ObservableCollection<ExpenseItem> Summaries { get; set; }

        public PAndLSummarySection(string sumType, int weekNum, IEnumerable<ExpenseItem> items, bool canEdit = false)
        {
            SummaryType = sumType;
            WeekNumber = weekNum;
            Summaries = new ObservableCollection<ExpenseItem>(items);
            CanEdit = canEdit;
        }
    }
}
