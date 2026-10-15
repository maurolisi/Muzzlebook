using System.Data;
using Dapper;

namespace Muzzlebook.Data;

public static class DatabaseInitializer
{
    public static void Initialize(IDbConnection db)
    {
        db.Open();

        var sql = @"
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

            CREATE INDEX IF NOT EXISTS IX_Dogs_OwnerId ON Dogs(OwnerId);
            CREATE INDEX IF NOT EXISTS IX_Vaccinations_NextDueDate ON Vaccinations(NextDueDate);
            CREATE INDEX IF NOT EXISTS IX_ParasiteTreatments_NextDueDate ON ParasiteTreatments(NextDueDate);
        ";

        db.Execute(sql);
    }
}