using SacksAIPlatform.DataLayer.Enums;
using System.Text.Json.Serialization;

namespace SacksAIPlatform.DataLayer.Entities;

public class Product
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty; // World Wide Unique Primary Key
    
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

    /// <summary>
    /// Validates the product entity to ensure it has all required fields and valid data
    /// </summary>
    /// <returns>True if the product is valid, false otherwise</returns>
    public bool Validate()
    {
        // Check required fields
        
        // Code is mandatory and should not be empty or whitespace
        if (string.IsNullOrWhiteSpace(Code))
            return false;
        
        // Name is mandatory and should not be empty or whitespace
        if (string.IsNullOrWhiteSpace(Name))
            return false;
        
        // BrandID should be positive (assuming 0 means no brand assigned, which might be invalid)
        // You can adjust this logic based on your business rules
        if (BrandID <= 0)
            return false;
        
        // Validate enum values are within expected ranges
        if (!Enum.IsDefined(typeof(Concentration), Concentration))
            return false;
            
        if (!Enum.IsDefined(typeof(PerfumeType), Type))
            return false;
            
        if (!Enum.IsDefined(typeof(Gender), Gender))
            return false;
            
        if (!Enum.IsDefined(typeof(Units), Units))
            return false;
        
        // Validate size if provided
        if (!string.IsNullOrEmpty(Size))
        {
            // Size should be a valid number if provided
            if (!decimal.TryParse(Size, out var sizeValue) || sizeValue <= 0)
                return false;
        }
        
        // Code should have a reasonable length (adjust limits as needed)
        if (Code.Length > 100)
            return false;
            
        // Name should have a reasonable length (adjust limits as needed)
        if (Name.Length > 200)
            return false;
        
        // All validations passed
        return true;
    }

    /// <summary>
    /// Gets detailed validation errors for the product
    /// </summary>
    /// <returns>List of validation error messages</returns>
    public List<string> GetValidationErrors()
    {
        var errors = new List<string>();
        
        // Check required fields
        if (string.IsNullOrWhiteSpace(Code))
            errors.Add("Product code is required and cannot be empty");
        
        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Product name is required and cannot be empty");
        
        if (BrandID <= 0)
            errors.Add("Valid brand ID is required (must be greater than 0)");
        
        // Validate enum values
        if (!Enum.IsDefined(typeof(Concentration), Concentration))
            errors.Add($"Invalid concentration value: {Concentration}");
            
        if (!Enum.IsDefined(typeof(PerfumeType), Type))
            errors.Add($"Invalid perfume type value: {Type}");
            
        if (!Enum.IsDefined(typeof(Gender), Gender))
            errors.Add($"Invalid gender value: {Gender}");
            
        if (!Enum.IsDefined(typeof(Units), Units))
            errors.Add($"Invalid units value: {Units}");
        
        // Validate size format
        if (!string.IsNullOrEmpty(Size))
        {
            if (!decimal.TryParse(Size, out var sizeValue) || sizeValue <= 0)
                errors.Add($"Invalid size value: '{Size}'. Size must be a positive number");
        }
        
        // Validate field lengths
        if (Code.Length > 100)
            errors.Add($"Product code is too long (max 100 characters): {Code.Length} characters");
            
        if (Name.Length > 200)
            errors.Add($"Product name is too long (max 200 characters): {Name.Length} characters");
        
        return errors;
    }
}
