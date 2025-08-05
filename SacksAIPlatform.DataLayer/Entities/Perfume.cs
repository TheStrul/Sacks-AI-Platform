using SacksAIPlatform.DataLayer.Enums;
using System.Text.Json.Serialization;

namespace SacksAIPlatform.DataLayer.Entities;

public class Perfume
{
    [JsonPropertyName("perfumeCode")]
    public string PerfumeCode { get; set; } = string.Empty; // World Wide Unique Primary Key
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("brandId")]
    public int BrandID { get; set; }
    
    [JsonPropertyName("concentration")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Concentration Concentration { get; set; } = Concentration.EDT;
    
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PerfumeType Type { get; set; } = PerfumeType.Spray;
    
    [JsonPropertyName("gender")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Gender Gender { get; set; } = Gender.Unisex;
    
    [JsonPropertyName("size")]
    public string Size { get; set; } = string.Empty;
    
    [JsonPropertyName("units")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Units Units { get; set; } = Units.ml;
    
    [JsonPropertyName("lilFree")]
    public bool LilFree { get; set; } = false;
    
    [JsonPropertyName("remarks")]
    public string Remarks { get; set; } = string.Empty;
    
    [JsonPropertyName("countryOfOrigin")]
    public string CountryOfOrigin { get; set; } = string.Empty;
    
    [JsonPropertyName("originalSource")]
    public string OriginalSource { get; set; } = string.Empty;

    // Navigation properties - excluded from JSON serialization
    [JsonIgnore]
    public virtual Brand Brand { get; set; } = null!;
}
