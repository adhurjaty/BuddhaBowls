﻿using BuddhaBowls.Helpers;
using BuddhaBowls.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Services
{
    public delegate void UpdateBinding();

    /// <summary>
    /// Base class for container for models. Supposed to allow for easier syncing of data across app
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ModelContainer<T> where T : Model, new()
    {
        protected List<ModelContainer<T>> _copies;
        protected bool _isMaster;

        protected List<T> _items;
        public List<T> Items
        {
            get
            {
                return _items;
            }
        }

        public ModelContainer(List<T> items)
        {
            _items = items;
            _copies = new List<ModelContainer<T>>();
        }

        public ModelContainer(List<T> items, bool isMaster) : this(items)
        {
            _isMaster = true;
        }

        public virtual T AddItem(T item)
        {
            if (!Contains(item))
            {
                _items.Add(item);
                if (_isMaster)
                    item.Insert();
            }

            return item;
        }

        public virtual void AddItems(List<T> items)
        {
            Items.AddRange(items);
        }

        public virtual void RemoveItem(T item)
        {
            _items.RemoveAll(x => x.Id == item.Id);
            if (_isMaster)
                item.Destroy();
        }

        public virtual void Update(T item)
        {
            int idx = Items.FindIndex(x => x.Id == item.Id);
            Items[idx] = item;

            if (_isMaster)
                item.Update();
        }

        public virtual void UpdateMultiple(IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                int idx = Items.FindIndex(x => x.Id == item.Id);
                if(idx != -1)
                    Items[idx] = item;
                if (_isMaster)
                    item.Update();
            }
        }

        public virtual bool Contains(T item)
        {
            return Items.Contains(item);
        }

        //public List<T> GetItemsCopy()
        //{
        //    return _items.Select(x => x.Copy()).ToList();
        //}

        //public void AddUpdateBinding(UpdateBinding ub)
        //{
        //    if (_updateFncs == null)
        //        _updateFncs = new HashSet<UpdateBinding>();
        //    _updateFncs.Add(ub);
        //}

        //public virtual void RemoveUpdateBinding(UpdateBinding ub)
        //{
        //    _updateFncs.Remove(ub);
        //}

        public void SetItems(List<T> items)
        {
            if (_isMaster)
            {
                List<int> ids = _items.Select(x => x.Id).ToList();
                foreach (T item in items)
                {
                    if (ids.Contains(item.Id))
                        item.Update();
                    else
                        item.Insert();
                }
            }
            _items = items;
            UpdateCopies();
        }

        public void SyncCopy(ModelContainer<T> copy)
        {
            SetItems(copy.Items);
        }

        protected virtual U Copy<U>() where U : ModelContainer<T>, new()
        {
            throw new InvalidOperationException("Cannot call base copy method");
        }

        /// <summary>
        /// Method to update copies of this ModelContainer to match it at its current state
        /// </summary>
        protected virtual void UpdateCopies()
        {
            
        }

        /// <summary>
        /// Method to update copies of this ModelContainer to match its item
        /// </summary>
        /// <param name="item">Item to synchronize the copies</param>
        protected virtual void UpdateCopies(T item)
        {
            foreach (ModelContainer<T> copy in _copies)
            {
                int idx = copy.Items.FindIndex(x => x.Id == item.Id);
                if(idx == -1)
                    copy.AddItem(item);
                else
                    copy.Update(item);
            }
        }
    }
}
