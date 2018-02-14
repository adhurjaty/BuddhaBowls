
using System.Collections.Generic;

namespace BuddhaBowls.Models
{
    public interface IItem : ISortable
    {
        int Id { get; set; }
        //new string Name { get; set; }
        string Category { get; set; }
        string RecipeUnit { get; set; }
        float? RecipeUnitConversion { get; set; }
        float Count { get; set; }
        string Measure { get; set; }

        float CostPerRU { get; }
        float RecipeCost { get; }

        float GetCost();
        void Destroy();
        IItem Copy();
        Dictionary<string, float> GetCategoryCosts();
        RecipeItem ToRecipeItem();
    }
}
