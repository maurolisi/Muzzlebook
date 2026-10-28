using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Muzzlebook.Helpers;

public static class R2StorageHelper
{
    private const string BucketName = "il-tuo-bucket-r2"; // Inserisci il nome del tuo bucket R2[cite: 2, 3]
    private const string BaseMediaUrl = "https://media.muzzlebook.it"; // Il tuo dominio personalizzato R2

    /// <summary>
    /// Carica un file IFormFile su Cloudflare R2 nel percorso specificato e restituisce l'URL completo.
    /// </summary>
    public static async Task<string?> UploadProfilePhotoAsync(IAmazonS3 s3Client, int dogId, IFormFile photo)
    {
        if (photo == null || photo.Length == 0)
            return null;

        // Genera un nome univoco per il file[cite: 3]
        string fileName = $"{Guid.NewGuid()}.webp";

        // Struttura del percorso: profile/{dogId}/{fileName}[cite: 3]
        string objectKey = $"profile/{dogId}/{fileName}";

        try
        {
            using var stream = photo.OpenReadStream();

            var putRequest = new PutObjectRequest
            {
                BucketName = BucketName,
                Key = objectKey,
                InputStream = stream,
                ContentType = photo.ContentType ?? "image/webp",
                DisablePayloadSigning = true // Ottimizzazione consigliata per R2[cite: 3]
            };

            var response = await s3Client.PutObjectAsync(putRequest);

            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                // Restituisce l'URL completo del file caricato[cite: 3]
                return $"{BaseMediaUrl}/{objectKey}";
            }
        }
        catch (Exception ex)
        {
            // Puoi gestire o tracciare l'eccezione tramite un logger
            Console.WriteLine($"Errore caricamento R2: {ex.Message}");
        }

        return null;
    }
}