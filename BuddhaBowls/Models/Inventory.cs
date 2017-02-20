using BuddhaBowls.Helpers;
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
        public DateTime Date { get; set; }

        public Inventory()
        {
            _tableName = "Inventory";
        }

        public Inventory(DateTime date) : this()
        {
            Date = date;
        }

        public List<InventoryItem> GetInventoryHistory()
        {
            return ModelHelper.InstantiateList<InventoryItem>(GetInventoryTable(), fileExists: false);
        }

        public int Insert(List<InventoryItem> items)
        {
            if(!File.Exists(Path.Combine(Properties.Settings.Default.DBLocation, GetInventoryTable() + ".csv")))
            {
                ModelHelper.CreateTable(items.OrderBy(x => x.Id).ToList(), GetInventoryTable());
                return base.Insert();
            }

            return -1;
        }

        public override int Insert()
        {
            throw new InvalidOperationException("Must use the parameter version of Insert(List<InventoryItem> item)");
        }

        public void Update(List<InventoryItem> items)
        {
            ModelHelper.CreateTable(items.OrderBy(x => x.Id).ToList(), GetInventoryTable());
            base.Update();
        }

        public override void Destroy()
        {
            _dbInt.DestroyTable(GetInventoryTable());
            base.Destroy();
        }

        private string GetInventoryTable()
        {
            return @"Inventory History\Inventory_" + Date.ToString("MM-dd-yyyy");
        }

    }
}
