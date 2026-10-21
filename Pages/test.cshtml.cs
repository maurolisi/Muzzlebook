using System.Data;
using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Muzzlebook.Models;

namespace Muzzlebook.Pages;

public class testModel : PageModel
{
    private readonly IDbConnection _db;

    public testModel(IDbConnection db)
    {
        _db = db;
    }

    public IEnumerable<Dog> DogsList { get; set; } = Enumerable.Empty<Dog>();

    [BindProperty]
    public Dog NewDog { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdStr, out int userId))
        {
            var sql = "SELECT * FROM Dogs WHERE OwnerId = @OwnerId ORDER BY CreatedAt DESC;";
            DogsList = await _db.QueryAsync<Dog>(sql, new { OwnerId = userId });
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
        {
            return Challenge();
        }

        NewDog.OwnerId = userId; // Assegnazione corretta di int

        var sql = @"INSERT INTO Dogs (OwnerId, Name, Breed, DateOfBirth, Gender, Microchip, CurrentWeightKg)
                    VALUES (@OwnerId, @Name, @Breed, @DateOfBirth, @Gender, @Microchip, @CurrentWeightKg);";

        await _db.ExecuteAsync(sql, NewDog);
        return RedirectToPage();
    }

}