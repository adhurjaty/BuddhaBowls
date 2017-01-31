
namespace BuddhaBowls.Models
{
    public interface IItem
    {
        string Name { get; set; }
        string Category { get; set; }
        string RecipeUnit { get; set; }
        float? RecipeUnitConversion { get; set; }
        float Count { get; set; }

        float GetCost();
        void Destroy();
    }
}
