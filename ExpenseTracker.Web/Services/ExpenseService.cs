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
                // Apply quick filters
                var fromDate = filter.FromDate;
                var toDate = filter.ToDate;
                
                if (!string.IsNullOrEmpty(filter.QuickFilter))
                {
                    var now = DateTime.Today;
                    switch (filter.QuickFilter.ToLower())
                    {
                        case "today":
                            fromDate = now;
                            toDate = now;
                            break;
                        case "week":
                            fromDate = now.AddDays(-(int)now.DayOfWeek);
                            toDate = fromDate.Value.AddDays(6);
                            break;
                        case "month":
                            fromDate = new DateTime(now.Year, now.Month, 1);
                            toDate = fromDate.Value.AddMonths(1).AddDays(-1);
                            break;
                        case "year":
                            fromDate = new DateTime(now.Year, 1, 1);
                            toDate = new DateTime(now.Year, 12, 31);
                            break;
                    }
                }
                
                var (items, total) = await _repository.SearchAsync(
                    userId, 
                    filter.Search, 
                    filter.CategoryId, 
                    fromDate, 
                    toDate, 
                    filter.MinAmount, 
                    filter.MaxAmount, 
                    filter.SortBy, 
                    filter.SortOrder, 
                    filter.Page, 
                    filter.PageSize);
                    
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
                expense.IsActive = true;
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
            var expenseList = expenses.ToList();
            
            using var workbook = new XLWorkbook();
            var ws = workbook.AddWorksheet("Expense Report");
            
            // Title and metadata
            ws.Cell("A1").Value = "Expense Report";
            ws.Cell("A1").Style.Font.FontSize = 18;
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontColor = XLColor.DarkBlue;
            
            ws.Cell("A2").Value = $"Generated on: {DateTime.Now:MMMM dd, yyyy HH:mm}";
            ws.Cell("A2").Style.Font.FontSize = 10;
            ws.Cell("A2").Style.Font.FontColor = XLColor.Gray;
            
            ws.Cell("A3").Value = $"Total Records: {expenseList.Count}";
            ws.Cell("A3").Style.Font.FontSize = 10;
            
            ws.Cell("A4").Value = $"Total Amount: {expenseList.Sum(e => e.Amount):C2}";
            ws.Cell("A4").Style.Font.FontSize = 12;
            ws.Cell("A4").Style.Font.Bold = true;
            ws.Cell("A4").Style.Font.FontColor = XLColor.DarkGreen;
            
            // Headers
            var headerRow = 6;
            ws.Cell(headerRow, 1).Value = "Date";
            ws.Cell(headerRow, 2).Value = "Category";
            ws.Cell(headerRow, 3).Value = "Amount";
            ws.Cell(headerRow, 4).Value = "Description";
            
            // Style headers
            var headerRange = ws.Range($"A{headerRow}:D{headerRow}");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Fill.BackgroundColor = XLColor.DarkBlue;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thick;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            
            // Data rows
            var dataStartRow = headerRow + 1;
            var currentRow = dataStartRow;
            
            foreach (var expense in expenseList.OrderByDescending(e => e.ExpenseDate))
            {
                ws.Cell(currentRow, 1).Value = expense.ExpenseDate;
                ws.Cell(currentRow, 1).Style.DateFormat.Format = "mmm dd, yyyy";
                
                ws.Cell(currentRow, 2).Value = expense.Category ?? "Uncategorized";
                
                ws.Cell(currentRow, 3).Value = expense.Amount;
                ws.Cell(currentRow, 3).Style.NumberFormat.Format = "$#,##0.00";
                
                ws.Cell(currentRow, 4).Value = expense.Description ?? "";
                
                // Alternate row colors
                if ((currentRow - dataStartRow) % 2 == 1)
                {
                    ws.Range($"A{currentRow}:D{currentRow}").Style.Fill.BackgroundColor = XLColor.LightGray;
                }
                
                currentRow++;
            }
            
            // Style data range
            if (expenseList.Any())
            {
                var dataRange = ws.Range($"A{dataStartRow}:D{currentRow - 1}");
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }
            
            // Summary section
            if (expenseList.Any())
            {
                var summaryRow = currentRow + 2;
                ws.Cell(summaryRow, 3).Value = "TOTAL:";
                ws.Cell(summaryRow, 3).Style.Font.Bold = true;
                ws.Cell(summaryRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                
                ws.Cell(summaryRow, 4).Value = expenseList.Sum(e => e.Amount);
                ws.Cell(summaryRow, 4).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(summaryRow, 4).Style.Font.Bold = true;
                ws.Cell(summaryRow, 4).Style.Font.FontColor = XLColor.DarkGreen;
                ws.Cell(summaryRow, 4).Style.Border.TopBorder = XLBorderStyleValues.Double;
            }
            
            // Auto-fit columns
            ws.Columns().AdjustToContents();
            
            // Set column widths
            ws.Column(1).Width = 12; // Date
            ws.Column(2).Width = 20; // Category
            ws.Column(3).Width = 12; // Amount
            ws.Column(4).Width = 40; // Description
            
            // Freeze header row
            ws.SheetView.FreezeRows(headerRow);
            
            using var stream = new System.IO.MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExportToPdf(IEnumerable<Expense> expenses)
        {
            var expenseList = expenses.ToList();
            var totalAmount = expenseList.Sum(e => e.Amount);
            var exportDate = DateTime.Now;
            
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));
                    
                    // Header
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(column =>
                        {
                            column.Item().Text("Expense Report").FontSize(24).Bold().FontColor(Colors.Blue.Darken2);
                            column.Item().Text($"Generated on {exportDate:MMMM dd, yyyy}").FontSize(10).FontColor(Colors.Grey.Darken1);
                        });
                        
                        row.ConstantItem(120).Column(column =>
                        {
                            column.Item().AlignRight().Text("ExpenseTracker").FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                            column.Item().AlignRight().Text("Financial Management").FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                    });
                    
                    // Content
                    page.Content().Column(column =>
                    {
                        // Summary Section
                        column.Item().PaddingVertical(20).Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Report Summary").FontSize(14).Bold().FontColor(Colors.Blue.Darken1);
                                col.Item().PaddingTop(5).Text($"Total Expenses: {expenseList.Count}").FontSize(10);
                                col.Item().Text($"Date Range: {(expenseList.Any() ? $"{expenseList.Min(e => e.ExpenseDate):MMM dd, yyyy} - {expenseList.Max(e => e.ExpenseDate):MMM dd, yyyy}" : "No data")}").FontSize(10);
                            });
                            
                            row.ConstantItem(150).Column(col =>
                            {
                                col.Item().AlignRight().Text("Total Amount").FontSize(12).Bold();
                                col.Item().AlignRight().Text(totalAmount.ToString("C2")).FontSize(18).Bold().FontColor(Colors.Green.Darken2);
                            });
                        });
                        
                        // Divider
                        column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                        
                        // Expenses Table
                        if (expenseList.Any())
                        {
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(80);  // Date
                                    columns.RelativeColumn(2);   // Category
                                    columns.ConstantColumn(80);  // Amount
                                    columns.RelativeColumn(3);   // Description
                                });
                                
                                // Table Header
                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Date").Bold();
                                    header.Cell().Element(CellStyle).Text("Category").Bold();
                                    header.Cell().Element(CellStyle).AlignRight().Text("Amount").Bold();
                                    header.Cell().Element(CellStyle).Text("Description").Bold();
                                    
                                    static IContainer CellStyle(IContainer container)
                                    {
                                        return container
                                            .Border(1)
                                            .BorderColor(Colors.Grey.Lighten1)
                                            .Background(Colors.Grey.Lighten3)
                                            .Padding(8);
                                    }
                                });
                                
                                // Table Rows
                                foreach (var expense in expenseList.OrderByDescending(e => e.ExpenseDate))
                                {
                                    table.Cell().Element(CellStyle).Text(expense.ExpenseDate.ToString("MMM dd, yyyy"));
                                    table.Cell().Element(CellStyle).Text(expense.Category ?? "Uncategorized");
                                    table.Cell().Element(CellStyle).AlignRight().Text(expense.Amount.ToString("C2"));
                                    table.Cell().Element(CellStyle).Text(expense.Description ?? "");
                                    
                                    static IContainer CellStyle(IContainer container)
                                    {
                                        return container
                                            .Border(1)
                                            .BorderColor(Colors.Grey.Lighten2)
                                            .Padding(8);
                                    }
                                }
                            });
                        }
                        else
                        {
                            column.Item().PaddingVertical(40).AlignCenter().Column(col =>
                            {
                                col.Item().Text("No expenses found").FontSize(14).FontColor(Colors.Grey.Darken1);
                                col.Item().Text("Try adjusting your filter criteria").FontSize(10).FontColor(Colors.Grey.Medium);
                            });
                        }
                    });
                    
                    // Footer
                    page.Footer().AlignCenter().DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Medium)).Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                        text.Span(" • Generated by ExpenseTracker • ");
                        text.Span(exportDate.ToString("yyyy-MM-dd HH:mm"));
                    });
                });
            });
            
            return doc.GeneratePdf();
        }
    }
}


