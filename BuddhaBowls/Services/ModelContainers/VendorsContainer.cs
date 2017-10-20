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
        public VendorsContainer(List<Vendor> items) : base(items)
        {
            //foreach (Vendor vend in items)
            //{
            //    vend.InitItems();
            //}
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
        /// Adds or updates vendor and sets the items sold to the invItems parameter
        /// </summary>
        /// <param name="vend"></param>
        /// <param name="invItems"></param>
        public void AddItem(Vendor vend, List<InventoryItem> invItems)
        {

        }

        /// <summary>
        /// Removes the inventory item from all the vendors' lists that contain it
        /// </summary>
        /// <param name="item"></param>
        public void RemoveItemFromVendors(VendorInventoryItem item)
        {
            foreach (Vendor v in item.Vendors)
            {
                v.RemoveInvItem(item);
            }
        }
    }
}
