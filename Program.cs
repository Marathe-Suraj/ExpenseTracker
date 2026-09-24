using System.Globalization;
using System.Text;
using ExpenseTracker.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Configure culture for Indian Rupee
var cultureInfo = new CultureInfo("en-IN");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Configure QuestPDF to use the free Community license
QuestPDF.Settings.License = LicenseType.Community;

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add localization services
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { cultureInfo };
    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(cultureInfo);
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

builder.Services.AddHttpContextAccessor();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ExpenseTracker";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ExpenseTracker.Maui";

// Cookie auth for MVC + JWT Bearer for /api
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "Cookies";
        options.DefaultAuthenticateScheme = "Cookies";
        options.DefaultChallengeScheme = "Cookies";
    })
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
        // Don't redirect API clients to login HTML
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
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
builder.Services.AddScoped<ExpenseTracker.Services.IAuthService, ExpenseTracker.Services.AuthService>();
builder.Services.AddScoped<ExpenseTracker.Services.IJwtTokenService, ExpenseTracker.Services.JwtTokenService>();
builder.Services.AddScoped<ExpenseTracker.Services.ICategoryService, ExpenseTracker.Services.CategoryService>();
builder.Services.AddScoped<ExpenseTracker.Services.IExpenseService, ExpenseTracker.Services.ExpenseService>();
builder.Services.AddScoped<ExpenseTracker.Services.IDashboardService, ExpenseTracker.Services.DashboardService>();

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

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
