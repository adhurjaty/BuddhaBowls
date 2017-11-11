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
        // tracks the open copies of this object for use in master -> copy updating
        private List<ExpenseItemsContainer> _copies;

        public ExpenseItemsContainer(List<ExpenseItem> items) : base(items)
        {

        }

        /// <summary>
        /// Copy the container
        /// </summary>
        /// <returns></returns>
        public ExpenseItemsContainer Copy()
        {
            ExpenseItemsContainer eic = new ExpenseItemsContainer(_items.Select(x => x.Copy<ExpenseItem>()).ToList());
            if (_copies == null)
                _copies = new List<ExpenseItemsContainer>();
            _copies.Add(eic);
            return eic;
        }
    }
}
