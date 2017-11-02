using BuddhaBowls.Models;
using BuddhaBowls.Services;
using BuddhaBowls.Square;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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

        private bool _notUpdating = true;
        public bool NotUpdating
        {
            get
            {
                return _notUpdating;
            }
            set
            {
                _notUpdating = value;
                NotifyPropertyChanged("NotUpdating");
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
            DateTime lastUpdated = GetLastUpdated(period, _models.DailySales);
            List<ExpenseItem> existingItems = _models.ExpenseItems.Where(x => x.Date == week.StartDate).ToList();
            if (existingItems.Count == 0 || (lastUpdated < week.EndDate && existingItems.Count > 0))
            {
                SquareProgMessage = "Updating from Square...";
                NotUpdating = false;
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

                SummarySections[1] = new CogsPAndL(week.Period, GetCogsSummaryItems(), SummarySections[0].TotalSalesItem);
                SummarySections[2] = new PayrollPAndL(week.Period, GetPayrollSummaryItems(period, week),
                                                      SummarySections[0].TotalSalesItem, SummarySections[1].Summaries.First(x => x.Name == "Total"));
                SummarySections[3] = new OverheadPAndL(week.Period, GetOverheadSummaryItems(period, week), SummarySections[0].TotalSalesItem);

                foreach (PAndLSummarySection section in SummarySections)
                {
                    if (section != null)
                    {
                        if (existingItems.Count == 0)
                        {
                            section.Insert();
                            _models.ExpenseItems.AddRange(section.Summaries);
                        }
                        else
                        {
                            List<ExpenseItem> itemsInSection = existingItems.Where(x => x.ExpenseType == section.SummaryType).ToList();
                            foreach (ExpenseItem item in itemsInSection)
                            {
                                ExpenseItem shownItem = section.Summaries.First(x => x.Name == item.Name);
                                shownItem.WeekBudget = item.WeekBudget;
                                SetPrevsDB(period, week, ref shownItem);
                            }
                            section.Update(_models.ExpenseItems);
                            section.RefreshPercentages();
                            section.CommitChange();
                        }
                    }
                }
                SquareProgMessage = "";
                NotUpdating = true;
            }
            else
            {
                SectionsFromDB(period, week);
            }

            RefreshAllSummaries();

            // TODO: populate payroll
        }

        #endregion

        /// <summary>
        /// Sets item with previous sales and budget values from database
        /// </summary>
        private void SetPrevsDB(PeriodMarker period, WeekMarker week, ref ExpenseItem item)
        {
            string expenseType = item.ExpenseType;
            string name = item.Name;
            List<ExpenseItem> prevItems = _models.ExpenseItems.Where(x => x.Date >= period.StartDate && x.Date < week.StartDate &&
                                                                          x.ExpenseType == expenseType && x.Name == name).ToList();
            item.PrevPeriodBudget = prevItems.Sum(x => x.WeekBudget);
            item.PrevPeriodSales = prevItems.Sum(x => x.WeekSales);
        }

        private void SectionsFromDB(PeriodMarker period, WeekMarker week)
        {
            Dictionary<string, List<ExpenseItem>> sectionDict = _models.ExpenseItems.Where(x => x.Date.Date == week.StartDate)
                                                                                    .GroupBy(x => x.ExpenseType)
                                                                                    .ToDictionary(x => x.Key,
                                                                                                  x => x.OrderByDescending(y => OrderBySales(y))
                                                                                    .ToList());
            SalesPAndL sales = new SalesPAndL(week.Period, sectionDict["Sales"]);
            SummarySections[0] = sales;
            SummarySections[1] = new CogsPAndL(week.Period, sectionDict["Cost of Sales"], sales.TotalSalesItem);
            SummarySections[2] = new PayrollPAndL(week.Period, sectionDict["Payroll"], sales.TotalSalesItem,
                                                  SummarySections[1].Summaries.First(x => x.Name == "Total"));
            SummarySections[3] = new OverheadPAndL(week.Period, sectionDict["Overhead Expense"], sales.TotalSalesItem);

            //foreach (PAndLSummarySection section in SummarySections)
            //{
            //    section.RefreshPercentages();
            //}
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < SummarySections[i].Summaries.Count; j++)
                {
                    ExpenseItem item = SummarySections[i].Summaries[j];
                    SetPrevsDB(period, week, ref item);
                }
                SummarySections[i].RefreshPercentages();
            }
        }

        private float OrderBySales(ExpenseItem item)
        {
            if (item.Name.Contains("Profit"))
                return -3;
            if (item.Name == "Prime Cost")
                return -2;
            if (item.Name.Contains("Total"))
                return -1;
            return item.WeekSales;
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

            DateTime lastUpdated = GetLastUpdated(period, _models.DailySales.Where(x => period.StartDate <= x.Date && x.Date < period.EndDate)
                                                                 .ToList());

            List<SquareSale>[] dailySales = GetDailySales(period, lastUpdated, cancelToken);

            _models.ClearPrevDailySales(period.StartDate);

            // destroy period sale records that are partial days
            foreach (DailySale sale in _models.DailySales.Where(x => x.Date.Date == lastUpdated))
            {
                sale.Destroy();
            }

            // for debugging
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

            foreach (string key in periodRevenueDict.Keys)
            {
                revenueItems.Add(new ExpenseItem()
                {
                    Name = key,
                    WeekSales = weekRevenueDict[key],
                    WeekPSales = weekRevenueDict[key] / weekTotal,
                    PrevPeriodSales = prevPeriodRevenueDict.ContainsKey(key) ? prevPeriodRevenueDict[key] : 0,
                    PeriodPSales = periodRevenueDict[key] / weekTotal,
                    Date = week.StartDate,
                });
            }
            revenueItems.Add(new ExpenseItem()
            {
                Name = "Total",
                WeekSales = weekRevenueDict.Sum(x => x.Value),
                WeekPSales = 1,
                WeekPBudget = 1,
                PrevPeriodSales = prevPeriodRevenueDict.Sum(x => x.Value),
                PeriodPSales = 1,
                PeriodPBudget = 1,
                Date = week.StartDate
            });

            return new SalesPAndL(week.Period, revenueItems);
        }

        /// <summary>
        /// Method called after editing item in the data grid
        /// </summary>
        /// <param name="section"></param>
        /// <param name="item"></param>
        public void FillEditableItem(PAndLSummarySection section, ExpenseItem item)
        {
            section.UpdateItem(item);
            section.CommitChange();

            RefreshAllSummaries();

            item.Update();
            //if (section.SummaryType == "Sales")
            //    section.TotalSalesItem.Update();
        }

        private void RefreshAllSummaries()
        {
            foreach (PAndLSummarySection sum in SummarySections)
            {
                if (sum != null)
                {
                    sum.UpdateTotal();
                    sum.RefreshPercentages();
                    sum.CommitChange();
                }
            }
        }

        private List<ExpenseItem> GetCogsSummaryItems()
        {
            List<ExpenseItem> summary = new List<ExpenseItem>();

            ExpenseItem foodSalesItem = SummarySections[0].Summaries.First(x => x.Name == "Food");
            ExpenseItem bevSalesItem = SummarySections[0].Summaries.First(x => x.Name == "Beverage");
            ExpenseItem totalSalesItem = SummarySections[0].Summaries.First(x => x.Name == "Total");
            float foodWeekSales = foodSalesItem.WeekSales;

            float totalWeekCogs = _reportsTab.WeekCogs.First(x => x.Name == "Total").CogsCost;
            float totalPerCogs = _reportsTab.PeriodCogs.First(x => x.Name == "Total").CogsCost;

            float foodCogs = _reportsTab.WeekCogs.First(x => x.Name == "Food Total").CogsCost;
            float perFoodCogs = _reportsTab.PeriodCogs.First(x => x.Name == "Food Total").CogsCost;
            DateTime weekStart = _reportsTab.PeriodSelector.SelectedWeek.StartDate;
            summary.Add(new ExpenseItem()
            {
                Name = "Food",
                WeekPSales = foodCogs / foodSalesItem.WeekSales,
                WeekSales = foodCogs,
                PeriodPSales = perFoodCogs / foodSalesItem.PeriodSales,
                PrevPeriodSales = perFoodCogs - foodCogs,
                Date = weekStart
            });
            float bevCogs = _reportsTab.WeekCogs.First(x => x.Name == "Beverage").CogsCost;
            float perBevCogs = _reportsTab.PeriodCogs.First(x => x.Name == "Beverage").CogsCost;
            summary.Add(new ExpenseItem()
            {
                Name = "Beverage",
                WeekPSales = bevCogs / bevSalesItem.WeekSales,
                WeekSales = bevCogs,
                PeriodPSales = perBevCogs / bevSalesItem.PeriodSales,
                PrevPeriodSales = perBevCogs - bevCogs,
                Date = weekStart
            });
            float paperCogs = _reportsTab.WeekCogs.First(x => x.Name == "Paper Goods").CogsCost;
            float perPaperCogs = _reportsTab.PeriodCogs.First(x => x.Name == "Paper Goods").CogsCost;
            summary.Add(new ExpenseItem()
            {
                Name = "Paper Goods",
                WeekPSales = paperCogs / totalSalesItem.WeekSales,
                WeekSales = paperCogs,
                PeriodPSales = perPaperCogs / totalSalesItem.PeriodSales,
                PrevPeriodSales = perPaperCogs - paperCogs,
                Date = weekStart
            });
            ExpenseItem totalExpenseItem = new ExpenseItem()
            {
                Name = "Total",
                WeekPSales = totalWeekCogs / totalSalesItem.WeekSales,
                WeekSales = totalWeekCogs,
                PeriodPSales = totalPerCogs / totalSalesItem.PeriodSales,
                PrevPeriodSales = totalPerCogs - totalWeekCogs,
                Date = weekStart
            };
            summary.Add(totalExpenseItem);
            summary.Add(new ExpenseItem()
            {
                Name = "Gross Profit",
                WeekPSales = 1 - totalExpenseItem.WeekPSales,
                WeekSales = totalSalesItem.WeekSales - totalExpenseItem.WeekSales,
                PeriodPSales = 1 - totalExpenseItem.PeriodPSales,
                PrevPeriodSales = totalSalesItem.PrevPeriodSales - totalExpenseItem.PrevPeriodSales,
                Date = weekStart
            });

            return summary;
        }

        private IEnumerable<ExpenseItem> GetPayrollSummaryItems(PeriodMarker period, PeriodMarker week)
        {
            ExpenseItem totalSales = SummarySections[0].Summaries.First(x => x.Name == "Total");
            List<ExpenseItem> periodPayrolls = _models.ExpenseItems.Where(x => x.ExpenseType == "Payroll" &&
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
        }

        private IEnumerable<ExpenseItem> GetOverheadSummaryItems(PeriodMarker period, PeriodMarker week)
        {
            string expenseType = "Overhead Expense";
            ExpenseItem totalSales = SummarySections[0].Summaries.First(x => x.Name == "Total");
            List<ExpenseItem> periodOverhead = _models.ExpenseItems.Where(x => x.ExpenseType == expenseType &&
                                                                            x.Date < week.EndDate).ToList();

            List<ExpenseItem> summary = new List<ExpenseItem>();

            string[] userDefFields = new string[] { "Credit Card Processing", "Direct Operating", "Administrative", "Advertising and Promotion",
                                                    "Repairs & Maintenance", "Occupancy Cost" };
            foreach (string key in userDefFields)
            {
                // if a current record for this date exists, set the item to it
                IEnumerable<ExpenseItem> pastItems = periodOverhead.Where(x => x.Name == key).OrderByDescending(x => x.Date);
                ExpenseItem latestItem = pastItems.FirstOrDefault() ?? new ExpenseItem(expenseType, key, week.StartDate);
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

            summary.Add(new ExpenseItem(expenseType, "Total Overhead Expense", week.StartDate)
            {
                WeekSales = summary.Sum(x => x.WeekSales),
                WeekBudget = summary.Sum(x => x.WeekBudget),
                PrevPeriodSales = summary.Sum(x => x.PrevPeriodSales),
                PrevPeriodBudget = summary.Sum(x => x.PrevPeriodBudget),
                PeriodPSales = summary.Sum(x => x.PeriodPSales),
                PeriodPBudget = summary.Sum(x => x.PeriodPBudget)
            });

            return summary;
        }

    }

    public class PAndLSummarySection : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string SummaryType { get; set; }
        public int WeekNumber { get; set; }
        public bool CanEdit { get; set; }
        private ObservableCollection<ExpenseItem> _summaries;
        public ObservableCollection<ExpenseItem> Summaries
        {
            get
            {
                return _summaries;
            }
            set
            {
                _summaries = value;
                NotifyPropertyChanged("Summaries");
            }
        }
        public ExpenseItem TotalSalesItem { get; set; }

        public PAndLSummarySection(string sumType, int weekNum, IEnumerable<ExpenseItem> items, ExpenseItem totalSalesItem, bool canEdit = false)
        {
            SummaryType = sumType;
            WeekNumber = weekNum;
            Summaries = new ObservableCollection<ExpenseItem>(items);
            foreach (ExpenseItem item in Summaries)
            {
                item.ExpenseType = sumType;
            }
            TotalSalesItem = totalSalesItem;
            CanEdit = canEdit;
        }

        public virtual void CommitChange()
        {
            Summaries = new ObservableCollection<ExpenseItem>(Summaries);
        }

        public virtual void UpdateItem(ExpenseItem item)
        {
            RefreshPercentages();
        }

        public void RefreshPercentages()
        {
            foreach (ExpenseItem item in Summaries)
            {
                if (TotalSalesItem.WeekSales != 0)
                    item.WeekPSales = item.WeekSales / TotalSalesItem.WeekSales;
                else
                    item.WeekPSales = 0;

                if (TotalSalesItem.WeekBudget != 0)
                    item.WeekPBudget = item.WeekBudget / TotalSalesItem.WeekBudget;
                else
                    item.WeekPBudget = 0;

                if (TotalSalesItem.PeriodSales != 0)
                    item.PeriodPSales = item.PeriodSales / TotalSalesItem.PeriodSales;
                else
                    item.PeriodPSales = 0;

                if (TotalSalesItem.PeriodBudget != 0)
                    item.PeriodPBudget = item.PeriodBudget / TotalSalesItem.PeriodBudget;
                else
                    item.PeriodPBudget = 0;
            }
        }

        public void Insert()
        {
            foreach (ExpenseItem item in Summaries)
            {
                item.Insert();
            }
        }

        public void Update()
        {
            foreach (ExpenseItem item in Summaries)
            {
                item.Update();
            }
        }

        public void Update(List<ExpenseItem> existingItems)
        {
            foreach (ExpenseItem item in Summaries)
            {
                int idx = existingItems.FindIndex(x => x.Name == item.Name && x.ExpenseType == item.ExpenseType && x.Date.Date == item.Date.Date);
                if(idx != -1)
                {
                    item.Id = existingItems[idx].Id;
                    existingItems[idx] = item;
                    item.Update();
                }
            }
        }

        public virtual void UpdateTotal() { }
    }

    public class SalesPAndL : PAndLSummarySection
    {

        public SalesPAndL(int weekNum, IEnumerable<ExpenseItem> items) : base("Sales", weekNum, items, items.First(x => x.Name == "Total"))
        {

        }

        public override void UpdateItem(ExpenseItem item)
        {
            UpdateTotal();
            if (item.Name != "Total")
            {
                base.UpdateItem(item);
            }
        }

        public override void UpdateTotal()
        {
            TotalSalesItem.WeekSales = Summaries.Take(Summaries.Count - 1).Sum(x => x.WeekSales);
            TotalSalesItem.WeekBudget = Summaries.Take(Summaries.Count - 1).Sum(x => x.WeekBudget);
        }
    }

    public class CogsPAndL : PAndLSummarySection
    {

        public CogsPAndL(int weekNum, IEnumerable<ExpenseItem> items, ExpenseItem totalSales) : base("Cost of Sales", weekNum, items, totalSales)
        {

        }

        public override void UpdateItem(ExpenseItem item)
        {
            UpdateTotal();
            if(item.Name != "Total" && item.Name != "Gross Profit")
                base.UpdateItem(item);
        }

        public override void UpdateTotal()
        {
            ExpenseItem totalItem = Summaries.First(x => x.Name == "Total");
            ExpenseItem profitItem = Summaries.First(x => x.Name == "Gross Profit");
            List<ExpenseItem> otherItems = Summaries.Where(x => x.Name != "Total" && x.Name != "Gross Profit").ToList();
            totalItem.WeekSales = otherItems.Sum(x => x.WeekSales);
            totalItem.WeekBudget = otherItems.Sum(x => x.WeekBudget);
            profitItem.WeekSales = TotalSalesItem.WeekSales - totalItem.WeekSales;
            profitItem.WeekBudget = TotalSalesItem.WeekBudget - totalItem.WeekBudget;
        }
    }

    public class PayrollPAndL : PAndLSummarySection
    {
        private ExpenseItem _totalCogsItem;

        public PayrollPAndL(int weekNum, IEnumerable<ExpenseItem> items, ExpenseItem totalSales, ExpenseItem totalCogs)
            : base("Payroll", weekNum, items, totalSales, true)
        {
            _totalCogsItem = totalCogs;
        }

        public override void UpdateItem(ExpenseItem item)
        {
            UpdateTotal();
            if(!new string[] { "Total Payroll", "Prime Cost", "Profit after Prime Cost" }.Contains(item.Name))
                base.UpdateItem(item);
        }

        public override void UpdateTotal()
        {
            ExpenseItem totalItem = Summaries.First(x => x.Name == "Total Payroll");
            ExpenseItem primeCostItem = Summaries.First(x => x.Name == "Prime Cost");
            ExpenseItem profitItem = Summaries.First(x => x.Name == "Profit after Prime Cost");
            List<ExpenseItem> otherItems = Summaries.Where(x => x != totalItem && x != primeCostItem && x != profitItem).ToList();
            totalItem.WeekSales = otherItems.Sum(x => x.WeekSales);
            totalItem.WeekBudget = otherItems.Sum(x => x.WeekBudget);
            primeCostItem.WeekSales = totalItem.WeekSales + _totalCogsItem.WeekSales;
            primeCostItem.WeekBudget = totalItem.WeekBudget + _totalCogsItem.WeekBudget;
            profitItem.WeekSales = TotalSalesItem.WeekSales - primeCostItem.WeekSales;
            profitItem.WeekBudget = TotalSalesItem.WeekBudget - primeCostItem.WeekBudget;
        }
    }

    public class OverheadPAndL : PAndLSummarySection
    {

        public OverheadPAndL(int weekNum, IEnumerable<ExpenseItem> items, ExpenseItem totalSales)
            : base("Overhead Expense", weekNum, items, totalSales, true)
        {

        }

        public override void UpdateItem(ExpenseItem item)
        {
            UpdateTotal();
            if(!item.Name.Contains("Total"))
                base.UpdateItem(item);
        }

        public override void UpdateTotal()
        {
            ExpenseItem totalItem = Summaries.First(x => x.Name.Contains("Total"));
            totalItem.WeekSales = Summaries.Take(Summaries.Count - 1).Sum(x => x.WeekSales);
            totalItem.WeekBudget = Summaries.Take(Summaries.Count - 1).Sum(x => x.WeekBudget);
        }
    }
}
