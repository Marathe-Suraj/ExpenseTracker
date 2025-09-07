using ClosedXML.Excel;
using ExpenseTracker.Web.Data.Repositories;
using ExpenseTracker.Web.Models;
using ExpenseTracker.Web.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using System.Globalization;

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
            var items = expenses.ToList();
            var generatedAt = DateTime.Now;
            var hasData = items.Any();
            var periodText = hasData
                ? $"{items.Min(e => e.ExpenseDate):yyyy-MM-dd} - {items.Max(e => e.ExpenseDate):yyyy-MM-dd}"
                : "No data available";

            using var workbook = new XLWorkbook();
            var ws = workbook.AddWorksheet("Expenses");

            // Title
            ws.Cell(1, 1).Value = "ExpenseTracker - Expenses Report";
            ws.Range(1, 1, 1, 4).Merge().Style
                .Font.SetBold().Font.SetFontSize(16)
                .Font.SetFontColor(XLColor.FromHtml("#2563EB"));

            // Meta
            ws.Cell(2, 1).Value = $"Generated: {generatedAt:yyyy-MM-dd HH:mm}    Period: {periodText}";
            ws.Range(2, 1, 2, 4).Merge().Style
                .Font.SetFontSize(10)
                .Font.SetFontColor(XLColor.FromHtml("#6B7280"));

            // Header row
            var headerRow = 4;
            ws.Cell(headerRow, 1).Value = "Date";
            ws.Cell(headerRow, 2).Value = "Category";
            ws.Cell(headerRow, 3).Value = "Amount";
            ws.Cell(headerRow, 4).Value = "Description";

            // Data rows
            var row = headerRow + 1;
            foreach (var e in items)
            {
                ws.Cell(row, 1).Value = e.ExpenseDate;
                ws.Cell(row, 2).Value = string.IsNullOrWhiteSpace(e.Category) ? $"#{e.CategoryId}" : e.Category;
                ws.Cell(row, 3).Value = e. Amount;
                ws.Cell(row, 4).Value = e.Description;
                row++;
            }

            // Styling & formats
            ws.Column(1).Width = 16; // Date
            ws.Column(2).Width = 22; // Category
            ws.Column(3).Width = 14; // Amount
            ws.Column(4).Width = 48; // Description

            ws.Column(1).Style.DateFormat.Format = "dd-MM-yyyy";
            var currencySymbol = CultureInfo.CurrentCulture.NumberFormat.CurrencySymbol;
            ws.Column(3).Style.NumberFormat.Format = $"{currencySymbol} #,##0.00";

            // Create table with totals
            var lastDataRow = Math.Max(headerRow + 1, row - 1);
            var range = ws.Range(headerRow, 1, lastDataRow, 4);
            var table = range.CreateTable();
            table.Theme = XLTableTheme.TableStyleMedium9;
            table.ShowTotalsRow = true;
            table.Field("Category").TotalsRowLabel = "Total";
            table.Field("Amount").TotalsRowFunction = XLTotalsRowFunction.Sum;

            // Freeze header
            ws.SheetView.FreezeRows(headerRow);

            // Summary sheet by category
            var summary = workbook.AddWorksheet("Summary");
            summary.Cell(1, 1).Value = "Totals by Category";
            summary.Range(1, 1, 1, 2).Merge().Style.Font.SetBold().Font.SetFontSize(13);
            summary.Cell(3, 1).Value = "Category";
            summary.Cell(3, 2).Value = "Total";
            var r = 4;
            foreach (var g in items
                .GroupBy(x => string.IsNullOrWhiteSpace(x.Category) ? $"#{x.CategoryId}" : x.Category!)
                .Select(g => new { Category = g.Key, Total = g.Sum(x => x.Amount) })
                .OrderByDescending(x => x.Total))
            {
                summary.Cell(r, 1).Value = g.Category;
                summary.Cell(r, 2).Value = g.Total;
                r++;
            }
            summary.Column(1).Width = 30;
            summary.Column(2).Width = 18;
            summary.Column(2).Style.NumberFormat.Format = $"{currencySymbol} #,##0.00";
            var sRange = summary.Range(3, 1, Math.Max(3, r - 1), 2);
            var sTable = sRange.CreateTable();
            sTable.Theme = XLTableTheme.TableStyleMedium2;
            sTable.ShowTotalsRow = true;
            sTable.Field("Category").TotalsRowLabel = "Grand Total";
            sTable.Field("Total").TotalsRowFunction = XLTotalsRowFunction.Sum;

            using var stream = new System.IO.MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExportToPdf(IEnumerable<Expense> expenses)
        {
            var items = expenses.ToList();
            var generatedAt = DateTime.Now;
            var hasData = items.Any();
            var periodText = hasData
                ? $"{items.Min(e => e.ExpenseDate):yyyy-MM-dd} - {items.Max(e => e.ExpenseDate):yyyy-MM-dd}"
                : "No data available";
            var totalAmount = items.Sum(x => x.Amount);
            var totalCount = items.Count;

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);

                    page.Header().Element(header =>
                    {
                        header.Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("ExpenseTracker").FontSize(22).Bold().FontColor(Colors.Blue.Medium);
                                col.Item().Text("Expenses Report").FontSize(14).SemiBold();
                                col.Item().Text(t =>
                                {
                                    t.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken2));
                                    t.Span("Generated: ").SemiBold();
                                    t.Span(generatedAt.ToString("yyyy-MM-dd HH:mm"));
                                    if (hasData)
                                    {
                                        t.Span("    Period: ").SemiBold();
                                        t.Span(periodText);
                                    }
                                });
                            });

                            row.ConstantItem(160).Border(1).BorderColor(Colors.Blue.Medium).Padding(10).AlignCenter().Column(col =>
                            {
                                col.Item().Text("Total").SemiBold().FontSize(10).FontColor(Colors.Grey.Darken2);
                                col.Item().Text(totalAmount.ToString("C2")).Bold().FontSize(16);
                                col.Item().Text($"{totalCount} item{(totalCount == 1 ? "" : "s")}").SemiBold().FontSize(11).FontColor(Colors.Grey.Darken2);
                            });
                        });
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(120); // Date
                                columns.RelativeColumn(2);   // Category
                                columns.RelativeColumn(3);   // Description
                                columns.ConstantColumn(90);  // Amount
                            });

                            table.Header(header =>
                            {
                                header.Cell().Padding(6).Background(Colors.Grey.Lighten3).Text("Date").SemiBold();
                                header.Cell().Padding(6).Background(Colors.Grey.Lighten3).Text("Category").SemiBold();
                                header.Cell().Padding(6).Background(Colors.Grey.Lighten3).Text("Description").SemiBold();
                                header.Cell().Padding(6).Background(Colors.Grey.Lighten3).AlignRight().Text("Amount").SemiBold();
                            });

                            foreach (var e in items)
                            {
                                table.Cell().Padding(6).Text(e.ExpenseDate.ToString("dd-MM-yyyy"));
                                table.Cell().Padding(6).Text(!string.IsNullOrWhiteSpace(e.Category) ? e.Category! : e.CategoryId.ToString());
                                table.Cell().Padding(6).Text(e.Description);
                                table.Cell().Padding(6).AlignRight().Text(e.Amount.ToString("C2"));
                            }

                            table.Footer(footer =>
                            {
                                footer.Cell().ColumnSpan(3).Padding(6).Background(Colors.Grey.Lighten3).AlignRight().Text("Total").SemiBold();
                                footer.Cell().Padding(6).Background(Colors.Grey.Lighten3).AlignRight().Text(totalAmount.ToString("C2")).SemiBold();
                            });
                        });

                        var byCategory = items
                            .GroupBy(x => string.IsNullOrWhiteSpace(x.Category) ? $"#{x.CategoryId}" : x.Category!)
                            .Select(g => new { Category = g.Key, Total = g.Sum(x => x.Amount) })
                            .OrderByDescending(x => x.Total)
                            .ToList();

                        if (byCategory.Any())
                        {
                            col.Item().PaddingTop(16).Text("Totals by Category").SemiBold().FontSize(12);
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(3);
                                    c.ConstantColumn(110);
                                });

                                table.Header(h =>
                                {
                                    h.Cell().Padding(6).Background(Colors.Grey.Lighten3).Text("Category").SemiBold();
                                    h.Cell().Padding(6).Background(Colors.Grey.Lighten3).AlignRight().Text("Total").SemiBold();
                                });

                                foreach (var c in byCategory)
                                {
                                    table.Cell().Padding(6).Text(c.Category);
                                    table.Cell().Padding(6).AlignRight().Text(c.Total.ToString("C2"));
                                }
                            });
                        }
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Page ");
                        t.CurrentPageNumber();
                        t.Span(" of ");
                        t.TotalPages();
                    });
                });
            });

            return doc.GeneratePdf();
        }
    }
}


