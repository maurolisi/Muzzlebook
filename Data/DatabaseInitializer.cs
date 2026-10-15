using System.Data;
using System.Text.Json;
using Dapper;
using Muzzlebook.Models; // Assicurati di avere il modello Breed

namespace Muzzlebook.Data;

public static class DatabaseInitializer
{
    public static void Initialize(IDbConnection db)
    {
        // Se la connessione non è aperta, la apriamo (anche se solitamente 
        // viene passata già configurata, è bene esserne sicuri)
        if (db.State != ConnectionState.Open)
        {
            db.Open();
        }

        var sql = @"
            CREATE TABLE IF NOT EXISTS Breeds (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FciGroup TEXT NOT NULL,
                Name TEXT NOT NULL UNIQUE,
                Size TEXT NOT NULL,
                OriginCountry TEXT,
                Temperament TEXT
            );

            CREATE TABLE IF NOT EXISTS Dogs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                OwnerId TEXT NOT NULL,
                Name TEXT NOT NULL,
                Breed TEXT,
                DateOfBirth DATE,
                Gender TEXT CHECK(Gender IN ('M', 'F')),
                Microchip TEXT UNIQUE,
                CurrentWeightKg REAL,
                ProfileImageUrl TEXT,
                CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
            );

            CREATE TABLE IF NOT EXISTS MedicalVisits (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                DogId INTEGER NOT NULL,
                VisitDate DATETIME NOT NULL,
                VeterinarianName TEXT,
                ReasonForVisit TEXT NOT NULL,
                Diagnosis TEXT,
                Prescription TEXT,
                Cost REAL,
                FOREIGN KEY(DogId) REFERENCES Dogs(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS Vaccinations (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                DogId INTEGER NOT NULL,
                VaccineName TEXT NOT NULL,
                AdministeredDate DATE NOT NULL,
                NextDueDate DATE,
                LotNumber TEXT,
                VeterinarianName TEXT,
                FOREIGN KEY(DogId) REFERENCES Dogs(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS ParasiteTreatments (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                DogId INTEGER NOT NULL,
                TreatmentType TEXT NOT NULL,
                ProductName TEXT NOT NULL,
                AdministeredDate DATE NOT NULL,
                NextDueDate DATE,
                Notes TEXT,
                FOREIGN KEY(DogId) REFERENCES Dogs(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS ClinicalDocuments (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                DogId INTEGER NOT NULL,
                DocumentType TEXT NOT NULL,
                Title TEXT NOT NULL,
                StorageUrl TEXT NOT NULL,
                UploadDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                Notes TEXT,
                FOREIGN KEY(DogId) REFERENCES Dogs(Id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_Breeds_Name ON Breeds(Name);
            CREATE INDEX IF NOT EXISTS IX_Dogs_OwnerId ON Dogs(OwnerId);
            CREATE INDEX IF NOT EXISTS IX_Vaccinations_NextDueDate ON Vaccinations(NextDueDate);
            CREATE INDEX IF NOT EXISTS IX_ParasiteTreatments_NextDueDate ON ParasiteTreatments(NextDueDate);
        ";

        // Esegue la creazione delle tabelle e degli indici
        db.Execute(sql);

        // Seeding automatico delle razze leggendo dal file JSON
        var count = db.ExecuteScalar<int>("SELECT COUNT(*) FROM Breeds;");

        if (count == 0)
        {
            // Cerca il file JSON nella cartella Data
            var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "breeds.json");

            if (File.Exists(jsonPath))
            {
                var jsonString = File.ReadAllText(jsonPath);

                // Deserializza il contenuto
                var breeds = JsonSerializer.Deserialize<List<Breed>>(jsonString);

                if (breeds != null && breeds.Any())
                {
                    // Query parametrizzata pronta per Dapper
                    var insertSql = @"
                        INSERT INTO Breeds (FciGroup, Name, Size, OriginCountry, Temperament) 
                        VALUES (@FciGroup, @Name, @Size, @OriginCountry, @Temperament);";

                    // Dapper mappa automaticamente la List<Breed> e cicla gli insert
                    db.Execute(insertSql, breeds);
                }
            }
            else
            {
                // Utile per il debug in caso il file non si trovi dove ci si aspetta
                Console.WriteLine($"File non trovato: {jsonPath}");
            }
        }
    }
}