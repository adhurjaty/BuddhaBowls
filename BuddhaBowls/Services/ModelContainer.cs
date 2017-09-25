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
        protected HashSet<UpdateBinding> _updateFncs;

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
        }

        public virtual void AddItem(T item)
        {
            if (!Contains(item))
            {
                _items.Add(item);
                PushChange();
            }
        }

        public virtual void RemoveItem(T item)
        {
            _items.RemoveAll(x => x.Id == item.Id);
            PushChange();
        }

        public virtual void Update(T item)
        {
            item.Update();
        }

        public virtual bool Contains(T item)
        {
            return Items.Contains(item);
        }

        //public List<T> GetItemsCopy()
        //{
        //    return _items.Select(x => x.Copy()).ToList();
        //}

        public void AddUpdateBinding(UpdateBinding ub)
        {
            if (_updateFncs == null)
                _updateFncs = new HashSet<UpdateBinding>();
            _updateFncs.Add(ub);
        }

        public virtual void RemoveUpdateBinding(UpdateBinding ub)
        {
            _updateFncs.Remove(ub);
        }

        public void SetItems(List<T> items)
        {
            _items = items;
            PushChange();
        }

        public void PushChange()
        {
            if (_updateFncs != null)
            {
                foreach (UpdateBinding ub in _updateFncs)
                {
                    ub();
                }
            }
        }
    }
}
