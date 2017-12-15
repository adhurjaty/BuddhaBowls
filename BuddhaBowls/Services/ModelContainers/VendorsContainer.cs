using BuddhaBowls.Messengers;
using BuddhaBowls.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Services
{

    /// <summary>
    /// Container for vendors
    /// </summary>
    public class VendorsContainer : ModelContainer<Vendor>
    {
        /// <summary>
        /// Instantiate container
        /// </summary>
        /// <param name="items"></param>
        public VendorsContainer(List<Vendor> items, bool isMaster = false) : base(items, isMaster)
        {

        }

        public override Vendor AddItem(Vendor item)
        {
            Vendor vend = base.AddItem(item);
            if (_isMaster)
                Messenger.Instance.NotifyColleagues(MessageTypes.VENDORS_CHANGED);
            return vend;
        }

        public override void Update(Vendor item)
        {
            base.Update(item);
            if (_isMaster)
                Messenger.Instance.NotifyColleagues(MessageTypes.VENDORS_CHANGED);
        }

        public override void RemoveItem(Vendor item)
        {
            base.RemoveItem(item);
            if (_isMaster)
                Messenger.Instance.NotifyColleagues(MessageTypes.VENDORS_CHANGED);
        }

        /// <summary>
        /// Check to see if the vendor name already exists (case and space insensitive)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public override bool Contains(Vendor item)
        {
            return Items.Select(x => x.Name.ToUpper().Replace(" ", "")).Contains(item.Name.ToUpper().Replace(" ", ""));
        }

        /// <summary>
        /// Removes the inventory item from all the vendors' lists and tables that contain it
        /// </summary>
        /// <param name="item"></param>
        public void RemoveItemFromVendors(VendorInventoryItem item)
        {
            foreach (Vendor v in item.Vendors)
            {
                v.RemoveInvItem(item.ToInventoryItem());
            }
        }

        public VendorsContainer Copy()
        {
            VendorsContainer cpy = new VendorsContainer(Items.Select(x => x.Copy()).ToList());
            _copies.Add(cpy);
            return cpy;
        }

        protected override void UpdateCopies()
        {
            base.UpdateCopies();
        }

        public void UpdateItem(VendorInventoryItem item)
        {
            if(item.SelectedVendor != null)
                item.SelectedVendor.Update(item.ToInventoryItem());
        }
    }
}
