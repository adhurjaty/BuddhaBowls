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
        public ExpenseItemsContainer(List<ExpenseItem> items) : base(items)
        {

        }
    }
}
