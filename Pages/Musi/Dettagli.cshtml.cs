using System.Data;
using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Muzzlebook.Models;
using Muzzlebook.Models.DTOs;
using Amazon.S3;
using Muzzlebook.Helpers;

namespace Muzzlebook.Pages.Musi
{
    [Authorize]
    public class DettagliModel : PageModel
    {
        private readonly IDbConnection _db;
        private readonly IAmazonS3 _s3Client;

        // Iniezione di IAmazonS3 necessaria per UpDogMediaAsync
        public DettagliModel(IDbConnection db, IAmazonS3 s3Client)
        {
            _db = db;
            _s3Client = s3Client;
        }

        public Dog CurrentDog { get; set; } = new();

        // Nuova proprietà per ricaricare i file in galleria
        public IEnumerable<MediaDto> DogMedia { get; set; } = Enumerable.Empty<MediaDto>();
        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToPage("/Login");
            }

            var sql = @"
            SELECT 
                d.*, 
                d.[Id], 
                o.[UserId] 
            FROM [Dogs] AS d
            INNER JOIN [DogOwners] AS o 
                ON o.[DogId] = d.[Id]
            WHERE d.[Id] = @Id 
               And o.[UserId] = @UserId;";

            var dog = await _db.QueryFirstOrDefaultAsync<Dog>(sql, new { Id = id, UserId = userId });

            if (dog == null)
            {
                return RedirectToPage("/Musi/Index");
            }

            CurrentDog = dog;

            // RECUPERO MEDIA DEL CANE
            var mediaSql = "SELECT * FROM Media WHERE IdDoc = @IdDoc ORDER BY idMedia DESC;";
            DogMedia = await _db.QueryAsync<MediaDto>(mediaSql, new { IdDoc = id });

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
        public async Task<IActionResult> OnGetOpenProfileImageAsync(int id)
        {
            var sql = @"SELECT * FROM Dogs WHERE Id=@Id";
            var dog = await _db.QueryFirstOrDefaultAsync<DogDto>(sql, new { Id = id });

            return Partial("Partials/_ProfileImage", dog);

        }
        //public async Task<IActionResult> OnPostUploadMediaAsync(int dogId, IFormFile mediaFile)
        //{
        //    if (mediaFile == null || mediaFile.Length == 0)
        //        return BadRequest("File non valido");

        //    var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //    if (!int.TryParse(userIdStr, out int userId))
        //        return Unauthorized();

        //    // 1. Verifica appartenenza del cane (sicurezza)
        //    var checkSql = "SELECT COUNT(1) FROM DogOwners WHERE DogId = @DogId AND UserId = @UserId;";
        //    var count = await _db.ExecuteScalarAsync<int>(checkSql, new { DogId = dogId, UserId = userId });
        //    if (count == 0)
        //        return Unauthorized();

        //    // 2. Upload su Cloudflare R2
        //    string? newUrl = await R2Storage.UpDogMediaAsync(_s3Client, dogId, mediaFile);

        //    if (newUrl == null)
        //        return BadRequest("Errore durante il salvataggio cloud.");

        //    // 3. Salvataggio record in tabella Media (IdDoc è l'id del cane)
        //    bool isVideo = mediaFile.ContentType?.StartsWith("video/") == true;

        //    var insertMediaSql = @"
        //        INSERT INTO Media (WebLink, OriginalName, MimeTipe, ContentType, Dimensioni, IdDoc) 
        //        VALUES (@WebLink, @OriginalName, @MimeTipe, @ContentType, @Dimensioni, @IdDoc);";

        //    var mediaRecord = new MediaDto
        //    {
        //        WebLink = newUrl,
        //        OriginalName = mediaFile.FileName,
        //        MimeTipe = mediaFile.ContentType, // Nome colonna esatto dal DB
        //        ContentType = isVideo ? "video" : "image",
        //        Dimensioni = (int)mediaFile.Length,
        //        IdDoc = dogId
        //    };

        //    await _db.ExecuteAsync(insertMediaSql, mediaRecord);

        //    // Restituisce l'url e il tipo per aggiornare l'interfaccia via Javascript
        //    return new JsonResult(new { success = true, url = newUrl, isVideo = isVideo });
        //}
        public async Task<IActionResult> OnPostUploadMediaAsync(int dogId, IFormFile mediaFile)
        {
            if (mediaFile == null || mediaFile.Length == 0)
                return BadRequest("File non valido");

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized();

            // 1. Verifica appartenenza del cane (sicurezza)
            var checkSql = "SELECT COUNT(1) FROM DogOwners WHERE DogId = @DogId AND UserId = @UserId;";
            var count = await _db.ExecuteScalarAsync<int>(checkSql, new { DogId = dogId, UserId = userId });
            if (count == 0)
                return Unauthorized();

            // 2. Upload su Cloudflare R2
            string? newUrl = await R2Storage.UpDogMediaAsync(_s3Client, dogId, mediaFile);

            if (newUrl == null)
                return BadRequest("Errore durante il salvataggio cloud.");

            // 3. Salvataggio record in tabella Media
            bool isVideo = mediaFile.ContentType?.StartsWith("video/") == true;

            var insertMediaSql = @"
                INSERT INTO Media (WebLink, OriginalName, MimeTipe, ContentType, Dimensioni, IdDoc, idUser) 
                VALUES (@WebLink, @OriginalName, @MimeTipe, @ContentType, @Dimensioni, @IdDoc, @IdUser);
                SELECT last_insert_rowid();"; // Recuperiamo l'ID appena inserito

            var mediaRecord = new MediaDto
            {
                WebLink = newUrl,
                OriginalName = mediaFile.FileName,
                MimeTipe = mediaFile.ContentType,
                ContentType = isVideo ? "video" : "image",
                Dimensioni = (int)mediaFile.Length,
                IdDoc = dogId,
                IdUser = userId
            };

            // Eseguiamo e recuperiamo l'ID generato per il nuovo record media
            int newMediaId = await _db.ExecuteScalarAsync<int>(insertMediaSql, mediaRecord);

            // 4. Generiamo l'HTML esatto della scheda media da iniettare via JS
            var realUrl = "https://media.muzzlebook.it" + newUrl;

            string htmlContent = $@"
            <div class=""col-6 col-md-3"" id=""mediaBox_{newMediaId}"">
                <div class=""position-relative rounded-3 overflow-hidden shadow-sm ratio ratio-1x1 border bg-dark"">
                    <button class=""btn btn-sm btn-danger position-absolute top-0 end-0 m-1 rounded-circle z-3 shadow"" 
                            onclick=""deleteDogMedia({newMediaId})"" title=""Rimuovi""
                            style=""width: 35px; height: 35px;"">
                        <i class=""fa-solid fa-trash-can""></i>
                    </button>";

            if (isVideo)
            {
                htmlContent += $@"<video src=""{realUrl}"" class=""object-fit-contain w-100 h-100"" controls></video>";
            }
            else
            {
                htmlContent += $@"<a href=""{realUrl}"" target=""_blank"">
                                   <img src=""{realUrl}"" class=""object-fit-cover w-100 h-100"" alt=""{mediaRecord.OriginalName}"" />
                               </a>";
            }

            htmlContent += "</div></div>";

            // Restituisce l'HTML come testo piano
            return Content(htmlContent, "text/html");
        }
        public async Task<IActionResult> OnPostDeleteMediaAsync(int mediaId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized();

            // 1. Recupero del record per assicurarmi che il media esista e appartenga all'utente loggato
            var sqlSelect = "SELECT * FROM Media WHERE idMedia = @MediaId;";
            var mediaRecord = await _db.QueryFirstOrDefaultAsync<MediaDto>(sqlSelect, new { MediaId = mediaId, });

            if (mediaRecord == null)
                return NotFound();

            // 2. Eliminazione del file da Cloudflare R2
            // Sfrutto il metodo statico creato in R2StorageHelper (che pulisce il path e rimuove da R2)
            bool r2DeleteSuccess = await R2Storage.DelDogMediaAsync(_s3Client, mediaRecord.WebLink);

            if (!r2DeleteSuccess)
            {
                // Decidi se bloccare o proseguire; solitamente è meglio bloccare per evitare disallineamenti
                return BadRequest("Errore durante l'eliminazione del file fisico dal cloud.");
            }

            // 3. Eliminazione del record dal DB
            var sqlDelete = "DELETE FROM Media WHERE idMedia = @MediaId;";
            await _db.ExecuteAsync(sqlDelete, new { MediaId = mediaId });

            return new JsonResult(new { success = true });
        }
    }
}
