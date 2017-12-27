using BuddhaBowls.Messengers;
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
        private VendorInvItemsContainer _viContainer;

        public PurchaseOrdersContainer(List<PurchaseOrder> items, VendorInvItemsContainer viContainer, bool isMaster = false) : base(items, isMaster)
        {
            _viContainer = viContainer;
        }

        public override PurchaseOrder AddItem(PurchaseOrder item)
        {
            PurchaseOrder order = base.AddItem(item);
            if (_isMaster)
            {
                Messenger.Instance.NotifyColleagues(MessageTypes.PO_CHANGED);
                _viContainer.NewOrderAdded(item);
            }

            return order;
        }

        public override void RemoveItem(PurchaseOrder item)
        {
            base.RemoveItem(item);
            _viContainer.OrderRemoved(item, Items);
            if (_isMaster)
            {
                Messenger.Instance.NotifyColleagues(MessageTypes.PO_CHANGED);
            }
        }

        public override void Update(PurchaseOrder order)
        {
            base.Update(order);
            PurchaseOrder newLatestOrder = Items.OrderByDescending(x => x.ReceivedDate).FirstOrDefault();
            _viContainer.OrderRemoved(order, Items);
            _viContainer.NewOrderAdded(newLatestOrder);
        }
    }
}
