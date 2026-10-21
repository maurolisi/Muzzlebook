using System.Data;
using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity; // Per PasswordHasher
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Muzzlebook.Models;

namespace Muzzlebook.Pages;

public class RegisterModel : PageModel
{
    private readonly IDbConnection _db;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public RegisterModel(IDbConnection db) => _db = db;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        // Controllo email esistente
        var existingUser = await _db.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Email = @Email;", new { Input.Email });

        if (existingUser != null)
        {
            ModelState.AddModelError("Input.Email", "Email già registrata.");
            return Page();
        }

        var newUser = new User
        {
            FullName = Input.FullName,
            Email = Input.Email
        };

        newUser.PasswordHash = _passwordHasher.HashPassword(newUser, Input.Password);

        var sql = @"INSERT INTO Users (Id, Email, PasswordHash, FullName) 
                    VALUES (@Id, @Email, @PasswordHash, @FullName);";

        await _db.ExecuteAsync(sql, newUser);

        // Effettua il login automatico dopo la registrazione
        await SignInUserAsync(newUser);

        return RedirectToPage("/Index");
    }

    private async Task SignInUserAsync(User user)
    {
        var claims = new List<Claim>
    {
        // Converte l'int in stringa per memorizzarlo nei Claim
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.FullName),
        new Claim(ClaimTypes.Email, user.Email)
    };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }
}
