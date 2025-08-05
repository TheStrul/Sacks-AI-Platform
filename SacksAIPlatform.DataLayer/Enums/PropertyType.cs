namespace SacksAIPlatform.DataLayer.Enums;

/// <summary>
/// Enum representing all mappable properties from CSV to Perfume entity
/// Used for flexible CSV column mapping configuration
/// </summary>
public enum PropertyType
{
    Code,
    Name,
    BrandID,
    Concentration,
    Type,
    Gender,
    Size,
    Units,
    LilFree,
    Remarks,
    CountryOfOrigin,
    OriginalSource,
    
    // Additional fields that might appear in CSV but not directly map to Perfume
    Brand,           // Brand name (will be resolved to BrandID)
    UPC,            // Universal Product Code (alternative to Code)
    ProductName,    // Alternative to Name
    SizeAndUnits,   // Combined size and units field
    LiFree,         // Alternative spelling
    Country,        // Alternative to CountryOfOrigin
    Source,         // Alternative to OriginalSource
    
    // Meta fields for CSV processing
    Confirmed,      // Confirmation status
    TotalProducts,  // Product count information
    Notes,          // Additional notes
    
    // Ignore this column
    Ignore
}
