namespace Muzzlebook.Models.DTOs
{
    public record BreedDto
    {
        public int Id { get; init; }
        public string FciGroup { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Size { get; init; } = string.Empty;
        public string? OriginCountry { get; init; }
        public string? Temperament { get; init; }
    }

    public record DogDto
    {
        public int Id { get; init; }
        public int OwnerId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Breed { get; init; }
        public DateTime? DateOfBirth { get; init; }
        public string? Gender { get; init; }
        public string? Microchip { get; init; }
        public double? CurrentWeightKg { get; init; }
        public string? ProfileImageUrl { get; init; }
        public DateTime? CreatedAt { get; init; }
    }

    public record ClinicalDocumentDto
    {
        public int Id { get; init; }
        public int DogId { get; init; }
        public string DocumentType { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string? Notes { get; init; }
    }

    public record DogOwnerDto
    {
        public int DogId { get; init; }
        public string UserId { get; init; } = string.Empty;
        public string? Role { get; init; }
    }

    public record MediaDto
    {
        public int IdMedia { get; init; }
        public string? WebLink { get; init; }
        public string? OriginalName { get; init; }
        public string? MimeTipe { get; init; }
        public string? ContentType { get; init; }
        public int Dimensioni { get; init; }
        public string? DescFile { get; init; }
        public int IdDoc { get; init; }
        public int IdProfile { get; init; }
    }

    public record MedicalVisitDto
    {
        public int Id { get; init; }
        public int DogId { get; init; }
        public DateTime? VisitDate { get; init; }
        public string? VeterinarianName { get; init; }
        public string? ReasonForVisit { get; init; }
        public string? Diagnosis { get; init; }
        public string? Prescription { get; init; }
        public double? Cost { get; init; }
    }

    public record ParasiteTreatmentDto
    {
        public int Id { get; init; }
        public int DogId { get; init; }
        public string? TreatmentType { get; init; }
        public string? ProductName { get; init; }
        public DateTime? AdministeredDate { get; init; }
        public DateTime? NextDueDate { get; init; }
        public string? Notes { get; init; }
    }

    public record UserDto
    {
        public int Id { get; init; }
        public string Email { get; init; } = string.Empty;
        public string PasswordHash { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public DateTime? CreatedAt { get; init; }
    }

    public record VaccinationDto
    {
        public int Id { get; init; }
        public int DogId { get; init; }
        public string? VaccineName { get; init; }
        public DateTime? AdministeredDate { get; init; }
        public DateTime? NextDueDate { get; init; }
        public string? LotNumber { get; init; }
        public string? VeterinarianName { get; init; }
    }
}