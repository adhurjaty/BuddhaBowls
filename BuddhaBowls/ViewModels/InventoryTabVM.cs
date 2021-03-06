﻿using BuddhaBowls.Helpers;
using BuddhaBowls.Messengers;
using BuddhaBowls.Models;
using BuddhaBowls.Services;
using BuddhaBowls.UserControls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BuddhaBowls
{
    /// <summary>
    /// Permanent tab showing the current inventory and the inventory history
    /// </summary>
    public partial class InventoryTabVM : ChangeableTabVM
    {

        #region Content Binders
        // Inventory item selected in the datagrids for Orders and Master List
        private Inventory _selectedInventory;
        public Inventory SelectedInventory
        {
            get
            {
                return _selectedInventory;
            }
            set
            {
                _selectedInventory = value;
                NotifyPropertyChanged("SelectedInventory");
            }
        }

        private ObservableCollection<Inventory> _inventoryList;
        public ObservableCollection<Inventory> InventoryList
        {
            get
            {
                return _inventoryList;
            }
            set
            {
                _inventoryList = value;
                NotifyPropertyChanged("InventoryList");
            }
        }

        private List<Inventory> _selectedMultiInventories;
        public List<Inventory> SelectedMultiInventories
        {
            get
            {
                return _selectedMultiInventories;
            }
            set
            {
                _selectedMultiInventories = value;
                CompareCanExecute = value.Count == 2;
            }
        }

        public InventoryListVM InvListVM { get; set; }

        #endregion

        #region ICommand Bindings and Can Execute
        // Plus button in Master invententory list form
        public ICommand AddCommand { get; set; }
        // Minus button in Master invententory list form
        public ICommand DeleteCommand { get; set; }
        // View button in Master invententory list form
        public ICommand ViewCommand { get; set; }
        // Compare button
        public ICommand CompareCommand { get; set; }
        public ICommand InvListCommand { get; set; }
        // TEMPORARY - REMOVE IN NEXT BUILD
        public ICommand FixYieldCommand { get; set; }

        public bool DeleteEditCanExecute
        {
            get
            {
                return SelectedInventory != null && DBConnection;
            }
        }

        public bool ChangeCountCanExecute { get; set; } = true;

        public bool CompareCanExecute { get; set; } = false;

        #endregion

        public InventoryTabVM() : base()
        {
            Header = "Inventory";

            InitSwitchButtons(new string[] { "Master", "Prep Item", "History" });

            AddCommand = new RelayCommand(StartNewInventory, x => DBConnection);
            DeleteCommand = new RelayCommand(DeleteInventory, x => DeleteEditCanExecute && DBConnection);
            ViewCommand = new RelayCommand(ViewInventory, x => DeleteEditCanExecute && DBConnection);
            InvListCommand = new RelayCommand(GenerateInvList, x => DBConnection);
            AddPrepCommand = new RelayCommand(AddNewPrepItem, x => DBConnection);
            EditPrepCommand = new RelayCommand(EditPrepItem, x => SelectedPrepItem != null && DBConnection);
            DeletePrepCommand = new RelayCommand(DeletePrepItem, x => SelectedPrepItem != null && DBConnection);

            FixYieldCommand = new RelayCommand(FixYields, x => DBConnection);

            Messenger.Instance.Register<Message>(MessageTypes.INVENTORY_CHANGED, (msg) => ShowSelectedWeek(PeriodSelector.SelectedPeriod, PeriodSelector.SelectedWeek));
            Messenger.Instance.Register<Message>(MessageTypes.PREP_ITEM_CHANGED, (msg) => Refresh());
            Messenger.Instance.Register<Message>(MessageTypes.VENDOR_INV_ITEMS_CHANGED, (msg) => Refresh());
            Messenger.Instance.Register<Message>(MessageTypes.RECIPE_CHANGED, (msg) => Refresh());

            if (DBConnection)
                PeriodSelector = new PeriodSelectorVM(_models, ShowSelectedWeek);
            // rest of initialization in ChangePageState called from base()
        }

        private void FixYields(object obj)
        {
            string[] paths = new string[] { Path.Combine(Properties.Settings.Default.DBLocation, "InventoryItem.csv") }
                                .Concat(Directory.GetFiles(Path.Combine(Properties.Settings.Default.DBLocation, "Vendors"))).ToArray();
            foreach (string path in paths)
            {
                string[][] contents = File.ReadAllLines(path).Select(x => x.Split(',')).ToArray();
                string[] header = contents[0];
                int yieldIdx = Array.IndexOf(header, "Yield");
                if(yieldIdx > -1)
                {
                    for (int i = 1; i < contents.Length; i++)
                    {
                        string yieldStr = contents[i][yieldIdx];
                        float yieldVal = 100;
                        float.TryParse(yieldStr, out yieldVal);
                        if(yieldVal > 8)
                        {
                            int factor = (int)Math.Round(Math.Log10(yieldVal));
                            contents[i][yieldIdx] = Math.Round((yieldVal * (Math.Pow(10, -factor))), 2).ToString();
                        }
                        
                    }
                }

                string fileContents = string.Join("\n", contents.Select(x => string.Join(",", x)));
                File.WriteAllText(path, fileContents);
            }
        }

        #region ICommand Helpers
        /// <summary>
        /// Creates new tab called Add Inventory Item and populates form - Plus button
        /// </summary>
        /// <param name="obj"></param>
        private void StartNewInventory(object obj)
        {
            NewInventoryVM tabVM = new NewInventoryVM();
            tabVM.Add("New Inventory");
        }

        /// <summary>
        /// Creates a new tab called Edit Inventory Item and populates form - Edit button
        /// </summary>
        /// <param name="obj"></param>
        private void ViewInventory(object obj)
        {
            NewInventoryVM tabVM = new NewInventoryVM(SelectedInventory);
            tabVM.Add("View Inventory");
        }

        /// <summary>
        /// Presents user with warning dialog, then removes item from DB and in-memory list - Minus button
        /// </summary>
        /// <param name="obj"></param>
        private void DeleteInventory(object obj)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete this record?", "Delete record?", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                SelectedInventory.Destroy();
                _models.InContainer.RemoveItem(SelectedInventory);
                SelectedInventory = null;
                Refresh();
            }
        }

        /// <summary>
        /// Triggered by the Compare Inv button - opens a new temp tab for comparison
        /// </summary>
        /// <param name="obj"></param>
        //private void CompareInventories(object obj)
        //{
        //    Inventory[] invs = SelectedMultiInventories.OrderBy(x => x.Date).ToArray();
        //    CompareInvVM tabVM = new CompareInvVM(invs[0], invs[1]);
        //    tabVM.Add("Compare Invs");
        //}

        /// <summary>
        /// Triggered by pressing the Excel Inventory List button
        /// </summary>
        /// <remarks>Can't figure out how to unit test this</remarks>
        /// <param name="obj"></param>
        private void GenerateInvList(object obj)
        {
            ReportGenerator generator = new ReportGenerator(_models);

            if (ParentContext.ExcelThread == null || !ParentContext.ExcelThread.IsAlive)
            {
                try
                {
                    ParentContext.ExcelThread = new Thread(delegate ()
                    {
                        string xlsPath = generator.GenerateMasterInventoryTable("Master Inventory List.xlsx");
                        generator.Close();
                        if (File.Exists(xlsPath))
                            System.Diagnostics.Process.Start(xlsPath);
                    });
                    ParentContext.ExcelThread.Start();
                }
                catch(Exception e)
                {
                    ErrorHandler.ReportError("InventoryTabVM", "GenerateInvList", e);
                }
            }
            else
            {
                MessageBox.Show("Excel process currently running. If you don't know what this means, hit OK and restart the application",
                    "Excel Warning", MessageBoxButton.OK);
            }
        }

        #endregion

        #region Initializers

        /// <summary>
        /// Display on the datagrids that the inventory items could not be found
        /// </summary>
        private void DisplayItemsNotFound()
        {
            // figure out way to display missing data
            InventoryList = new ObservableCollection<Inventory>() { new Inventory() };
        }

        #endregion

        #region Update UI Methods

        /// <summary>
        /// Called when Master List is edited
        /// </summary>
        public void InventoryItemCountChanged()
        {
            ChangeCountCanExecute = true;
        }

        public override void Refresh()
        {
            ShowSelectedWeek(PeriodSelector.SelectedPeriod, PeriodSelector.SelectedWeek);
            if(InvListVM != null)
                InvListVM.InitContainer();


            FilterText = "";
            FilterItems("");
        }

        private void ShowSelectedWeek(PeriodMarker period, WeekMarker week)
        {
            InventoryList = new ObservableCollection<Inventory> (_models.InContainer.Items.Where(x => week.StartDate <= x.Date &&
                                                                                                         x.Date <= week.EndDate)
                                                                                               .OrderByDescending(x => x.Date));
        }

        #endregion

        protected override void ChangePageState(int pageIdx)
        {
            base.ChangePageState(pageIdx);

            switch(pageIdx)
            {
                case 0:
                    if(InvListVM == null)
                        InvListVM = new InventoryListVM();
                    TabControl = _tabCache[0] ?? new MasterInventoryControl(this);
                    break;
                case 1:
                    if (PrepItemList == null)
                        Refresh();
                    TabControl = _tabCache[1] ?? new PrepListControl(this);
                    break;
                case 2:
                    if (InventoryList == null)
                        InventoryList = new ObservableCollection<Inventory>(_models.InContainer.Items.OrderByDescending(x => x.Date));
                    TabControl = _tabCache[2] ?? new InventoryHistoryControl(this);
                    break;
                case -1:
                    DisplayItemsNotFound();
                    break;
            }
        }

    }
}
