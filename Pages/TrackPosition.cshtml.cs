using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
using System.Security.Claims;

namespace Muzzlebook.Pages
{
    // L'attributo garantisce che solo gli utenti loggati possano inviare posizioni
    [Authorize]
    [IgnoreAntiforgeryToken] // Disabilitiamo il token solo per questo endpoint per semplificare la fetch globale
    public class TrackPositionModel : PageModel
    {
        private readonly IDbConnection _db;

        public TrackPositionModel(IDbConnection db)
        {
            _db = db;
        }

        public class GlobalPositionDto
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
        }

        // Questo handler risponderà alla rotta /TrackPosition
        public async Task<IActionResult> OnPostAsync([FromBody] GlobalPositionDto pos)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized();

            // Trova il cane (o il primo cane) associato all'utente loggato
            var sqlDog = @"
                SELECT d.Id 
                FROM Dogs d 
                INNER JOIN DogOwners o ON d.Id = o.DogId 
                WHERE o.UserId = @UserId 
                LIMIT 1;";

            var dogId = await _db.ExecuteScalarAsync<int?>(sqlDog, new { UserId = userId });

            if (dogId.HasValue)
            {
                // 1. Calcola l'ora italiana ignorando il fuso orario del server
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");
                var oraItaliana = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

                // 2. Passa il parametro @OraItaliana alla query
                var sql = @"
                    INSERT INTO DogLastPosition (DogId, Latitude, Longitude, Timestamp) 
                    VALUES (@DogId, @Latitude, @Longitude, @OraItaliana)
                    ON CONFLICT(DogId) DO UPDATE SET 
                        Latitude = excluded.Latitude, 
                        Longitude = excluded.Longitude, 
                        Timestamp = excluded.Timestamp;";

                await _db.ExecuteAsync(sql, new
                {
                    DogId = dogId.Value,
                    pos.Latitude,
                    pos.Longitude,
                    OraItaliana = oraItaliana
                });
            }

            return new JsonResult(new { success = true });
        }
    }
}