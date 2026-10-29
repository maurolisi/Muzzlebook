using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;// 1. Namespace da aggiungere
using Microsoft.Data.Sqlite;
using System.Data;
using Amazon.S3;

var builder = WebApplication.CreateBuilder(args);
// 2. Definizione della cartella permanente per le chiavi (es. Data/Keys)
var keysFolder = Path.Combine(builder.Environment.ContentRootPath, "Data", "Keys");
if (!Directory.Exists(keysFolder))
{
    Directory.CreateDirectory(keysFolder);
}

// 3. Configurazione di Data Protection
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysFolder))
    .SetApplicationName("Muzzlebook"); // Identificativo costante dell'applicazione 

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



// Leggi la configurazione di Cloudflare R2 (da User Secrets o Variabili d'Ambiente)
var r2Section = builder.Configuration.GetSection("CloudflareR2");
var accountId = r2Section["AccountId"];
var accessKeyId = r2Section["AccessKeyId"];
var secretAccessKey = r2Section["SecretAccessKey"];




// Configura l'endpoint S3 per puntare a Cloudflare R2
var s3Config = new AmazonS3Config
{
    ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
    AuthenticationRegion = "auto"
};

// Registra IAmazonS3 in modo che sia disponibile in tutto il progetto
builder.Services.AddSingleton<IAmazonS3>(new AmazonS3Client(accessKeyId, secretAccessKey, s3Config));









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