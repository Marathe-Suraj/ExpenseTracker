using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using ExpenseTracker.Web.Data.Repositories;
using ExpenseTracker.Web.Models;
using ExpenseTracker.Web.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Web.Services
{
    public interface IExpenseService
    {
        Task<PagedResult<Expense>> SearchAsync(int userId, ExpenseFilterViewModel filter);
        Task<Expense?> GetAsync(int userId, int id);
        Task<int> CreateAsync(Expense expense);
        Task<bool> UpdateAsync(Expense expense);
        Task<bool> DeleteAsync(int userId, int id);
        byte[] ExportToExcel(IEnumerable<Expense> expenses);
        byte[] ExportToPdf(IEnumerable<Expense> expenses);
    }

    public class ExpenseService : IExpenseService
    {
        private readonly IExpenseRepository _repository;
        private readonly ILogger<ExpenseService> _logger;

        public ExpenseService(IExpenseRepository repository, ILogger<ExpenseService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<PagedResult<Expense>> SearchAsync(int userId, ExpenseFilterViewModel filter)
        {
            try
            {
                var (items, total) = await _repository.SearchAsync(userId, filter.Search, filter.CategoryId, filter.FromDate, filter.ToDate, filter.Page, filter.PageSize);
                return new PagedResult<Expense>
                {
                    Items = items.ToList(),
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                    TotalCount = total
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search expenses for user {UserId}", userId);
                throw;
            }
        }

        public async Task<Expense?> GetAsync(int userId, int id)
        {
            try
            {
                return await _repository.GetByIdAsync(userId, id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get expense {Id} for user {UserId}", id, userId);
                throw;
            }
        }

        public async Task<int> CreateAsync(Expense expense)
        {
            try
            {
                return await _repository.CreateAsync(expense);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create expense for user {UserId}", expense.UserId);
                throw;
            }
        }

        public async Task<bool> UpdateAsync(Expense expense)
        {
            try
            {
                return await _repository.UpdateAsync(expense);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update expense {Id} for user {UserId}", expense.ExpenseId, expense.UserId);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int userId, int id)
        {
            try
            {
                return await _repository.DeleteAsync(userId, id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete expense {Id} for user {UserId}", id, userId);
                throw;
            }
        }

        public byte[] ExportToExcel(IEnumerable<Expense> expenses)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.AddWorksheet("Expenses");
            ws.Cell(1, 1).Value = "Date";
            ws.Cell(1, 2).Value = "Category";
            ws.Cell(1, 3).Value = "Amount";
            ws.Cell(1, 4).Value = "Description";
            var row = 2;
            foreach (var e in expenses)
            {
                ws.Cell(row, 1).Value = e.ExpenseDate;
                ws.Cell(row, 2).Value = e.Category;
                ws.Cell(row, 3).Value = e.Amount;
                ws.Cell(row, 4).Value = e.Description;
                row++;
            }
            using var stream = new System.IO.MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExportToPdf(IEnumerable<Expense> expenses)
        {
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Header().Text("Expenses").SemiBold().FontSize(20);
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(80);
                            columns.RelativeColumn(2);
                            columns.ConstantColumn(100);
                            columns.RelativeColumn(3);
                        });
                        table.Header(header =>
                        {
                            header.Cell().Text("Date");
                            header.Cell().Text("CategoryId");
                            header.Cell().Text("Amount");
                            header.Cell().Text("Description");
                        });
                        foreach (var e in expenses)
                        {
                            table.Cell().Text(e.ExpenseDate.ToShortDateString());
                            table.Cell().Text(e.CategoryId.ToString());
                            table.Cell().Text(e.Amount.ToString("C2"));
                            table.Cell().Text(e.Description);
                        }
                    });
                });
            });
            return doc.GeneratePdf();
        }
    }
}


