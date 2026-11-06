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

        // DTO per mappare i dati in arrivo dalla fetch
        public class FissaPosizioneDto
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public bool Fissa { get; set; }
        }

        // Handler POST per gestire l'attivazione o la rimozione della posizione fissa
        public async Task<IActionResult> OnPostFissaPosizioneAsync([FromBody] FissaPosizioneDto data)
        {
            if (data == null)
            {
                return BadRequest("Payload non valido o vuoto.");
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }
            // Trova il primo cane associato all'utente loggato (in ordine crescente di ID, come richiesto)
            var sqlDog = @"
            SELECT DogId 
            FROM DogOwners 
            WHERE UserId = @UserId 
            ORDER BY DogId desc 
            LIMIT 1;";
            var dogId = await _db.ExecuteScalarAsync<int?>(sqlDog, new { UserId = userId });
            if (data.Fissa)
            {
                if (!dogId.HasValue)
                {
                    return BadRequest("Nessun cane associato all'utente.");
                }
                var sqlUpsert = @"
            INSERT INTO DogLastPosition (DogId, Latitude, Longitude, Timestamp) 
            VALUES (@DogId, @Latitude, @Longitude, '2050-01-01 00:00:00')
            ON CONFLICT(DogId) DO UPDATE SET 
                Latitude = excluded.Latitude, 
                Longitude = excluded.Longitude, 
                Timestamp = '2050-01-01 00:00:00';";

                await _db.ExecuteAsync(sqlUpsert, new { DogId = dogId.Value, data.Latitude, data.Longitude });
            }
            else
            {
                // Cancella le posizioni di TUTTI i cani appartenenti a questo utente
                var sqlDelete = @"
            DELETE FROM DogLastPosition 
            WHERE DogId = @DogId;";
                await _db.ExecuteAsync(sqlDelete, new { DogId = dogId.Value });
            }

            return new JsonResult(new { success = true });
        }
    }
}