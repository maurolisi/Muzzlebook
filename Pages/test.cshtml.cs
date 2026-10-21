using System.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Muzzlebook.Models;

namespace Muzzlebook.Pages
{
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
            // LETTURA: Recupera tutti i cani
            var sql = "SELECT * FROM Dogs ORDER BY CreatedAt DESC;";
            DogsList = await _db.QueryAsync<Dog>(sql);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // SCRITTURA: Inserimento
            var sql = @"
            INSERT INTO Dogs (OwnerId, Name, Breed, DateOfBirth, Gender, Microchip, CurrentWeightKg)
            VALUES (@OwnerId, @Name, @Breed, @DateOfBirth, @Gender, @Microchip, @CurrentWeightKg);";

            NewDog.OwnerId = "User_Test_123";

            await _db.ExecuteAsync(sql, NewDog);

            return RedirectToPage();
        }

        // 🗑️ CANCELLAZIONE: Handler per rimuovere un cane in base al suo ID
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var sql = "DELETE FROM Dogs WHERE Id = @Id;";
            await _db.ExecuteAsync(sql, new { Id = id });

            //return RedirectToPage();
            return new OkResult();
        }
        public async Task<IActionResult> OnGetDeleteAsync(int id)
        {
            var sql = "DELETE FROM Dogs WHERE Id = @Id;";
            await _db.ExecuteAsync(sql, new { Id = id });

            //return RedirectToPage();
            return new OkResult();
        }
    }
}
