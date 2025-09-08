using System;
using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Models
{
    public class Expense
    {
        public int ExpenseId { get; set; }
        public int UserId { get; set; }
        [Required(ErrorMessage = "Category is required")]
        public int CategoryId { get; set; }
        public string? Category { get; set; }
        [Range(typeof(decimal), "0.01", "1000000000", ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }
        [Required(ErrorMessage = "Description is required")]
        [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
        public string Description { get; set; } = string.Empty;
        [DataType(DataType.Date)]
        [Required(ErrorMessage = "Date is required")]
        public DateTime ExpenseDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }
}


