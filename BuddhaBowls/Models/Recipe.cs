using BuddhaBowls.Helpers;
using BuddhaBowls.Messengers;
using BuddhaBowls.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuddhaBowls.Models
{
    public class Recipe : Model, IItem
    {
        private RecipesContainer _recipesContainer;
        //private List<RecipeItem> _recipeItems;

        private string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                NotifyPropertyChanged("Name");
            }
        }

        private string _recipeUnit;
        public string RecipeUnit
        {
            get
            {
                return _recipeUnit;
            }
            set
            {
                _recipeUnit = value;
                NotifyPropertyChanged("RecipeUnit");
            }
        }

        private float? _recipeUnitConversion;
        public float? RecipeUnitConversion
        {
            get
            {
                return _recipeUnitConversion;
            }
            set
            {
                _recipeUnitConversion = value;
                NotifyPropertyChanged("RecipeUnitConversion");
                NotifyPropertyChanged("CostPerRU");
                NotifyPropertyChanged("RecipeCost");
            }
        }

        private string _category;
        public string Category
        {
            get
            {
                return _category;
            }
            set
            {
                _category = value;
                NotifyPropertyChanged("Category");
            }
        }

        private float _count;
        public float Count
        {
            get
            {
                return _count;
            }
            set
            {
                _count = value;
                NotifyPropertyChanged("Count");
                NotifyPropertyChanged("RecipeCost");
            }
        }

        private string _measure;
        public string Measure
        {
            get
            {
                return _measure;
            }
            set
            {
                _measure = value;
                NotifyPropertyChanged("Measure");
            }
        }

        public bool IsBatch { get; set; }

        public float RecipeCost
        {
            get
            {
                try
                {
                    return CostPerRU * Count;
                }
                catch (Exception e)
                {
                    return 0;
                }
            }
        }

        public float CostPerRU
        {
            get
            {
                if (RecipeUnitConversion == null || RecipeUnitConversion == 0)
                    return 0;
                return (float)(TotalCost / RecipeUnitConversion);
            }
        }

        public float TotalCost
        {
            get
            {
                return ItemList.Sum(x => x.RecipeCost);
            }
        }

        private ObservableCollection<IItem> _itemList;
        public ObservableCollection<IItem> ItemList
        {
            get
            {
                if (_itemList == null)
                {
                    _itemList = new ObservableCollection<IItem>(GetRecipeInvItems());
                    NotifyPropertyChanged("ProportionDetails");
                    NotifyPropertyChanged("TotalCost");
                }
                return _itemList;
            }
            set
            {
                _itemList = value;
                NotifyPropertyChanged("ItemList");
                NotifyPropertyChanged("ProportionDetails");
            }
        }

        public List<CategoryProportion> ProportionDetails
        {
            get
            {
                List<Dictionary<string, float>> catCosts = ItemList.Select(x => x.GetCategoryCosts()).ToList();
                Dictionary<string, float> combinedCatCosts = new Dictionary<string, float>();
                foreach (Dictionary<string, float> dict in catCosts)
                {
                    foreach (KeyValuePair<string, float> kvp in dict)
                    {
                        if (!combinedCatCosts.ContainsKey(kvp.Key))
                            combinedCatCosts[kvp.Key] = 0;
                        combinedCatCosts[kvp.Key] += kvp.Value;
                    }
                }
                float total = combinedCatCosts.Sum(x => x.Value);
                return combinedCatCosts.Select(x => new CategoryProportion(x.Key, x.Value, total))
                                       .OrderByDescending(x => x.CostProportion).ToList();
            }
        }

        public Recipe() : base()
        {
            _tableName = "Recipe";
            Messenger.Instance.Register(MessageTypes.VENDOR_INV_ITEMS_CHANGED, new Action<Message>(RecipeChanged));
        }

        public Recipe(Dictionary<string, string> searchParams) : this()
        {
            string[] record = _dbInt.GetRecord(_tableName, searchParams);

            if (record != null)
            {
                InitializeObject(record);
                GetRecipeItems();
            }

        }

        public float GetCost()
        {
            return RecipeCost * Count;
        }

        public Dictionary<string, float> GetCategoryCosts()
        {
            Dictionary<string, float> costDict = new Dictionary<string, float>();

            foreach (IItem item in ItemList)
            {
                if (item.GetType() == typeof(Recipe))
                {
                    Dictionary<string, float> subCostDict = ((Recipe)item).GetCategoryCosts();
                    foreach (KeyValuePair<string, float> kvp in subCostDict)
                    {
                        if (!costDict.Keys.Contains(kvp.Key))
                            costDict[kvp.Key] = 0;
                        costDict[kvp.Key] += kvp.Value;
                    }
                }
                else
                {
                    if (!costDict.Keys.Contains(item.Category))
                        costDict[item.Category] = 0;
                    costDict[item.Category] += item.GetCost();
                }
            }

            return costDict;
        }

        public override void Update()
        {
            if (ItemList.Count > 0)
                ModelHelper.CreateTable(ConvToRecipeItems(), GetRecipeTableName());
            base.Update();
        }

        public override int Insert()
        {
            if (ItemList.Count > 0)
                ModelHelper.CreateTable(ConvToRecipeItems().ToList(), GetRecipeTableName());
            return base.Insert();
        }

        public override void Destroy()
        {
            _dbInt.DestroyTable(GetRecipeTableName());
            base.Destroy();
        }

        public override string[] GetPropertiesDB(string[] omit = null)
        {
            string[] theseOmissions = new string[] { "RecipeCost", "Measure" };
            return base.GetPropertiesDB(ModelHelper.CombineArrays(omit, theseOmissions));
        }

        public IItem Copy()
        {
            Recipe rec = Copy<Recipe>();
            rec.SetContainer(_recipesContainer);

            return rec;
        }

        public void AddItem(IItem item)
        {
            ItemList.Add(item.Copy());
            ItemList = new ObservableCollection<IItem>(MainHelper.SortItems(ItemList));
            UpdateProperties();

        }

        public void RemoveItem(IItem item)
        {
            ItemList.Remove(ItemList.FirstOrDefault(x => x.GetType() == item.GetType() && x.Id == item.Id));
            UpdateProperties();
        }

        public void SetContainer(RecipesContainer recContainer)
        {
            _recipesContainer = recContainer;
            UpdateProperties();
        }

        private List<RecipeItem> GetRecipeItems()
        {
            return ModelHelper.InstantiateList<RecipeItem>(GetRecipeTableName(), false);
        }

        private IEnumerable<IItem> GetRecipeInvItems()
        {
            return _recipesContainer.GetRecipeInvItems(GetRecipeItems());
        }

        public RecipeItem ToRecipeItem()
        {
            return new RecipeItem() { Name = Name, Quantity = Count, InventoryItemId = null, Measure = Measure };
        }

        private List<RecipeItem> ConvToRecipeItems()
        {
            List<RecipeItem> recItems = new List<RecipeItem>();
            for(int i = 0; i < ItemList.Count; i++)
            {
                RecipeItem ri = ItemList[i].ToRecipeItem();
                ri.Id = i;
                recItems.Add(ri);
            }

            return recItems;
        }

        public IEnumerable<InventoryItem> GetAllItems()
        {
            foreach (IItem item in ItemList)
            {
                if (item.GetType() == typeof(InventoryItem))
                    yield return (InventoryItem)item;
                else
                {
                    foreach (InventoryItem rItem in ((Recipe)item).GetAllItems())
                    {
                        yield return rItem;
                    }
                }
            }
        }

        public void UpdateProperties()
        {
            NotifyPropertyChanged("ItemList");
            NotifyPropertyChanged("ProportionDetails");
        }

        private void RecipeChanged(Message msg)
        {
            InventoryItem item = (InventoryItem)msg.Payload;
            //if(item == null || ItemList.FirstOrDefault(x => x.GetType() == item.GetType() && x.Id == item.Id) != null)
            //{
                ItemList = null;
            //}
        }

        private string GetRecipeTableName()
        {
            return @"Recipes\" + Name;
        }

        public string GetRecipeTablePath()
        {
            return _dbInt.FilePath(GetRecipeTableName());
        }

        public Dictionary<string, float> GetCatCostProportions()
        {
            Dictionary<string, float> propDict = GetCategoryCosts();
            float totalCost = GetCost();

            if (totalCost == 0)
                return null;

            foreach (KeyValuePair<string, float> kvp in propDict)
            {
                propDict[kvp.Key] /= totalCost;
            }

            return propDict;
        }

        public RecipesContainer GetRecContainer()
        {
            return _recipesContainer;
        }

        public void SetRecipeItems(List<RecipeItem> recipeItems)
        {
            ItemList = new ObservableCollection<IItem>(_recipesContainer.GetRecipeInvItems(recipeItems));
        }
    }
}
