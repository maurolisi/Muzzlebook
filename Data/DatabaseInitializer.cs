using System.Data;
using Dapper;

namespace Muzzlebook.Data;

public static class DatabaseInitializer
{
    public static void Initialize(IDbConnection db)
    {
        db.Open();

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

        db.Execute(sql);

        // Seeding automatico delle razze se la tabella è vuota
        var count = db.ExecuteScalar<int>("SELECT COUNT(*) FROM Breeds;");
        if (count == 0)
        {
            var seedSql = @"
                INSERT INTO Breeds (FciGroup, Name, Size, OriginCountry, Temperament) VALUES
                ('Gruppo 1 - Pastori e Bovari', 'Pastore Tedesco', 'Grande', 'Germania', 'Intelligente, leale, versatile'),
                ('Gruppo 1 - Pastori e Bovari', 'Pastore Belga (Malinois)', 'Grande', 'Belgio', 'Reattivo, atletico, da lavoro'),
                ('Gruppo 1 - Pastori e Bovari', 'Border Collie', 'Media', 'Regno Unito', 'Iperattivo, estremamente intelligente'),
                ('Gruppo 1 - Pastori e Bovari', 'Pastore Maremmano Abruzzese', 'Grande', 'Italia', 'Protettivo, indipendente, coraggioso'),
                ('Gruppo 1 - Pastori e Bovari', 'Welsh Corgi Pembroke', 'Piccola', 'Regno Unito', 'Vivace, affettuoso, attento'),
                ('Gruppo 1 - Pastori e Bovari', 'Pastore Scozzese (Collie)', 'Grande', 'Regno Unito', 'Dolce, protettivo, sensibile'),
                ('Gruppo 1 - Pastori e Bovari', 'Australian Shepherd', 'Media', 'USA', 'Vivace, versatile, socievole'),

                ('Gruppo 2 - Pinscher, Schnauzer e Molossi', 'Bouledogue Français', 'Piccola', 'Francia', 'Affettuoso, giocherellone, da compagnia'),
                ('Gruppo 2 - Pinscher, Schnauzer e Molossi', 'Cane Corso', 'Grande', 'Italia', 'Protettivo, equilibrato, leale'),
                ('Gruppo 2 - Pinscher, Schnauzer e Molossi', 'Boxer', 'Grande', 'Germania', 'Esuberante, affettuoso, paziente'),
                ('Gruppo 2 - Pinscher, Schnauzer e Molossi', 'Rottweiler', 'Grande', 'Germania', 'Calmo, protettivo, sicuro di sé'),
                ('Gruppo 2 - Pinscher, Schnauzer e Molossi', 'Mastino Napoletano', 'Gigante', 'Italia', 'Imponente, pacifico, da guardia'),
                ('Gruppo 2 - Pinscher, Schnauzer e Molossi', 'Alano', 'Gigante', 'Germania', 'Amichevole, docile, maestoso'),
                ('Gruppo 2 - Pinscher, Schnauzer e Molossi', 'Dobermann', 'Grande', 'Germania', 'Vigile, intelligente, fedele'),
                ('Gruppo 2 - Pinscher, Schnauzer e Molossi', 'Schnauzer (Nano / Medio / Gigante)', 'Varia', 'Germania', 'Attento, vivace, leale'),
                ('Gruppo 2 - Pinscher, Schnauzer e Molossi', 'San Bernardo', 'Gigante', 'Svizzera', 'Tranquillo, benevolo, da soccorso'),
                ('Gruppo 2 - Pinscher, Schnauzer e Molossi', 'Bulldog Inglese', 'Media', 'Regno Unito', 'Calmo, ostinato, affettuoso'),

                ('Gruppo 3 - Terrier', 'Jack Russell Terrier', 'Piccola', 'Regno Unito', 'Energico, audace, curioso'),
                ('Gruppo 3 - Terrier', 'Bull Terrier', 'Media', 'Regno Unito', 'Giocoso, coraggioso, determinato'),
                ('Gruppo 3 - Terrier', 'Yorkshire Terrier', 'Piccola', 'Regno Unito', 'Vivace, affettuoso, vigile'),
                ('Gruppo 3 - Terrier', 'American Staffordshire Terrier (Amstaff)', 'Media', 'USA', 'Potente, leale, affettuoso'),
                ('Gruppo 3 - Terrier', 'West Highland White Terrier', 'Piccola', 'Regno Unito', 'Indipendente, allegro, intrepido'),

                ('Gruppo 4 - Bassotti', 'Bassotto Tedesco (Pelo corto / duro / lungo)', 'Piccola', 'Germania', 'Curioso, tenace, fedele'),

                ('Gruppo 5 - Spitz e Tipo Primitivo', 'Husky Siberiano', 'Media', 'Russia', 'Socievole, energico, indipendente'),
                ('Gruppo 5 - Spitz e Tipo Primitivo', 'Akita Inu', 'Grande', 'Giappone', 'Dignitoso, riservato, leale'),
                ('Gruppo 5 - Spitz e Tipo Primitivo', 'Shiba Inu', 'Piccola', 'Giappone', 'Attento, indipendente, pulito'),
                ('Gruppo 5 - Spitz e Tipo Primitivo', 'Volpino Italiano', 'Piccola', 'Italia', 'Allegro, vigile, affettuoso'),
                ('Gruppo 5 - Spitz e Tipo Primitivo', 'Pomerania (Spitz Nano)', 'Piccola', 'Germania / Polonia', 'Estroverso, intelligente, vivace'),
                ('Gruppo 5 - Spitz e Tipo Primitivo', 'Samoyedo', 'Grande', 'Russia', 'Amichevole, gioioso, socievole'),

                ('Gruppo 6 - Segugi e Pista di Sangue', 'Beagle', 'Media', 'Regno Unito', 'Curioso, affabile, ottimo fiuto'),
                ('Gruppo 6 - Segugi e Pista di Sangue', 'Basset Hound', 'Media', 'Francia / Regno Unito', 'Pacato, affettuoso, tenace'),
                ('Gruppo 6 - Segugi e Pista di Sangue', 'Segugio Italiano', 'Media', 'Italia', 'Resistente, passionale nella caccia'),
                ('Gruppo 6 - Segugi e Pista di Sangue', 'Dalmata', 'Grande', 'Croazia', 'Attivo, intelligente, amichevole'),

                ('Gruppo 7 - Cani da Ferma', 'Setter Inglese', 'Grande', 'Regno Unito', 'Elegante, affettuoso, dinamico'),
                ('Gruppo 7 - Cani da Ferma', 'Bracco Italiano', 'Grande', 'Italia', 'Docile, intelligente, cacciatore'),
                ('Gruppo 7 - Cani da Ferma', 'Kurzhaar (Bracco Tedesco)', 'Grande', 'Germania', 'Energico, equilibrato, versatile'),
                ('Gruppo 7 - Cani da Ferma', 'Epagneul Breton', 'Media', 'Francia', 'Vivace, intelligente, adattabile'),

                ('Gruppo 8 - Riporto, Cerca e Acqua', 'Labrador Retriever', 'Grande', 'Canada', 'Socievole, docile, ottimo nuotatore'),
                ('Gruppo 8 - Riporto, Cerca e Acqua', 'Golden Retriever', 'Grande', 'Regno Unito', 'Affettuoso, intelligente, equilibrato'),
                ('Gruppo 8 - Riporto, Cerca e Acqua', 'Cocker Spaniel Inglese', 'Media', 'Regno Unito', 'Giocoso, affettuoso, esuberante'),
                ('Gruppo 8 - Riporto, Cerca e Acqua', 'Lagotto Romagnolo', 'Media', 'Italia', 'Lavoratore, docile, cerca tartufi'),

                ('Gruppo 9 - Cani da Compagnia', 'Barboncino (Toy / Nano / Medio / Grande)', 'Varia', 'Francia', 'Estremamente intelligente, affettuoso'),
                ('Gruppo 9 - Cani da Compagnia', 'Chihuahua', 'Piccola', 'Messico', 'Vivace, sveglio, molto legato al padrone'),
                ('Gruppo 9 - Cani da Compagnia', 'Cavalier King Charles Spaniel', 'Piccola', 'Regno Unito', 'Dolce, gentile, socievole'),
                ('Gruppo 9 - Cani da Compagnia', 'Maltese', 'Piccola', 'Italia / Mediterraneo', 'Vivace, affettuoso, elegante'),
                ('Gruppo 9 - Cani da Compagnia', 'Carlino (Pug)', 'Piccola', 'Cina', 'Affettuoso, tranquillo, simpatico'),
                ('Gruppo 9 - Cani da Compagnia', 'Shih Tzu', 'Piccola', 'Tibet / Cina', 'Intelligente, indipendente, amichevole'),

                ('Gruppo 10 - Levrieri', 'Piccolo Levriero Italiano', 'Piccola', 'Italia', 'Elegante, affettuoso, sensibile'),
                ('Gruppo 10 - Levrieri', 'Greyhound', 'Grande', 'Regno Unito', 'Velocissimo, docile, calmo'),
                ('Gruppo 10 - Levrieri', 'Whippet', 'Media', 'Regno Unito', 'Gentile, atletico, silenzioso'),
                ('Gruppo 10 - Levrieri', 'Levriero Afgano', 'Grande', 'Afghanistan', 'Nobile, riservato, indipendente');
            ";

            db.Execute(seedSql);
        }
    }
}