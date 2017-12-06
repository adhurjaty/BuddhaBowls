using BuddhaBowls.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Services
{
    public class ExpenseItemsContainer : ModelContainer<ExpenseItem>
    {
        public ExpenseItemsContainer(List<ExpenseItem> items, bool isMaster = false) : base(items, isMaster)
        {
            
        }

        /// <summary>
        /// Copy the container
        /// </summary>
        /// <returns></returns>
        public ExpenseItemsContainer Copy()
        {
            ExpenseItemsContainer eic = new ExpenseItemsContainer(_items.Select(x => x.Copy<ExpenseItem>()).ToList());
            _copies.Add(eic);
            return eic;
        }

        protected override void UpdateCopies()
        {
            for (int i = 0; i < _copies.Count; i++)
            {
                _copies[i] = Copy();
            }
        }
    }
}
