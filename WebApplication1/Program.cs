using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using WebApplication1.Data;
using WebApplication1.Service;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Add session services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// existing DI registrations (DbContext, services, password hasher)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<BazaDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<IPasswordHasher<WebApplication1.Models.User>, PasswordHasher<WebApplication1.Models.User>>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFilmoviService, FilmoviService>();
builder.Services.AddScoped<IIznajmljivanjeService, IznajmljivanjeService>();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

// enable session middleware before controllers are executed
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();