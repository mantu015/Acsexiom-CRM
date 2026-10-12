using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services;
using AcxiomCRM.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Database Provider (SQLite default, SQL Server switchable)
var dbProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "Sqlite";
if (dbProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServerConnection")));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection") ?? "Data Source=AcxiomCRM.db"));
}

// 2. Configure ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password policy
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 3. Configure Cookies for Web App
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// 4. Configure JWT Bearer for REST API
var jwtSecret = builder.Configuration["JwtSettings:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret))
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException("Configure JwtSettings:Secret with a secure value before running outside Development.");
    }

    jwtSecret = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
}

builder.Configuration["JwtSettings:Secret"] = jwtSecret;
var key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "AcxiomCRM",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "AcxiomCRMClients",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// 5. Register Application Services (Dependency Injection)
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ILeadService, LeadService>();
builder.Services.AddScoped<IOpportunityService, OpportunityService>();
builder.Services.AddScoped<IFollowUpService, FollowUpService>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IUserService, UserService>();

// 6. Add Controllers & Views
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

var app = builder.Build();

// Seed Database automatically on startup
using (var scope = app.Services.CreateScope())
{
    await DatabaseSeeder.SeedAsync(scope.ServiceProvider);
}

// Configure HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
