using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Muzzlebook.Helpers; 

public static class R2Storage
{
    private const string BucketName = "muzzlebook"; // Inserisci il nome del tuo bucket R2
    /// <summary>
    /// Carica una immagine o un breve video su Cloudflare R2 nel percorso specificato e restituisce l'URL completo.
    /// </summary>
    public static async Task<string?> UpDogMediaAsync(IAmazonS3 s3Client, int dogId, IFormFile mediaFile)
    {
        if (mediaFile == null || mediaFile.Length == 0)
            return null;

        // 1. Estrae l'estensione originale dal file caricato (es. .jpg, .mp4, .webm)
        string extension = Path.GetExtension(mediaFile.FileName).ToLowerInvariant();

        // Fallback di sicurezza se il browser non invia l'estensione nel nome
        if (string.IsNullOrEmpty(extension))
        {
            extension = mediaFile.ContentType?.StartsWith("video/") == true ? ".mp4" : ".webp";
        }

        // 2. Genera un nome univoco mantenendo l'estensione corretta
        string fileName = $"{Guid.NewGuid()}{extension}";

        // Struttura del percorso: DogMedia/{dogId}/{fileName}
        string objectKey = $"DogMedia/{dogId}/{fileName}";

        try
        {
            using var stream = mediaFile.OpenReadStream();

            var putRequest = new PutObjectRequest
            {
                BucketName = BucketName,
                Key = objectKey,
                InputStream = stream,
                // 3. Usa il ContentType inviato dal browser, fondamentale per far riprodurre i video correttamente
                ContentType = mediaFile.ContentType ?? (extension == ".mp4" ? "video/mp4" : "image/webp"),
                DisablePayloadSigning = true // Ottimizzazione consigliata per R2
            };

            var response = await s3Client.PutObjectAsync(putRequest);

            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                // Restituisce l'URL parziale o completo del file caricato
                return "/" + objectKey;
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
    /// Elimina un singolo file multimediale dal bucket R2 dato il suo WebLink.
    /// </summary>
    public static async Task<bool> DelDogMediaAsync(IAmazonS3 s3Client, string webLink)
    {
        if (string.IsNullOrWhiteSpace(webLink))
            return false;

        try
        {
            // Il weblink salvato nel DB inizia solitamente con "/" (es. "/media/15/foto.webp")
            // Cloudflare R2 / AWS S3 richiede la "Key" senza lo slash iniziale
            string objectKey = webLink.TrimStart('/');

            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = BucketName, // Usa la costante/variabile già definita nella tua classe
                Key = objectKey
            };

            // Esegue l'eliminazione del singolo file
            var response = await s3Client.DeleteObjectAsync(deleteRequest);

            return response.HttpStatusCode == System.Net.HttpStatusCode.NoContent ||
                   response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore durante l'eliminazione del file R2 ({webLink}): {ex.Message}");
            return false;
        }
    }
    /// <summary>
    /// Carica un file IFormFile su Cloudflare R2 nel percorso specificato e restituisce l'URL completo.
    /// </summary>
    public static async Task<string?> UpProfileAsync(IAmazonS3 s3Client, int dogId, IFormFile photo)
    {
        if (photo == null || photo.Length == 0)
            return null;

        // Genera un nome univoco per il file
        string fileName = $"{Guid.NewGuid()}.webp";

        // Struttura del percorso: profile/{dogId}/{fileName}
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
                DisablePayloadSigning = true // Ottimizzazione consigliata per R2
            };

            var response = await s3Client.PutObjectAsync(putRequest);

            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                // Restituisce l'URL completo del file caricato
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
    /// Elimina l'intera cartella @IdDog dentro Profile (tutti i file con un determinato prefisso) associata al cane dal bucket R2.
    /// </summary>
    public static async Task<bool> DelProfileAsync(IAmazonS3 s3Client, int dogId)
    {
        //return true;
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
