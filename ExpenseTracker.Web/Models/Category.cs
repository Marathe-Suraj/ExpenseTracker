using System;

namespace ExpenseTracker.Web.Models
{
    public class Category
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
        public int UserId { get; set; } // Add UserId for user-category mapping
    }
}


