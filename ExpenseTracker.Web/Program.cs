var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var mvcBuilder = builder.Services.AddControllersWithViews();
if (builder.Environment.IsDevelopment())
{
    mvcBuilder.AddRazorRuntimeCompilation();
}
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
builder.Services.AddSingleton<ExpenseTracker.Web.Data.IDbConnectionFactory, ExpenseTracker.Web.Data.DbConnectionFactory>();
builder.Services.AddScoped<ExpenseTracker.Web.Data.Repositories.IUserRepository, ExpenseTracker.Web.Data.Repositories.UserRepository>();
builder.Services.AddScoped<ExpenseTracker.Web.Data.Repositories.ICategoryRepository, ExpenseTracker.Web.Data.Repositories.CategoryRepository>();
builder.Services.AddScoped<ExpenseTracker.Web.Data.Repositories.IExpenseRepository, ExpenseTracker.Web.Data.Repositories.ExpenseRepository>();
builder.Services.AddScoped<ExpenseTracker.Web.Services.IAuthService, ExpenseTracker.Web.Services.AuthService>();
builder.Services.AddScoped<ExpenseTracker.Web.Services.ICategoryService, ExpenseTracker.Web.Services.CategoryService>();
builder.Services.AddScoped<ExpenseTracker.Web.Services.IExpenseService, ExpenseTracker.Web.Services.ExpenseService>();
builder.Services.AddScoped<ExpenseTracker.Web.Services.IDashboardService, ExpenseTracker.Web.Services.DashboardService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
