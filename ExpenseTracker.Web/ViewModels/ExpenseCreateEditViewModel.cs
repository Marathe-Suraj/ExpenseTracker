using ExpenseTracker.Web.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ExpenseTracker.Web.ViewModels
{
    public class ExpenseCreateEditViewModel
    {
        public Expense Expense { get; set; } = new Expense();
        public SelectList Categories { get; set; } = new SelectList(Enumerable.Empty<Category>(), nameof(Category.CategoryId), nameof(Category.Name));
        public bool IsEdit { get; set; }
    }
}
