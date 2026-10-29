using Amazon.S3;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Muzzlebook.Helpers;
using Muzzlebook.Models;
using System.Data;
using System.Security.Claims;

namespace Muzzlebook.Pages.Musi
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IDbConnection _db;
        private readonly IAmazonS3 _s3Client;

        // ✅ UNICO COSTRUTTORE: Inietta sia il DB che il client S3 insieme
        public IndexModel(IDbConnection db, IAmazonS3 s3Client)
        {
            _db = db;
            _s3Client = s3Client;
        }

        public IEnumerable<Dog> DogsList { get; set; } = Enumerable.Empty<Dog>();

        public async Task OnGetAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                var sql = "SELECT * FROM Dogs WHERE OwnerId = @OwnerId ORDER BY CreatedAt DESC;";
                DogsList = await _db.QueryAsync<Dog>(sql, new { OwnerId = userId });
            }

        }
        public async Task<IActionResult> OnPostUploadPhotoAsync(int dogId, IFormFile photo)
        {
            // Elimina la cartella del cane da R2
            await R2StorageHelper.DeleteDogFolderAsync(_s3Client, dogId);
            //poi aggiungo la nuova foto
            string? newUrl = await R2StorageHelper.UploadProfilePhotoAsync(_s3Client, dogId, photo);

            if (newUrl == null)
                return BadRequest();
            string sql = "UPDATE Dogs SET ProfileImageUrl = @ProfilePhotoUrl WHERE Id = @DogId;";
            await _db.ExecuteAsync(sql, new { ProfilePhotoUrl = newUrl, DogId = dogId });

            return new JsonResult(new { newUrl });
        }
        public async Task<IActionResult> OnGetDeletePhotoAsync(int dogId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }
            // Verifica che il cane appartenga all'utente
            var sqlCheck = "SELECT COUNT(1) FROM Dogs WHERE Id = @DogId AND OwnerId = @OwnerId;";
            var count = await _db.ExecuteScalarAsync<int>(sqlCheck, new { DogId = dogId, OwnerId = userId });
            if (count == 0)
            {
                return NotFound();
            }
            // Elimina la foto dal database (non da R2, perché non abbiamo l'URL completo)
            var sqlUpdate = "UPDATE Dogs SET ProfileImageUrl = NULL WHERE Id = @DogId;";
            await _db.ExecuteAsync(sqlUpdate, new { DogId = dogId });
            // Elimina la cartella del cane da R2
            await R2StorageHelper.DeleteDogFolderAsync(_s3Client, dogId);
            return new JsonResult(new { success = true });
        }
    }

}
