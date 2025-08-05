using System.Text.Json.Serialization;

namespace SacksAIPlatform.DataLayer.Entities;

public class Manufacturer
{
    [JsonPropertyName("manufacturerId")]
    public int ManufacturerID { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("website")]
    public string Website { get; set; } = string.Empty;

    // Navigation properties - excluded from JSON serialization
    [JsonIgnore]
    public virtual ICollection<Brand> Brands { get; set; } = new List<Brand>();
}
