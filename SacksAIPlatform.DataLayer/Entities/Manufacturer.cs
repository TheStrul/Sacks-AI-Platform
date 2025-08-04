namespace SacksAIPlatform.DataLayer.Entities;

public class Manufacturer
{
    public int ManufacturerID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;

    // Navigation properties
    public virtual ICollection<Brand> Brands { get; set; } = new List<Brand>();
}
