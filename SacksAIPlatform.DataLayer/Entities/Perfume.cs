namespace SacksAIPlatform.DataLayer.Entities;

public class Perfume
{
    public string PerfumeCode { get; set; } = string.Empty; // World Wide Unique Primary Key
    public string Name { get; set; } = string.Empty;
    public int BrandID { get; set; }
    public string Concentration { get; set; } = string.Empty; // EDT, EDP, Parfum, etc.
    public string Type { get; set; } = string.Empty; // Spray, Colon
    public string Gender { get; set; } = string.Empty; // Unisex, Male, Female
    public string Size { get; set; } = string.Empty;
    public string Units { get; set; } = string.Empty;
    public string LilFree { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public string CountryOfOrigin { get; set; } = string.Empty;
    public string OriginalSource { get; set; } = string.Empty;

    // Navigation properties
    public virtual Brand Brand { get; set; } = null!;
}
