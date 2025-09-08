using ExpenseTracker.Models;

namespace ExpenseTracker.ViewModels
{
    public class CategoryIndexViewModel
    {
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();
        public int ActiveCategoriesCount { get; set; }
        public int InactiveCategoriesCount { get; set; }
        public int TotalCategoriesCount { get; set; }
    }
}
