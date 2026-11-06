using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;

namespace Muzzlebook.Pages
{
    [Authorize]
    public class MappaAmiciModel : PageModel
    {
        private readonly IDbConnection _db;

        public MappaAmiciModel(IDbConnection db)
        {
            _db = db;
        }

        // DTO per inviare i dati formattati al Javascript
        public class CaneAttivoDto
        {
            public int DogId { get; set; }
            public string Name { get; set; } = string.Empty;
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public DateTime Timestamp { get; set; }
            public string? ProfileImageUrl { get; set; }
        }

        public void OnGet()
        {
            // Carica la pagina vuota con la struttura HTML
        }

        // Endpoint chiamato ogni minuto dalla fetch JavaScript
        public async Task<IActionResult> OnGetPosizioniAttiveAsync()
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");
            var oraItaliana = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

            // Calcola la soglia temporale: 5 minuti fa
            var limiteTemporale = oraItaliana.AddMinutes(-5);

            var sql = @"
                SELECT 
                    d.Id AS DogId, 
                    d.Name, 
                    p.Latitude, 
                    p.Longitude, 
                    p.Timestamp, 
                    d.ProfileImageUrl
                FROM DogLastPosition p
                INNER JOIN Dogs d ON p.DogId = d.Id
                WHERE p.Timestamp >= @LimiteTemporale;";

            var caniAttivi = await _db.QueryAsync<CaneAttivoDto>(sql, new { LimiteTemporale = limiteTemporale });

            return new JsonResult(caniAttivi);
        }
    }
}