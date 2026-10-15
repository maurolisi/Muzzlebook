namespace Muzzlebook.Models;

public class Breed
{
    public int Id { get; set; }
    public string FciGroup { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string? OriginCountry { get; set; }
    public string? Temperament { get; set; }
}