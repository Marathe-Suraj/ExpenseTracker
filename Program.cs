using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Configure culture for Indian Rupee
var cultureInfo = new CultureInfo("en-IN");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Configure QuestPDF to use the free Community license
QuestPDF.Settings.License = LicenseType.Community;

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// Add localization services
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { cultureInfo };
    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(cultureInfo);
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

builder.Services.AddHttpContextAccessor();

// Cookie Authentication
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.Cookie.Name = "ExpenseTracker.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// DI registrations
builder.Services.AddSingleton<ExpenseTracker.Data.IDbConnectionFactory, ExpenseTracker.Data.DbConnectionFactory>();
builder.Services.AddScoped<ExpenseTracker.Data.Repositories.IUserRepository, ExpenseTracker.Data.Repositories.UserRepository>();
builder.Services.AddScoped<ExpenseTracker.Data.Repositories.ICategoryRepository, ExpenseTracker.Data.Repositories.CategoryRepository>();
builder.Services.AddScoped<ExpenseTracker.Data.Repositories.IExpenseRepository, ExpenseTracker.Data.Repositories.ExpenseRepository>();
builder.Services.AddScoped<ExpenseTracker.Data.Repositories.IBudgetRepository, ExpenseTracker.Data.Repositories.BudgetRepository>();
builder.Services.AddScoped<ExpenseTracker.Data.Repositories.IIncomeRepository, ExpenseTracker.Data.Repositories.IncomeRepository>();
builder.Services.AddScoped<ExpenseTracker.Data.Repositories.IRecurringExpenseRepository, ExpenseTracker.Data.Repositories.RecurringExpenseRepository>();
builder.Services.AddScoped<ExpenseTracker.Data.Repositories.IHouseholdRepository, ExpenseTracker.Data.Repositories.HouseholdRepository>();
builder.Services.AddScoped<ExpenseTracker.Services.IAuthService, ExpenseTracker.Services.AuthService>();
builder.Services.AddScoped<ExpenseTracker.Services.ICategoryService, ExpenseTracker.Services.CategoryService>();
builder.Services.AddScoped<ExpenseTracker.Services.IExpenseService, ExpenseTracker.Services.ExpenseService>();
builder.Services.AddScoped<ExpenseTracker.Services.IDashboardService, ExpenseTracker.Services.DashboardService>();
builder.Services.AddScoped<ExpenseTracker.Services.IBudgetService, ExpenseTracker.Services.BudgetService>();
builder.Services.AddScoped<ExpenseTracker.Services.IIncomeService, ExpenseTracker.Services.IncomeService>();
builder.Services.AddScoped<ExpenseTracker.Services.IRecurringExpenseService, ExpenseTracker.Services.RecurringExpenseService>();
builder.Services.AddScoped<ExpenseTracker.Services.IRecurringExpenseGenerator, ExpenseTracker.Services.RecurringExpenseGenerator>();
builder.Services.AddScoped<ExpenseTracker.Services.IHouseholdService, ExpenseTracker.Services.HouseholdService>();
builder.Services.AddScoped<ExpenseTracker.Services.IReportsService, ExpenseTracker.Services.ReportsService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

// Use localization
app.UseRequestLocalization();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
