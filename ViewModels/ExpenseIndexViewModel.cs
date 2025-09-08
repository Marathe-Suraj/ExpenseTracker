using ExpenseTracker.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ExpenseTracker.ViewModels
{
    public class ExpenseIndexViewModel
    {
        public IEnumerable<Expense> Expenses { get; set; } = new List<Expense>();
        public PagedResult<Expense> PagedResult { get; set; } = new PagedResult<Expense>();
        public ExpenseFilterViewModel Filter { get; set; } = new ExpenseFilterViewModel();
        public Dictionary<int, string> CategoryLookup { get; set; } = new Dictionary<int, string>();
    }
}
