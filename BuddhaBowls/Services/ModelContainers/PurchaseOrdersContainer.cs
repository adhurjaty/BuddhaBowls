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
                Messenger.Instance.NotifyColleagues(MessageTypes.PO_CHANGED, item);
            }

            return order;
        }

        public override void RemoveItem(PurchaseOrder item)
        {
            base.RemoveItem(item);
            _viContainer.UpdateMasterItemOrderRemoved(item, Items);
            if (_isMaster)
            {
                Messenger.Instance.NotifyColleagues(MessageTypes.PO_CHANGED, item);
            }
        }

        public override void Update(PurchaseOrder order)
        {
            base.Update(order);
            _viContainer.UpdateMasterItemOrderChanged(order, Items);
            if (_isMaster)
            {
                Messenger.Instance.NotifyColleagues(MessageTypes.PO_CHANGED, order);
            }
        }

        public void RecieveOrder(PurchaseOrder order)
        {
            order.Receive();
            _viContainer.UpdateMasterItemOrderAdded(order);
            if (_isMaster)
                Messenger.Instance.NotifyColleagues(MessageTypes.PO_CHANGED, order);
        }

        public void ReOpenOrder(PurchaseOrder order)
        {
            order.ReOpen();
            _viContainer.UpdateMasterItemOrderRemoved(order, Items);
        }
    }
}
