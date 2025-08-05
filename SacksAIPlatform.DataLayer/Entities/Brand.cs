using System.Text.Json.Serialization;
using SacksAIPlatform.DataLayer.Enums;

namespace SacksAIPlatform.DataLayer.Entities;

public class Brand
{
    [JsonPropertyName("brandId")]
    public int BrandID { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("manufacturerId")]
    public int ManufacturerID { get; set; }
    
    [JsonPropertyName("countryOfOrigin")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Country CountryOfOrigin { get; set; } = Country.USA;

    // Navigation properties - excluded from JSON serialization
    [JsonIgnore]
    public virtual Manufacturer Manufacturer { get; set; } = null!;
    
    [JsonIgnore]
    public virtual ICollection<Perfume> Perfumes { get; set; } = new List<Perfume>();
}