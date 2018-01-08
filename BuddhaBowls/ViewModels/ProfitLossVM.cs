using BuddhaBowls.Helpers;
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
        private Dictionary<string, float> _otherWeekSquare;
        private Dictionary<string, float> _otherPeriodSquare;

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
            //_models.InContainer.AddUpdateBinding(PopulateCogs);
            //_models.POContainer.AddUpdateBinding(PopulateCogs);

            _otherWeekSquare = new Dictionary<string, float>();
            _otherPeriodSquare = new Dictionary<string, float>();
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

        public async void CalculatePAndL(PeriodMarker period, WeekMarker week)
        {
            DateTime lastUpdated = GetLastUpdated(period, _models.DSContainer.Items);
            List<ExpenseItem> existingItems = _models.EIContainer.Items.Where(x => x.Date == week.StartDate).ToList();
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

                PopulateCogs(week);
                SummarySections[2] = new PayrollPAndL(week.Period, GetPayrollSummaryItems(period, week),
                                                      SummarySections[0].TotalSalesItem, SummarySections[1].Summaries.First(x => x.Name == "Total"));
                SummarySections[3] = new OverheadPAndL(week.Period, GetOverheadSummaryItems(period, week), SummarySections[0].TotalSalesItem);
                PopulateTakeaway(GetTakeawaySummaryItems(period, week), week);

                foreach (PAndLSummarySection section in SummarySections)
                {
                    if (section != null)
                    {
                        if (existingItems.Where(x => x.ExpenseType == section.SummaryType).Count() == 0)
                        {
                            section.Insert();
                            _models.EIContainer.AddItems(section.Summaries.ToList());
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
                            section.Update(_models.EIContainer.Items);
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

        public void PopulateCogs(WeekMarker week)
        {
            SummarySections[1] = new CogsPAndL(week.Period, GetCogsSummaryItems(week), SummarySections[0].TotalSalesItem);
            NotifyPropertyChanged("SummarySections");
        }

        public void PopulateCogs()
        {
            PopulateCogs(PeriodSelector.SelectedWeek);
        }

        private void PopulateTakeaway(List<ExpenseItem> items, WeekMarker week)
        {
            SummarySections[4] = new TakeawayPAndL(week.Period, items,
                                                    SummarySections[0].Summaries.First(x => x.Name == "Total"),
                                                    SummarySections[2].Summaries.First(x => x.Name == "Prime Cost"),
                                                    SummarySections[3].Summaries.First(x => x.Name.Contains("Total")));
            NotifyPropertyChanged("SummarySections");
        }

        /// <summary>
        /// Sets item with previous sales and budget values from database
        /// </summary>
        private void SetPrevsDB(PeriodMarker period, WeekMarker week, ref ExpenseItem item)
        {
            string expenseType = item.ExpenseType;
            string name = item.Name;
            List<ExpenseItem> prevItems = _models.EIContainer.Items.Where(x => x.Date >= period.StartDate && x.Date < week.StartDate &&
                                                                          x.ExpenseType == expenseType && x.Name == name).ToList();
            item.PrevPeriodBudget = prevItems.Sum(x => x.WeekBudget);
            item.PrevPeriodSales = prevItems.Sum(x => x.WeekSales);
        }

        private void SectionsFromDB(PeriodMarker period, WeekMarker week)
        {
            Dictionary<string, List<ExpenseItem>> sectionDict = _models.EIContainer.Items.Where(x => x.Date.Date == week.StartDate)
                                                                                        .GroupBy(x => x.ExpenseType)
                                                                                        .ToDictionary(x => x.Key,
                                                                                                      x => x.OrderByDescending(y => OrderBySales(y))
                                                                                        .ToList());
            SalesPAndL sales = new SalesPAndL(week.Period, sectionDict["Sales"]);
            SummarySections[0] = sales;
            if (sectionDict.ContainsKey("Cost of Sales"))
                SummarySections[1] = new CogsPAndL(week.Period, sectionDict["Cost of Sales"], sales.FoodTotal);
            else
                PopulateCogs(week);
            SummarySections[2] = new PayrollPAndL(week.Period, sectionDict["Payroll"], sales.FoodTotal,
                                                  SummarySections[1].Summaries.First(x => x.Name == "Total"));
            SummarySections[3] = new OverheadPAndL(week.Period, sectionDict["Overhead Expense"], sales.FoodTotal);

            // temporary check to create blank takeaway in case the other records exist and this type does not
            if (!sectionDict.ContainsKey("Takeaway"))
            {
                PopulateTakeaway(GetTakeawaySummaryItems(period, week), week);
                SummarySections[4].Insert();
            }
            else
            {
                PopulateTakeaway(sectionDict["Takeaway"], week);
            }

            for (int i = 0; i < SummarySections.Count; i++)
            {
                SummarySections[i].UpdateTotal();
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

            DateTime lastUpdated = GetLastUpdated(period, _models.DSContainer.GetSalesInPeriod(period));

            List<SquareSale>[] dailySales = GetDailySales(period, lastUpdated, cancelToken);

            _models.DSContainer.ClearPrevDailySales(period.StartDate);
            _models.DSContainer.DestroyPartialDays(lastUpdated);

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

            _otherWeekSquare["Credit Card Processing"] = 0;
            _otherPeriodSquare["Credit Card Processing"] = 0;
            // fill dictionaries with historical data from database
            foreach (DailySale sale in _models.DSContainer.Items.Where(x => x.Date < lastUpdated))
            {
                if (!itemCategoryCache.ContainsKey(sale.Name))
                    itemCategoryCache[sale.Name] = sale.Category;
                if (!periodRevenueDict.ContainsKey(sale.Category))
                {
                    periodRevenueDict[sale.Category] = 0;
                    weekRevenueDict[sale.Category] = 0;
                    prevPeriodRevenueDict[sale.Category] = 0;
                }
                periodRevenueDict[sale.Category] += sale.GrossTotal;

                if (week.StartDate <= sale.Date && sale.Date <= week.EndDate)
                {
                    weekRevenueDict[sale.Category] += sale.GrossTotal;
                    _otherWeekSquare["Credit Card Processing"] += sale.ChargeFee;
                }
                if (sale.Date < week.StartDate)
                {
                    prevPeriodRevenueDict[sale.Category] += sale.GrossTotal;
                    _otherPeriodSquare["Credit Card Processing"] += sale.ChargeFee;
                }
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
                            // average the charge fee per itemization
                            float chargeFee = sale.ChargeFee / sale.Itemizations.Count;
                            _otherWeekSquare["Credit Card Processing"] += chargeFee;

                            cancelToken.ThrowIfCancellationRequested();
                            if (!itemCategoryCache.ContainsKey(itemization.Name))
                            {
                                Recipe matchingRec = soldItems.FirstOrDefault(x => MainHelper.CompareStrings(x.Name, itemization.Name));
                                if (matchingRec != null)
                                    itemCategoryCache[itemization.Name] = matchingRec.Category;
                                else
                                    itemCategoryCache[itemization.Name] = "Other";
                            }
                            if (!periodRevenueDict.ContainsKey(itemCategoryCache[itemization.Name]))
                            {
                                periodRevenueDict[itemCategoryCache[itemization.Name]] = 0;
                                weekRevenueDict[itemCategoryCache[itemization.Name]] = 0;
                                prevPeriodRevenueDict[itemCategoryCache[itemization.Name]] = 0;
                            }
                            periodRevenueDict[itemCategoryCache[itemization.Name]] += itemization.NetTotal;

                            if (week.StartDate <= sale.TransactionTime && sale.TransactionTime <= week.EndDate)
                            {
                                weekRevenueDict[itemCategoryCache[itemization.Name]] += itemization.NetTotal;
                            }
                            if (sale.TransactionTime < week.StartDate)
                            {
                                prevPeriodRevenueDict[itemCategoryCache[itemization.Name]] += itemization.NetTotal;
                            }

                            DailySale existingSale = salesToSave[i].FirstOrDefault(x => x.Name == itemization.Name);
                            if (existingSale != null)
                            {
                                existingSale.Quantity += itemization.Quantity;
                                existingSale.GrossTotal += itemization.NetTotal;
                                existingSale.ChargeFee += chargeFee;
                                existingSale.Date = sale.TransactionTime;
                            }
                            else
                            {
                                salesToSave[i].Add(new DailySale()
                                {
                                    Name = itemization.Name,
                                    Category = itemCategoryCache[itemization.Name],
                                    Quantity = itemization.Quantity,
                                    GrossTotal = itemization.NetTotal,
                                    ChargeFee = chargeFee,
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

        public void OpenDetails(PAndLSummarySection section, ExpenseItem item)
        {
            ExpenseDetailVM tabVM;
            switch (item.Name)
            {
                case "Direct Operating":
                    tabVM = new DirectOpExpenseVM(section, item, PeriodSelector.SelectedPeriod, PeriodSelector.SelectedWeek);
                    tabVM.Add("Edit Expenses");
                    break;
                case "Administrative":
                    tabVM = new AdminExpenseVM(section, item, PeriodSelector.SelectedPeriod, PeriodSelector.SelectedWeek);
                    tabVM.Add("Edit Expenses");
                    break;
                case "Advertising and Promotion":
                    tabVM = new AdvertisingExpenseVM(section, item, PeriodSelector.SelectedPeriod, PeriodSelector.SelectedWeek);
                    tabVM.Add("Edit Expenses");
                    break;
            }
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

        private List<ExpenseItem> GetCogsSummaryItems(WeekMarker week)
        {
            List<ExpenseItem> summary = new List<ExpenseItem>();

            ExpenseItem foodSalesItem = SummarySections[0].Summaries.FirstOrDefault(x => x.Name == "Food") ??
                                        new ExpenseItem("Cost of Sales", "Food", week.StartDate);
            ExpenseItem bevSalesItem = SummarySections[0].Summaries.FirstOrDefault(x => x.Name == "Beverage") ??
                                        new ExpenseItem("Cost of Sales", "Beverage", week.StartDate);
            ExpenseItem totalSalesItem = SummarySections[0].Summaries.First(x => x.Name == "Total");
            float foodWeekSales = foodSalesItem.WeekSales;

            float totalWeekCogs = _reportsTab.WeekCogs.First(x => x.Name == "Total").CogsCost;
            float totalPerCogs = _reportsTab.PeriodCogs.First(x => x.Name == "Total").CogsCost;

            float foodCogs = _reportsTab.WeekCogs.First(x => x.Name == "Food Total").CogsCost;
            float perFoodCogs = _reportsTab.PeriodCogs.First(x => x.Name == "Food Total").CogsCost;
            DateTime weekStart = week.StartDate;
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

        private IEnumerable<ExpenseItem> GetPayrollSummaryItems(PeriodMarker period, WeekMarker week)
        {
            ExpenseItem totalSales = SummarySections[0].Summaries.First(x => x.Name == "Total");
            List<ExpenseItem> periodPayrolls = GetPeriodItems("Payroll", period, week);

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

        private IEnumerable<ExpenseItem> GetOverheadSummaryItems(PeriodMarker period, WeekMarker week)
        {
            string expenseType = "Overhead Expense";
            ExpenseItem totalSales = SummarySections[0].Summaries.First(x => x.Name == "Total");
            List<ExpenseItem> periodOverhead = GetPeriodItems(expenseType, period, week);

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

                if (_otherWeekSquare.ContainsKey(key))
                {
                    latestItem.WeekSales = _otherWeekSquare[key];
                    latestItem.PrevPeriodSales = _otherPeriodSquare[key];
                }

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

        private List<ExpenseItem> GetTakeawaySummaryItems(PeriodMarker period, WeekMarker week)
        {
            string expenseType = "Takeaway";
            ExpenseItem totalSales = SummarySections[0].Summaries.First(x => x.Name == "Total");
            ExpenseItem payrollPrime = SummarySections[2].Summaries.First(x => x.Name == "Prime Cost");
            ExpenseItem overheadTotal = SummarySections[3].Summaries.First(x => x.Name.Contains("Total"));

            List<ExpenseItem> periodTakeaways = GetPeriodItems(expenseType, period, week);

            List<ExpenseItem> summary = new List<ExpenseItem>();

            string[] userDefFields = new string[] { "Owners Draws", "Loan Payment", "Loan Tax" };
            foreach (string key in userDefFields)
            {
                // if a current record for this date exists, set the item to it
                IEnumerable<ExpenseItem> pastItems = periodTakeaways.Where(x => x.Name == key).OrderByDescending(x => x.Date);
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

            ExpenseItem totalTakeaway = new ExpenseItem(expenseType, "Net Operating Income", week.StartDate);
            totalTakeaway.WeekSales = totalSales.WeekSales - summary.Sum(x => x.WeekSales) - payrollPrime.WeekSales - overheadTotal.WeekSales;
            totalTakeaway.WeekBudget = totalSales.WeekBudget - summary.Sum(x => x.WeekBudget) - payrollPrime.WeekBudget - overheadTotal.WeekBudget;
            totalTakeaway.PrevPeriodSales = totalSales.PrevPeriodSales - summary.Sum(x => x.PrevPeriodSales) -
                                payrollPrime.PrevPeriodSales - overheadTotal.PrevPeriodSales;
            totalTakeaway.PrevPeriodBudget = totalSales.PrevPeriodBudget - summary.Sum(x => x.PrevPeriodBudget) -
                                   payrollPrime.PrevPeriodBudget - overheadTotal.PrevPeriodBudget;
            totalTakeaway.PeriodPSales = totalSales.PeriodSales != 0 ? totalTakeaway.PeriodSales / totalSales.PeriodSales : 0;
            totalTakeaway.PeriodPBudget = totalSales.PeriodPBudget != 0 ? totalTakeaway.PeriodPBudget / totalSales.PeriodPBudget : 0;
            summary.Add(totalTakeaway);

            return summary;
        }

        private List<ExpenseItem> GetPeriodItems(string expenseType, PeriodMarker period, WeekMarker week)
        {
            return _models.EIContainer.Items.Where(x => x.ExpenseType == expenseType && x.Date >= period.StartDate &&
                                                   x.Date < week.EndDate).ToList();
        }
    }
}
