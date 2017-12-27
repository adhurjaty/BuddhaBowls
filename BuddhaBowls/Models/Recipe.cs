using BuddhaBowls.Helpers;
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
        private InventoryItemsContainer _invItemsContainer;
        private RecipesContainer _recipesContainer;

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

        public ObservableCollection<IItem> ItemList
        {
            get
            {
                if(_invItemsContainer == null || _recipesContainer == null)
                    SetContainers();

                return new ObservableCollection<IItem>(_invItemsContainer.Items.Select(x => (IItem)x).Concat(_recipesContainer.Items.Select(x => (IItem)x)).ToList());
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
                ModelHelper.CreateTable(ConvToRecipeItems(ItemList.ToList()), GetRecipeTableName());
            base.Update();
        }

        public override int Insert()
        {
            if (ItemList.Count > 0)
                ModelHelper.CreateTable(ConvToRecipeItems(ItemList.ToList()).ToList(), GetRecipeTableName());
            return base.Insert();
        }

        public override void Destroy()
        {
            _dbInt.DestroyTable(GetRecipeTableName());
            base.Destroy();
        }

        public override string[] GetPropertiesDB(string[] omit = null)
        {
            string[] theseOmissions = new string[] { "RecipeCost" };
            return base.GetPropertiesDB(ModelHelper.CombineArrays(omit, theseOmissions));
        }

        public IItem Copy()
        {
            Recipe rec = Copy<Recipe>();
            if (_invItemsContainer == null || _recipesContainer == null)
                SetContainers();
            rec.SetContainers(_invItemsContainer.Copy(), _recipesContainer.Copy());

            return rec;
        }

        public void RemoveCopy(Recipe item)
        {
            _invItemsContainer.RemoveCopy(item.GetInvContainer());
            _recipesContainer.RemoveCopy(item.GetRecContainer());
        }

        public void AddItem(IItem item)
        {
            if (item.GetType() == typeof(InventoryItem))
                _invItemsContainer.AddItem((InventoryItem)item);
            else
                _recipesContainer.AddItem((Recipe)item);
            NotifyPropertyChanged("ItemList");
            NotifyPropertyChanged("ProportionDetails");

        }

        public void RemoveItem(IItem item)
        {
            if (item.GetType() == typeof(InventoryItem))
                _invItemsContainer.RemoveItem((InventoryItem)item);
            else
                _recipesContainer.RemoveItem((Recipe)item);
            NotifyPropertyChanged("ItemList");
            NotifyPropertyChanged("ProportionDetails");
        }

        public void SetContainers(InventoryItemsContainer invContainer, RecipesContainer recContainer)
        {
            _invItemsContainer = invContainer;
            _recipesContainer = recContainer;
            NotifyPropertyChanged("ItemList");
            NotifyPropertyChanged("ProportionDetails");
        }

        private List<RecipeItem> GetRecipeItems()
        {
            return ModelHelper.InstantiateList<RecipeItem>(GetRecipeTableName(), false);
        }

        public void SetRecipeItems(List<RecipeItem> items)
        {
            Dictionary<Type, List<IItem>> itemsByType = items.Select(x => x.GetIItem())
                                                                 .Where(x => x != null)
                                                                 .GroupBy(x => x.GetType())
                                                                 .ToDictionary(x => x.Key, x => x.ToList());
            if (itemsByType.ContainsKey(typeof(InventoryItem)))
                _invItemsContainer = new InventoryItemsContainer(itemsByType[typeof(InventoryItem)].Select(x => (InventoryItem)x).ToList());
            if (itemsByType.ContainsKey(typeof(Recipe)))
                _recipesContainer = new RecipesContainer(itemsByType[typeof(Recipe)].Select(x => (Recipe)x).ToList());

            NotifyPropertyChanged("ItemList");
            NotifyPropertyChanged("ProportionDetails");
        }

        public RecipeItem ToRecipeItem()
        {
            return new RecipeItem() { Name = Name, Quantity = Count, InventoryItemId = null };
        }

        private void SetContainers()
        {
            _invItemsContainer = new InventoryItemsContainer(new List<InventoryItem>());
            _recipesContainer = new RecipesContainer(new List<Recipe>());
            List<RecipeItem> items = GetRecipeItems();
            if (items != null)
            {
                SetRecipeItems(items);
            }
        }

        private List<RecipeItem> ConvToRecipeItems(List<IItem> items)
        {
            List<RecipeItem> recItems = new List<RecipeItem>();
            for(int i = 0; i < items.Count; i++)
            {
                RecipeItem ri = items[i].ToRecipeItem();
                ri.Id = i;
                recItems.Add(ri);
            }

            return recItems;
        }

        public IEnumerable<InventoryItem> GetAllItems()
        {
            if (_invItemsContainer == null || _recipesContainer == null)
                SetContainers();
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

        public InventoryItemsContainer GetInvContainer()
        {
            return _invItemsContainer;
        }

        public RecipesContainer GetRecContainer()
        {
            return _recipesContainer;
        }

    }
}
