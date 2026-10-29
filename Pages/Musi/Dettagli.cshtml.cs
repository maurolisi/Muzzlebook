using System.Data;
using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Muzzlebook.Models;

namespace Muzzlebook.Pages.Musi;

[Authorize]
public class DettagliModel : PageModel
{
    private readonly IDbConnection _db;

    public DettagliModel(IDbConnection db)
    {
        _db = db;
    }

    public Dog CurrentDog { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        // Recupera l'ID dell'utente autenticato
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId))
        {
            return RedirectToPage("/Login");
        }

        // Recupera il cane, assicurandosi che appartenga all'utente corrente
        var sql = "SELECT * FROM Dogs WHERE Id = @Id AND OwnerId = @OwnerId;";
        var dog = await _db.QueryFirstOrDefaultAsync<Dog>(sql, new { Id = id, OwnerId = userId });

        // Se il cane non esiste o non è del proprietario, torna alla lista
        if (dog == null)
        {
            return RedirectToPage("/Musi/Index");
        }

        CurrentDog = dog;
        return Page();
    }
}