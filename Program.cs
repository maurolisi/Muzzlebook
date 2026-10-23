using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

// Configurazione Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Index?handler=Logout";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(15); // Allineato al timer di sessione lato JS
        options.SlidingExpiration = true;
    });
// Aggiungi supporto Razor Pages
builder.Services.AddRazorPages();

// Registra la connessione SQLite per Dapper
builder.Services.AddScoped<IDbConnection>(sp =>
    new SqliteConnection(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

//using (var scope = app.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<IDbConnection>();
//    Muzzlebook.Data.DatabaseInitializer.Initialize(db);
//}

app.Run();