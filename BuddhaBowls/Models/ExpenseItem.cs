using BuddhaBowls.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Models
{
    public class ExpenseItem : Model
    {
        public string Name { get; set; }
        public float WeekPSales { get; set; }
        public float WeekSales { get; set; }
        public float WeekPBudget { get; set; }
        public float WeekBudget { get; set; }
        public float WeekVar
        {
            get
            {
                return WeekSales - WeekBudget;
            }
        }
        public float WeekPVar
        {
            get
            {
                if (WeekBudget != 0)
                    return WeekVar / WeekBudget;
                return 0;
            }
        }
        public float PrevPeriodSales { get; set; }
        public float PeriodPSales { get; set; }
        public float PeriodSales
        {
            get
            {
                return WeekSales + PrevPeriodSales;
            }
        }
        public float PrevPeriodBudget { get; set; }
        public float PeriodPBudget { get; set; }
        public float PeriodBudget
        {
            get
            {
                return WeekBudget + PrevPeriodBudget;
            }
        }
        public float PeriodVar
        {
            get
            {
                return PeriodSales - PeriodBudget;
            }
        }
        public float PeriodPVar
        {
            get
            {
                if (PeriodBudget != 0)
                    return PeriodVar / PeriodBudget;
                return 0;
            }
        }
        public DateTime Date { get; set; }
        public string ExpenseType { get; set; }

        public ExpenseItem()
        {
            Date = DateTime.Now;
        }

        public ExpenseItem(string expenseType, string name, DateTime date)
        {
            _tableName = "ExpenseItem";
            Date = date;
            Name = name;
            ExpenseType = expenseType;
        }

        public override string[] GetPropertiesDB(string[] omit = null)
        {
            string[] theseOmissions = new string[] { "PrevPeriodSales", "PrevPeriodBudget" };
            return base.GetPropertiesDB(ModelHelper.CombineArrays(omit, theseOmissions));
        }

        public override int Insert()
        {
            if (string.IsNullOrEmpty(_tableName))
                throw new MissingMethodException("Can't call Insert for this type of Expense Item");
            return base.Insert();
        }

        public override void Update()
        {
            if (string.IsNullOrEmpty(_tableName))
                throw new MissingMethodException("Can't call Update for this type of Expense Item");
            base.Update();
        }

        public override void Destroy()
        {
            if (string.IsNullOrEmpty(_tableName))
                throw new MissingMethodException("Can't call Destroy for this type of Expense Item");
            base.Destroy();
        }
    }
}
