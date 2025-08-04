using SacksAIPlatform.DataLayer.Enums;

namespace SacksAIPlatform.DataLayer.Entities;

public class Perfume
{
    public string PerfumeCode { get; set; } = string.Empty; // World Wide Unique Primary Key
    public string Name { get; set; } = string.Empty;
    public int BrandID { get; set; }
    public Concentration Concentration { get; set; } = Concentration.EDT;
    public PerfumeType Type { get; set; } = PerfumeType.Spray;
    public Gender Gender { get; set; } = Gender.Unisex;
    public string Size { get; set; } = string.Empty;
    public Units Units { get; set; } = Units.ml;
    public bool LilFree { get; set; } = false;
    public string Remarks { get; set; } = string.Empty;
    public string CountryOfOrigin { get; set; } = string.Empty;
    public string OriginalSource { get; set; } = string.Empty;

    // Navigation properties
    public virtual Brand Brand { get; set; } = null!;
}
