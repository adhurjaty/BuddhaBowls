using BuddhaBowls.Helpers;
using BuddhaBowls.Messengers;
using BuddhaBowls.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Services
{
    /// <summary>
    /// Class to hold the inventory items associated with which vendors sell them
    /// </summary>
    public class VendorInvItemsContainer
    {
        // tracks vendors, necessary for each item to reference
        private VendorsContainer _vendorsContainer;
        private InventoryItemsContainer _invItemsContainer;
        private bool _isMaster = false;
        private List<VendorInvItemsContainer> _copies;

        private List<VendorInventoryItem> _items;
        public List<VendorInventoryItem> Items
        {
            get
            {
                return _items;
            }
            private set
            {
                _items = value;
            }
        }

        /// <summary>
        /// Instantiate the container. Should only be called in the DBCache class
        /// </summary>
        /// <param name="items">List of vendor inventory items to contain</param>
        /// <param name="vContainer">Vendor container</param>
        public VendorInvItemsContainer(InventoryItemsContainer items, VendorsContainer vContainer)
        {
            _invItemsContainer = items;
            _vendorsContainer = vContainer;
            _copies = new List<VendorInvItemsContainer>();
            SetItems(_invItemsContainer);
            //Messenger.Instance.Register(MessageTypes.INVENTORY_ITEM_ADDED, new Action<Message>(OnInventoryItemAdded));
            //Messenger.Instance.Register(MessageTypes.INVENTORY_ITEM_REMOVED, new Action<Message>(OnInventoryItemRemoved));
            //Messenger.Instance.Register(MessageTypes.INVENTORY_ITEM_CHANGED, new Action<Message>(OnInventoryItemChanged));
        }

        public VendorInvItemsContainer(InventoryItemsContainer items, VendorsContainer vContainer, bool isMaster) : this(items, vContainer)
        {
            _isMaster = isMaster;
        }

        public void SetItems(InventoryItemsContainer itemsCont)
        {
            _invItemsContainer = itemsCont;
            Items = _invItemsContainer.Items.Select(x => new VendorInventoryItem(GetVendorsFromItem(x), x)).ToList();
            UpdateCopies();
        }

        /// <summary>
        /// Adds item to the list (no DB insert)
        /// </summary>
        /// <param name="item"></param>
        public void AddItem(VendorInventoryItem item)
        {
            Items.Add(item);
            _items = MainHelper.SortItems(_items).ToList();
        }

        /// <summary>
        /// Adds a new item with a list of which vendors sell the item
        /// </summary>
        /// <param name="item">New item</param>
        /// <param name="vendors">List of vendors that sell it</param>
        public VendorInventoryItem AddItem(InventoryItem item, List<VendorInfo> vendors)
        {
            _invItemsContainer.AddItem(item);
            VendorInventoryItem vItem;

            int idx = Items.FindIndex(x => x.Id == item.Id);
            if (idx != -1)
            {
                _items[idx].SetVendorDict(vendors);
                //_items[idx].InvItem = item;
                vItem = _items[idx];
            }
            else
            {
                vItem = new VendorInventoryItem(item, vendors);
                int insertIdx = Properties.Settings.Default.InventoryOrder.IndexOf(item.Name);
                if (insertIdx == -1)
                    _items.Add(vItem);
                else
                    _items.Insert(insertIdx, vItem);
            }

            UpdateCopies(vItem);
            Messenger.Instance.NotifyColleagues(MessageTypes.VENDOR_INV_ITEMS_CHANGED, vItem);
            return vItem;
        }

        /// <summary>
        /// Removes item from current inventory list and all vendor lists
        /// </summary>
        /// <param name="item"></param>
        public void RemoveItem(VendorInventoryItem item)
        {
            if(_isMaster)
                _vendorsContainer.RemoveItemFromVendors(item);

            _invItemsContainer.RemoveItem(item.ToInventoryItem());
            Items.Remove(item);
            Messenger.Instance.NotifyColleagues(MessageTypes.VENDOR_INV_ITEMS_CHANGED, item);
            UpdateCopies();
        }

        public void Update(VendorInventoryItem item)
        {
            //int idx = Items.FindIndex(x => x.Id == item.Id);
            //item.SetVendorItem(item.SelectedVendor, item.ToInventoryItem());
            //Items[idx] = item;
            if (_isMaster)
                item.Update();
            _vendorsContainer.UpdateItem(item);
            UpdateCopies(item);
        }

        /// <summary>
        /// Updates the items in the list. Does not remove any items from the master list
        /// </summary>
        /// <param name="items"></param>
        public void Update(List<VendorInventoryItem> items)
        {
            foreach (VendorInventoryItem item in items)
            {
                Update(item);
            }
        }

        /// <summary>
        /// Copy the container
        /// </summary>
        /// <returns></returns>
        public VendorInvItemsContainer Copy()
        {
            VendorInvItemsContainer viic = new VendorInvItemsContainer(_invItemsContainer.Copy(), _vendorsContainer.Copy());
            _copies.Add(viic);
            return viic;
        }

        public void UpdateCopies()
        {
            for (int i = 0; i < _copies.Count; i++)
            {
                // don't want to change count values for the copies, so re-assign count value (bit of a hack)
                Dictionary<int, float> countDict = _copies[i].Items.ToDictionary(x => x.Id, y => y.Count);
                _copies[i].SetItems(GetInvItemsContainer());

                foreach (VendorInventoryItem item in _copies[i].Items)
                {
                    if(countDict.ContainsKey(item.Id))
                        item.Count = countDict[item.Id];
                }

            }
        }

        public void UpdateCopies(VendorInventoryItem item)
        {
            for (int i = 0; i < _copies.Count; i++)
            {
                int idx = _copies[i].Items.FindIndex(x => x.Id == item.Id);
                if (idx == -1)
                    _copies[i].AddItem((VendorInventoryItem)item.Copy());
                else
                    _copies[i].Items[idx] = (VendorInventoryItem)item.Copy();
            }
        }

        /// <summary>
        /// Brings this container to the same state as the copy. Removes the copy from _copies
        /// </summary>
        /// <param name="container"></param>
        public void SyncCopy(VendorInvItemsContainer container)
        {
            _invItemsContainer = container.GetInvItemsContainer();
            _vendorsContainer = container.GetVendorsContainer();
            RemoveCopy(container);
        }

        /// <summary>
        /// Adds vendor to vendor container and associates items with new vendor
        /// </summary>
        /// <param name="vend"></param>
        public void AddVendor(Vendor vend, List<InventoryItem> invItems)
        {
            _vendorsContainer.AddItem(vend);
            //vend.SetItemsContainer(new InventoryItemsContainer(invItems));
            AddVendorAssociations(vend, invItems);
        }

        /// <summary>
        /// Associates items with the vendor (only adds associations)
        /// </summary>
        /// <param name="vend"></param>
        /// <param name="invItems"></param>
        public void AddVendorAssociations(Vendor vend, List<InventoryItem> invItems)
        {
            foreach (InventoryItem item in invItems)
            {
                VendorInventoryItem vItem = Items.First(x => x.Id == item.Id);
                vItem.AddVendor(vend, item);
                UpdateCopies(vItem);
            }
        }

        /// <summary>
        /// Removes a vendor from vendor container and all associations with inv items
        /// </summary>
        /// <param name="vend"></param>
        public void RemoveVendor(Vendor vend)
        {
            _vendorsContainer.RemoveItem(vend);
            RemoveVendorAssociations(vend);   
        }

        /// <summary>
        /// Removes all links from inventory items to the vendor param
        /// </summary>
        /// <param name="vend"></param>
        public void RemoveVendorAssociations(Vendor vend)
        {
            List<InventoryItem> vendItems = new List<InventoryItem>(vend.ItemList);
            foreach (InventoryItem item in vendItems)
            {
                VendorInventoryItem vItem = Items.First(x => x.Id == item.Id);
                vItem.DeleteVendor(vend);
                UpdateCopies(vItem);
            }
        }

        /// <summary>
        /// Remove copy from _copies. Used when closing a temp tab that holds a copy
        /// </summary>
        /// <param name="viContainer"></param>
        public void RemoveCopy(VendorInvItemsContainer viContainer)
        {
            _invItemsContainer.RemoveCopy(viContainer.GetInvItemsContainer());
            _vendorsContainer.RemoveCopy(viContainer.GetVendorsContainer());
            _copies.Remove(viContainer);
        }

        /// <summary>
        /// Save the display order of the inventory items
        /// </summary>
        public void SaveOrder()
        {
            string dir = Path.Combine(Properties.Settings.Default.DBLocation, "Settings");
            Directory.CreateDirectory(dir);
            File.WriteAllLines(Path.Combine(dir, GlobalVar.INV_ORDER_FILE), Properties.Settings.Default.InventoryOrder);
            _items = MainHelper.SortItems(_items).ToList();
        }

        /// <summary>
        /// Get the PriceExtension value of each category of items
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, float> GetCategoryValues()
        {
            Dictionary<string, float> costDict = new Dictionary<string, float>();

            foreach (VendorInventoryItem item in _items)
            {
                if (!costDict.Keys.Contains(item.Category))
                    costDict[item.Category] = 0;
                costDict[item.Category] += item.PriceExtension;
            }

            return costDict;
        }

        /// <summary>
        /// Update (push to DB) all of the items in the container
        /// </summary>
        //public void UpdateContainer()
        //{
        //    foreach (VendorInventoryItem item in Items)
        //    {
        //        item.Update();
        //    }
        //}

        /// <summary>
        /// Associates item with vendor and updates vendor
        /// </summary>
        /// <param name="item"></param>
        /// <param name="vend"></param>
        public void UpdateItem(InventoryItem item, Vendor vend)
        {
            VendorInventoryItem vItem = Items.First(x => x.Id == item.Id);
            vItem.SetVendorItem(vend, item);
            vend.AddInvItem(item);
            _vendorsContainer.Update(vend);
            UpdateCopies(vItem);
        }

        public void RemoveFromVendor(InventoryItem item, Vendor vend)
        {
            vend.RemoveInvItem(item);
            VendorInventoryItem vItem = Items.First(x => x.Id == item.Id);
            vItem.DeleteVendor(vend);
            _vendorsContainer.Update(vend);
            UpdateCopies(vItem);
        }

        /// <summary>
        /// Associates vendor with all of items in invItems, removes association with vendor and items not in invItems
        /// </summary>
        /// <param name="vendor"></param>
        /// <param name="list"></param>
        public void UpdateVendorItems(Vendor vendor, List<InventoryItem> invItems)
        {
            RemoveVendorAssociations(vendor);
            AddVendorAssociations(vendor, invItems);
        }

        /// <summary>
        /// Updates the last vendor and selected vendor for VendorInventoryItems in the order
        /// </summary>
        /// <param name="order"></param>
        public void UpdateSelectedVendor(PurchaseOrder order)
        {
            Vendor vend = _vendorsContainer.Items.First(x => x.Name == order.VendorName);
            foreach (int id in order.GetPOItems().Select(x => x.Id))
            {
                VendorInventoryItem item = Items.First(x => x.Id == id);
                item.SelectedVendor = vend;
                item.LastVendorId = vend.Id;
                item.Update();
            }
        }

        public InventoryItemsContainer GetInvItemsContainer()
        {
            return _invItemsContainer;
        }

        public VendorsContainer GetVendorsContainer()
        {
            return _vendorsContainer;
        }

        /// <summary>
        /// Gets a dictionary of vendors that offer the passed-in inventory item. The inventory item value is the vendor-specific inventory
        /// item associated with that vendor (not the one from the model container, which is passed in).
        /// </summary>
        public Dictionary<Vendor, InventoryItem> GetVendorsFromItem(InventoryItem item)
        {
            Dictionary<Vendor, InventoryItem> vendorDict = new Dictionary<Vendor, InventoryItem>();
            foreach (Vendor v in _vendorsContainer.Items)
            {
                InventoryItem vendorItem = v.ItemList.FirstOrDefault(x => x.Id == item.Id);
                if (vendorItem != null)
                    vendorDict[v] = _isMaster ? vendorItem : (InventoryItem)vendorItem.Copy();
            }

            return vendorDict;
        }

        /// <summary>
        /// Gets list of items that vendor sells from this container and sets the Vendor's items to this
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public List<InventoryItem> GetVendorItems(Vendor v)
        {
            List<InventoryItem> items = Items.Where(x => x.Vendors.Select(y => y.Id).Contains(v.Id)).Select(x => x.GetInvItemFromVendor(v)).ToList();
            return items;
        }

        private void OnInventoryItemAdded(Message msg)
        {

        }

        private void OnInventoryItemRemoved(Message msg)
        {

        }

        private void OnInventoryItemChanged(Message msg)
        {

        }
    }
}
