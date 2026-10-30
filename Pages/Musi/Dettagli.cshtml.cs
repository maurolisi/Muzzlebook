using System.Data;
using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Muzzlebook.Models;
using Muzzlebook.Models.DTOs;

namespace Muzzlebook.Pages.Musi
{
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
            var sql = @"
            SELECT 
                d.*, 
                d.[Id], 
                o.[UserId]
            FROM [Dogs] AS d
            INNER JOIN [DogOwners] AS o 
                ON o.[DogId] = d.[Id]
            WHERE d.[Id] =@Id   
               And o.[UserId] = @UserId;";
            var dog = await _db.QueryFirstOrDefaultAsync<Dog>(sql, new { Id = id, UserId = userId });

            // Se il cane non esiste o non è del proprietario, torna alla lista
            if (dog == null)
            {
                return RedirectToPage("/Musi/Index");
            }

            CurrentDog = dog;
            return Page();
        }

        public async Task<IActionResult> OnGetDatiFascicoloAsync(int id, string sezione)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }

            switch (sezione?.ToLower())
            {
                case "vaccinazioni":
                    var vacc = await _db.QueryAsync<VaccinationDto>(@"
                SELECT *
                FROM Vaccinations 
                WHERE DogId = @Id 
                ORDER BY AdministeredDate DESC", new { Id = id });
                    return Partial("Partials/_ListaVaccinazioni", vacc);

                case "parassitari":
                    var parassitari = await _db.QueryAsync<ParasiteTreatmentDto>(@"
                SELECT *
                FROM ParasiteTreatments 
                WHERE DogId = @Id 
                ORDER BY AdministeredDate DESC", new { Id = id });
                    return Partial("Partials/_ListaParassitari", parassitari);

                case "visite":
                    var visite = await _db.QueryAsync<MedicalVisitDto>(@"
                SELECT *
                FROM MedicalVisits 
                WHERE DogId = @Id 
                ORDER BY VisitDate DESC", new { Id = id });
                    return Partial("Partials/_ListaVisite", visite);

                case "documenti":
                    var documenti = await _db.QueryAsync<ClinicalDocumentDto>(@"
                SELECT * 
                FROM ClinicalDocuments 
                WHERE DogId = @Id 
                ORDER BY id DESC", new { Id = id });
                    return Partial("Partials/_ListaDocumenti", documenti);

                default:
                    return BadRequest("Sezione non valida");
            }
        }
    }
}
