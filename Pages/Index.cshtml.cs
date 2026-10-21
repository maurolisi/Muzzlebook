using System.Data;
using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Muzzlebook.Models;

namespace Muzzlebook.Pages;

public class IndexModel : PageModel
{
    private readonly IDbConnection _db;

    public IndexModel(IDbConnection db)
    {
        _db = db;
    }

    public IEnumerable<Dog> DogsList { get; set; } = Enumerable.Empty<Dog>();

    [BindProperty]
    public Dog NewDog { get; set; } = new();

    public async Task OnGetAsync()
    {
        // Recupera l'ID dell'utente loggato come stringa dai Claim e convertilo in int
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdStr, out int userId))
        {
            // Filtra i cani appartenenti solo all'utente loggato
            var sql = "SELECT * FROM Dogs WHERE OwnerId = @OwnerId ORDER BY CreatedAt DESC;";
            DogsList = await _db.QueryAsync<Dog>(sql, new { OwnerId = userId });
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        // Recupera l'ID utente loggato dai Claim
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
        {
            return Challenge();
        }

        // Assegna il valore int convertito
        NewDog.OwnerId = userId;

        var sql = @"INSERT INTO Dogs (OwnerId, Name, Breed, DateOfBirth, Gender, Microchip, CurrentWeightKg)
                    VALUES (@OwnerId, @Name, @Breed, @DateOfBirth, @Gender, @Microchip, @CurrentWeightKg);";

        await _db.ExecuteAsync(sql, NewDog);
        return RedirectToPage();
    }

    // Handler per la cancellazione di un cane
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdStr, out int userId))
        {
            var sql = "DELETE FROM Dogs WHERE Id = @Id AND OwnerId = @OwnerId;";
            await _db.ExecuteAsync(sql, new { Id = id, OwnerId = userId });
        }

        return new OkResult();
    }

    // Handler per il Logout
    public async Task<IActionResult> OnGetLogoutAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Login");
    }
}