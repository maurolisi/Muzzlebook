using System.Data;
using Dapper;
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
        // 🔍 LETTURA: Recupera tutti i cani memorizzati nel database SQLite
        var sql = "SELECT * FROM Dogs ORDER BY CreatedAt DESC;";
        DogsList = await _db.QueryAsync<Dog>(sql);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // ✍️ SCRITTURA: Inserisci un nuovo record nella tabella Dogs usando Dapper
        var sql = @"
            INSERT INTO Dogs (OwnerId, Name, Breed, DateOfBirth, Gender, Microchip, CurrentWeightKg)
            VALUES (@OwnerId, @Name, @Breed, @DateOfBirth, @Gender, @Microchip, @CurrentWeightKg);";

        // Assegniamo temporaneamente un ID utente di prova
        NewDog.OwnerId = "User_Test_123";

        await _db.ExecuteAsync(sql, NewDog);

        return RedirectToPage();
    }
}