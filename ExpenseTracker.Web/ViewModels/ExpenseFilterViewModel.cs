using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ExpenseTracker.Web.ViewModels
{
    public class ExpenseFilterViewModel
    {
        public string? Search { get; set; }
        public int? CategoryId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        
        [Range(0.01, double.MaxValue, ErrorMessage = "Minimum amount must be greater than 0")]
        public decimal? MinAmount { get; set; }
        
        [Range(0.01, double.MaxValue, ErrorMessage = "Maximum amount must be greater than 0")]
        public decimal? MaxAmount { get; set; }
        
        public string SortBy { get; set; } = "ExpenseDate";
        public string SortOrder { get; set; } = "desc";
        
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public SelectList? Categories { get; set; }
        
        // Quick filter options
        public string? QuickFilter { get; set; } // today, week, month, year
        
        // Validation
        public bool IsValid()
        {
            if (FromDate.HasValue && ToDate.HasValue && FromDate > ToDate)
                return false;
                
            if (MinAmount.HasValue && MaxAmount.HasValue && MinAmount > MaxAmount)
                return false;
                
            return true;
        }
        
        // Helper methods for UI
        public bool HasActiveFilters => 
            !string.IsNullOrEmpty(Search) || 
            CategoryId.HasValue || 
            FromDate.HasValue || 
            ToDate.HasValue || 
            MinAmount.HasValue || 
            MaxAmount.HasValue ||
            !string.IsNullOrEmpty(QuickFilter);
    }
}


