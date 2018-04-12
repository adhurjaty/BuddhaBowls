using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Models
{
    public class PrepItem : Model, ISortable
    {
        private IItem _refItem;

        private int? _inventoryItemId;
        public int? InventoryItemId
        {
            get
            {
                return _inventoryItemId;
            }
            set
            {
                _inventoryItemId = value;
                if (_inventoryItemId != null)
                    RecipeItemId = null;
            }
        }

        private int? _recipeItemId;
        public int? RecipeItemId
        {
            get
            {
                return _recipeItemId;
            }
            set
            {
                _recipeItemId = value;
                if (_recipeItemId != null)
                    InventoryItemId = null;
            }
        }

        private float _conversion;
        public float Conversion
        {
            get
            {
                return _conversion;
            }
            set
            {
                _conversion = value;
                NotifyPropertyChanged("Conversion");
                NotifyPropertyChanged("Cost");
            }
        }

        public string Name
        {
            get
            {
                if (_refItem == null)
                    return "";
                return _refItem.Name;
            }
        }

        private string _countUnit;
        public string CountUnit
        {
            get
            {
                return _countUnit;
            }
            set
            {
                _countUnit = value;
                NotifyPropertyChanged("CountUnit");
            }
        }

        public float Cost
        {
            get
            {
                if (_refItem == null)
                    return 0;
                if (_refItem.GetType() == typeof(Recipe))
                    return _refItem.CostPerRU * Conversion;
                return _refItem.CountPrice * Conversion;
            }
        }

        private float _lineCount;
        public float LineCount
        {
            get
            {
                return _lineCount;
            }
            set
            {
                _lineCount = value;
                NotifyPropertyChanged("LineCount");
                NotifyPropertyChanged("TotalCount");
                NotifyPropertyChanged("Extension");
            }
        }

        private float _walkInCount;
        public float WalkInCount
        {
            get
            {
                return _walkInCount;
            }
            set
            {
                _walkInCount = value;
                NotifyPropertyChanged("WalkInCount");
                NotifyPropertyChanged("TotalCount");
                NotifyPropertyChanged("Extension");
            }
        }

        public float TotalCount
        {
            get
            {
                return LineCount + WalkInCount;
            }
        }

        public float Extension
        {
            get
            {
                return Cost * TotalCount;
            }
        }

        public PrepItem() : base()
        {
            _tableName = "PrepItem";
        }

        public PrepItem(Dictionary<string, string> searchParams) : this()
        {
            string[] record = _dbInt.GetRecord(_tableName, searchParams);

            if (record != null)
            {
                InitializeObject(record);
            }
        }

        public PrepItem(IItem item) : this()
        {
            SetItem(item);
        }

        public void SetItem(IItem item)
        {
            _refItem = item;

            if (item.GetType() == typeof(Recipe))
                RecipeItemId = item.Id;
            else
                InventoryItemId = item.Id;
            NotifyPropertyChanged("Cost");
            NotifyPropertyChanged("Extension");
            NotifyPropertyChanged("Name");
        }

        public PrepItem Copy()
        {
            PrepItem outItem = Copy<PrepItem>();
            outItem.SetItem(_refItem);
            return outItem;
        }

        public override int Insert()
        {
            if (_refItem != null)
                SetItem(_refItem);

            return base.Insert();
        }

        public override void Update()
        {
            if (_refItem != null)
                SetItem(_refItem);

            base.Update();
        }

        public IItem GetBaseItem()
        {
            return _refItem;
        }

        public CountPrepItem ToCountPrepItem()
        {
            return new CountPrepItem(this);
        }

        public PrepItem FromCountPrep(CountPrepItem item)
        {
            PrepItem outItem = Copy();
            outItem.WalkInCount = item.WalkInCount;
            outItem.LineCount = item.LineCount;

            return outItem;
        }

        public Dictionary<string, float> GetCategoryCosts()
        {
            if (_refItem.GetType() == typeof(VendorInventoryItem))
                return new Dictionary<string, float>() { { _refItem.Category, _refItem.CountPrice * TotalCount * Conversion } };

            Dictionary<string, float> catCosts = _refItem.GetCategoryCosts();
            List<string> keys = catCosts.Keys.ToList();
            foreach (string k in keys)
            {
                try
                {
                    catCosts[k] *= TotalCount * Conversion / (float)_refItem.RecipeUnitConversion;
                }
                catch(DivideByZeroException e)
                {
                    continue;
                }
            }

            return catCosts;
        }
    }
}
