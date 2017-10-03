using BuddhaBowls.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Services
{
    public class PurchaseOrdersContainer : ModelContainer<PurchaseOrder>
    {
        public PurchaseOrdersContainer(List<PurchaseOrder> items) : base(items)
        {

        }
    }
}
