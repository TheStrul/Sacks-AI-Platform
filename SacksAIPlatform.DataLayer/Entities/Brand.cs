namespace SacksAIPlatform.DataLayer.Entities;

public class Brand
{
    public int BrandID { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ManufacturerID { get; set; }

    // Navigation properties
    public virtual Manufacturer Manufacturer { get; set; } = null!;
    public virtual ICollection<Perfume> Perfumes { get; set; } = new List<Perfume>();
}
