using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Muzzlebook.Models;
using System.Data;
using System.Security.Claims;

namespace Muzzlebook.Pages;

public class LoginModel : PageModel
{
    private readonly IDbConnection _db;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public LoginModel(IDbConnection db) => _db = db;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var user = await _db.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Email = @Email;", new { Input.Email });

        if (user == null || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, Input.Password) == PasswordVerificationResult.Failed)
        {
            ErrorMessage = "Credenziali non valide.";
            return Page();
        }

        // 🌟 Passiamo la scelta dell'utente al metodo di login
        await SignInUserAsync(user, Input.RememberMe);
        return RedirectToPage("/Index");
    }

    /// <summary>
    /// Handler per Login Statico / Rapido (es. per test immediati)
    /// </summary>
    public async Task<IActionResult> OnPostStaticLoginAsync()
    {
        //var staticUserId = "User_Test_123";

        // Verifica se l'utente statico esiste nel DB, altrimenti lo crea al volo
        var user = await _db.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Id = @Id;", new { Id = 1 });

        if (user == null)
        {
            user = new User
            {
             
                Email = "test@muzzlebook.local",
                FullName = "Utente Test Statico",
                PasswordHash = "STATIC_BYPASS"
            };

            await _db.ExecuteAsync(
                "INSERT INTO Users (Id, Email, PasswordHash, FullName) VALUES (@Id, @Email, @PasswordHash, @FullName);",
                user);
        }

        await SignInUserAsync(user);
        return RedirectToPage("/Index");
    }
    /// <summary>
    /// Handler per generare o aggiornare l'utente m@m.it con l'hash corretto per 1q2w3e4r
    /// </summary>
    public async Task<IActionResult> OnGetGenerateTestUserAsync()
    {
        var email = "m@m.it";
        var plainPassword = "1q2w3e4r";

        // 1. Controlla se l'utente esiste già
        var user = await _db.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Email = @Email;", new { Email = email });

        if (user == null)
        {
            user = new User
            {
                Email = email,
                FullName = "Mauro"
            };

            // 2. Genera l'hash valido nativamente tramite PasswordHasher<User>
            user.PasswordHash = _passwordHasher.HashPassword(user, plainPassword);

            // 3. Inserisce il nuovo utente nel DB SQLite
            var insertSql = @"INSERT INTO Users (Email, PasswordHash, FullName) 
                              VALUES (@Email, @PasswordHash, @FullName);";
            await _db.ExecuteAsync(insertSql, user);

            ErrorMessage = $"Utente {email} creato con successo! Ora puoi accedere con password '{plainPassword}'.";
        }
        else
        {
            // Se esiste già, aggiorna la password con l'hash corretto
            user.PasswordHash = _passwordHasher.HashPassword(user, plainPassword);

            var updateSql = "UPDATE Users SET PasswordHash = @PasswordHash WHERE Email = @Email;";
            await _db.ExecuteAsync(updateSql, new { user.PasswordHash, user.Email });

            ErrorMessage = $"Password per {email} aggiornata e formattata con successo per '{plainPassword}'!";
        }

        // Stampa nella console di log per verificare l'hash generato
        Console.WriteLine($"[SETUP] Hash generato da PasswordHasher per {email}: {user.PasswordHash}");

        return Page();
    }


    // Aggiorniamo la firma del metodo per accettare il parametro booleano
    private async Task SignInUserAsync(User user, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName ?? string.Empty),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        // 🌟 Configurazione dinamica in base alla spunta
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            // Se spuntato, dura 30 giorni. Se NON spuntato, scade alla chiusura del browser.
            ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(30) : null
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal, authProperties);
    }
//}


}