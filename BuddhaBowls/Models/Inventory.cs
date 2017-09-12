using BuddhaBowls.Helpers;
using BuddhaBowls.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Models
{
    public class Inventory : Model
    {
        private bool _invChanged = true;    // used to speed up calls to CategoryItemsDict

        public DateTime Date { get; set; }

        private Dictionary<string, List<InventoryItem>> _categoryItemsDict;
        public Dictionary<string, List<InventoryItem>> CategoryItemsDict
        {
            get
            {
                //if (_invChanged)
                //{
                //    _categoryItemsDict = GetInventoryHistory().GroupBy(x => x.Category).ToDictionary(x => x.Key, x => x.ToList());
                //    _invChanged = false;
                //}
                return _categoryItemsDict;
            }
        }

        public VendorInvItemsContainer InvItemsContainer { get; private set; }

        public Inventory()
        {
            _tableName = "Inventory";
        }

        public Inventory(DateTime date) : this()
        {
            Date = date;
        }

        public Inventory(DateTime date, VendorInvItemsContainer viContainer) : this(date)
        {
            InvItemsContainer = viContainer;
        }

        /// <summary>
        /// Load the inventory items from the history folder
        /// </summary>
        //public VendorInvItemsContainer GetInventoryHistory()
        //{
        //    return new VendorInvItemsContainer(ModelHelper.InstantiateList<InventoryItem>(GetInventoryTable(), isModel: false));
        //}

        /// <summary>
        /// Creates a new record in the Inventory table and creates a new inventory sold items table
        /// </summary>
        /// <returns></returns>
        public override int Insert()
        {
            if(InvItemsContainer == null)
            {
                throw new InvalidOperationException("Must set InvItemsContainer before using insert");
            }

            if (!File.Exists(Path.Combine(Properties.Settings.Default.DBLocation, GetInventoryTable() + ".csv")))
            {
                ModelHelper.CreateTable(InvItemsContainer.Items.Select(x => x.ToInventoryItem()).OrderBy(x => x.Id).ToList(), GetInventoryTable());
                _invChanged = true;
                return base.Insert();
            }

            return -1;
        }

        /// <summary>
        /// Update both the record in the Inventory table
        /// </summary>
        /// <param name="items"></param>
        public override void Update()
        {
            if (InvItemsContainer != null)
            {
                ModelHelper.CreateTable(InvItemsContainer.Items.Select(x => x.ToInventoryItem()).OrderBy(x => x.Id).ToList(), GetInventoryTable());
                _invChanged = true;
            }
            base.Update();
        }

        public override void Destroy()
        {
            _dbInt.DestroyTable(GetInventoryTable());
            _invChanged = true;
            base.Destroy();
        }

        public void SetInvItemsContainer(VendorInvItemsContainer container)
        {
            InvItemsContainer = container;
        }

        //public string GetHistoryTablePath()
        //{
        //    return Path.Combine(Properties.Settings.Default.DBLocation, GetInventoryTable() + ".csv");
        //}

        private string GetInventoryTable()
        {
            return @"Inventory History\Inventory_" + Date.ToString("MM-dd-yyyy");
        }

    }
}
