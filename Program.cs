using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartStock.Data;
using SmartStock.Models;

var builder = WebApplication.CreateBuilder(args);

// ================= MVC =================
builder.Services.AddControllersWithViews();

// ================= DOMAIN DB CONTEXT =================
builder.Services.AddDbContext<SmartStockDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ================= IDENTITY DB CONTEXT =================
builder.Services.AddDbContext<SmartStockIdentityDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ================= ASP.NET IDENTITY =================
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Optional but recommended defaults
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<SmartStockIdentityDbContext>()
    .AddDefaultTokenProviders();

// 🔑 THIS IS THE IMPORTANT PART
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";

    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

var app = builder.Build();

// ================= PIPELINE =================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ⚠️ ORDER IS CORRECT – DO NOT CHANGE
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
