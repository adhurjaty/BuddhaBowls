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
        /// Removes the inventory item from all the vendors' lists that contain it
        /// </summary>
        /// <param name="item"></param>
        public void RemoveItemFromVendors(VendorInventoryItem item)
        {
            if (_isMaster)
            {
                foreach (Vendor v in item.Vendors)
                {
                    v.RemoveInvItem(item.ToInventoryItem());
                }
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
            item.SelectedVendor.Update(item.ToInventoryItem());
        }
    }
}
