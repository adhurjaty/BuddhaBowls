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
        // used to figure out last order amount and vendor for vendor inventory items
        private PurchaseOrder _latestRecOrder;
        private VendorInvItemsContainer _viContainer;

        public PurchaseOrdersContainer(List<PurchaseOrder> items) : base(items)
        {
            _latestRecOrder = items.OrderByDescending(x => x.ReceivedDate).FirstOrDefault();
        }

        public PurchaseOrdersContainer(List<PurchaseOrder> items, VendorInvItemsContainer viContainer) : this(items)
        {
            _viContainer = viContainer;
        }

        public override PurchaseOrder AddItem(PurchaseOrder item)
        {
            PurchaseOrder order = base.AddItem(item);
            _latestRecOrder = Items.OrderByDescending(x => x.ReceivedDate).FirstOrDefault();
            if(_latestRecOrder.Id == item.Id)
            {
                _viContainer.UpdateSelectedVendor(_latestRecOrder);
            }
            return order;
        }

        public override void RemoveItem(PurchaseOrder item)
        {
            bool removingLatest = item.Id == _latestRecOrder.Id;
            base.RemoveItem(item);

            _latestRecOrder = Items.OrderByDescending(x => x.ReceivedDate).FirstOrDefault();
            if(removingLatest)
            {
                _viContainer.UpdateSelectedVendor(_latestRecOrder);
            }
        }

        public override void Update(PurchaseOrder order)
        {
            base.Update(order);
            PurchaseOrder newLatestOrder = Items.OrderByDescending(x => x.ReceivedDate).FirstOrDefault();
            if (newLatestOrder != _latestRecOrder)
            {
                _viContainer.UpdateSelectedVendor(newLatestOrder);
            }
            _latestRecOrder = newLatestOrder;
        }

        public void ReceiveOrders(List<PurchaseOrder> orders)
        {
            foreach (PurchaseOrder order in orders)
            {
                order.Receive();
                _viContainer.UpdateSelectedVendor(order);
            }
            PushChange();
        }
    }
}
