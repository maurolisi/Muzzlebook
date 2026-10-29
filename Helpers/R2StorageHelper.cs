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
    private const string BucketName = "muzzlebook"; // Inserisci il nome del tuo bucket R2[cite: 2, 3]
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
                return "/"+objectKey;
            }
        }
        catch (Exception ex)
        {
            // Puoi gestire o tracciare l'eccezione tramite un logger
            Console.WriteLine($"Errore caricamento R2: {ex.Message}");
        }

        return null;
    }
    /// <summary>
    /// Elimina l'intera cartella (tutti i file con un determinato prefisso) associata al cane dal bucket R2.
    /// </summary>
    public static async Task<bool> DeleteDogFolderAsync(IAmazonS3 s3Client, int dogId)
    {
        // Il prefisso che identifica la "cartella" del cane. 
        // Importante: mantenere lo slash finale (/) per non eliminare per sbaglio cartelle come "profile/1234/" se si passa "123".
        string folderPrefix = $"profile/{dogId}/";

        try
        {
            var listRequest = new ListObjectsV2Request
            {
                BucketName = BucketName,
                Prefix = folderPrefix
            };

            ListObjectsV2Response listResponse;

            // Usa un ciclo do-while per gestire la paginazione, nel caso ci siano più di 1000 file
            do
            {
                listResponse = await s3Client.ListObjectsV2Async(listRequest);

                if (listResponse.S3Objects.Count > 0)
                {
                    var deleteRequest = new DeleteObjectsRequest
                    {
                        BucketName = BucketName
                    };

                    // Aggiungi tutte le chiavi trovate alla richiesta di cancellazione di massa
                    foreach (var obj in listResponse.S3Objects)
                    {
                        deleteRequest.AddKey(obj.Key);
                    }

                    // Esegue l'eliminazione
                    await s3Client.DeleteObjectsAsync(deleteRequest);
                }

                // Prepara il token per la prossima pagina di risultati (se la lista era troncata)
                listRequest.ContinuationToken = listResponse.NextContinuationToken;

            } while (listResponse.IsTruncated==true);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore durante l'eliminazione della cartella R2: {ex.Message}");
            return false;
        }
    }
}
