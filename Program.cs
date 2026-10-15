using System.Data;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

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

app.UseAuthorization();

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IDbConnection>();
    Muzzlebook.Data.DatabaseInitializer.Initialize(db);
}

app.Run();