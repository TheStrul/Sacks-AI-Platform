using SacksAIPlatform.DataLayer.Csv.Interfaces;
using SacksAIPlatform.DataLayer.Csv.Models;
using SacksAIPlatform.DataLayer.Entities;
using SacksAIPlatform.DataLayer.Enums;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing.Interfaces;

namespace SacksAIPlatform.DataLayer.Csv.Implementations;

public class FiletoProductConverter : IFiletoProductConverter
{
    private readonly IFileDataReader _fileDataReader;

    public FiletoProductConverter(IFileDataReader fileDataReader)
    {
        _fileDataReader = fileDataReader;
    }

    public async Task<FileConversionResult> ConvertFileToProductsAsync(string csvFilePath, FileConfiguration? configuration = null)
    {
        configuration ??= FileConfiguration.CreateDefaultConfiguration();
        configuration.Validate();
        
        var result = new FileConversionResult();
        var products = new List<Product>();
        var errors = new List<FileValidationError>();
        
        try
        {
            var fileData = await _fileDataReader.ReadFileAsync(csvFilePath);
            var endRow = configuration.EndAtRow == -1 ? fileData.RowCount - 1 : Math.Min(configuration.EndAtRow, fileData.RowCount - 1);
            
            for (int i = configuration.StartFromRow; i <= endRow; i++)
            {
                var rowNumber = i + 1;
                result.TotalRecordsProcessed++;
                
                // Skip inner titles if configured
                if (configuration.HasInnerTitles && IsLikelyTitleRow(fileData, i))
                {
                    continue;
                }
                
                try
                {
                    var product = ParseRowToPerfume(fileData, i, rowNumber, configuration);
                    if (product != null)
                    {
                        products.Add(product);
                    }
                }
                catch (Exception ex)
                {
                    var rowData = string.Join(",", fileData.GetRow(i));
                    errors.Add(new FileValidationError
                    {
                        RowNumber = rowNumber,
                        Field = "General",
                        Value = rowData,
                        ErrorMessage = ex.Message,
                        RawLine = rowData
                    });
                }
            }
            
            result.ValidProducts = products;
            result.ValidationErrors = errors;
            
            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to process CSV file: {ex.Message}", ex);
        }
    }

    public async Task<FileConversionResult> ConvertCsvToPerfumesAsync(string csvFilePath, bool skipFirstRow = true)
    {
        var configuration = FileConfiguration.CreateDefaultConfiguration();
        configuration.StartFromRow = skipFirstRow ? 1 : 0;
        return await ConvertFileToProductsAsync(csvFilePath, configuration);
    }
    
    private bool IsLikelyTitleRow(SacksAIPlatform.InfrastructuresLayer.FileProcessing.Models.FileData fileData, int rowIndex)
    {
        // Simple heuristic to detect title rows
        var fields = fileData.GetRow(rowIndex);
        
        // If most fields contain common header words, it's likely a title row
        var headerKeywords = new[] { "confirmed", "upc", "brand", "product", "name", "size", "type", "concentration", "gender", "country", "origin" };
        var matchCount = fields.Count(field => 
            headerKeywords.Any(keyword => 
                field?.ToLowerInvariant().Contains(keyword) == true));
        
        return matchCount >= 3; // If 3 or more fields match header keywords, consider it a title row
    }
    
    private Product? ParseRowToPerfume(SacksAIPlatform.InfrastructuresLayer.FileProcessing.Models.FileData fileData, int rowIndex, int rowNumber, FileConfiguration configuration)
    {
        // Get row data from FileData
        var fields = fileData.GetRow(rowIndex);
        
        if (fields.Length < configuration.MinimumColumns)
        {
            throw new ArgumentException($"Insufficient fields in CSV line. Expected at least {configuration.MinimumColumns}, got {fields.Length}");
        }
        
        var product = new Product
        {
            OriginalSource = configuration.FormatName
        };
        
        // Process each field according to configuration
        for (int columnIndex = 0; columnIndex < fields.Length; columnIndex++)
        {
            if (configuration.IsColumnIgnored(columnIndex))
                continue;
                
            var propertyType = configuration.GetPropertyType(columnIndex);
            var fieldValue = CleanField(fields[columnIndex]);
            
            MapFieldToPerfume(product, propertyType, fieldValue, rowNumber);
        }
        
        // Validate required fields
        if (string.IsNullOrWhiteSpace(product.Code) || string.IsNullOrWhiteSpace(product.Name))
        {
            return null; // Skip invalid records
        }
        
        return product;
    }
    
    private void MapFieldToPerfume(Product product, PropertyType propertyType, string fieldValue, int rowNumber)
    {
        try
        {
            switch (propertyType)
            {
                case PropertyType.Code:
                case PropertyType.UPC:
                    if (!string.IsNullOrWhiteSpace(fieldValue))
                        product.Code = fieldValue;
                    break;
                    
                case PropertyType.Name:
                case PropertyType.ProductName:
                    if (!string.IsNullOrWhiteSpace(fieldValue))
                        product.Name = CleanProductName(fieldValue);
                    break;
                    
                case PropertyType.BrandID:
                    if (int.TryParse(fieldValue, out var brandId))
                        product.BrandID = brandId;
                    break;
                    
                case PropertyType.Brand:
                    // Store brand name in remarks, will need to resolve to BrandID later
                    if (!string.IsNullOrWhiteSpace(fieldValue))
                        product.Remarks = $"Brand: {fieldValue}, {product.Remarks}".TrimEnd(", ".ToCharArray());
                    break;
                    
                case PropertyType.Concentration:
                    product.Concentration = ParseConcentration(fieldValue);
                    break;
                    
                case PropertyType.Type:
                    product.Type = ParseType(fieldValue);
                    break;
                    
                case PropertyType.Gender:
                    product.Gender = ParseGender(fieldValue);
                    break;
                    
                case PropertyType.Size:
                    if (!string.IsNullOrWhiteSpace(fieldValue))
                        product.Size = ParseSize(fieldValue);
                    break;
                    
                case PropertyType.SizeAndUnits:
                    if (!string.IsNullOrWhiteSpace(fieldValue))
                    {
                        product.Size = ParseSize(fieldValue);
                        product.Units = ParseUnits(fieldValue);
                    }
                    break;
                    
                case PropertyType.Units:
                    product.Units = ParseUnitsFromText(fieldValue);
                    break;
                    
                case PropertyType.LilFree:
                case PropertyType.LiFree:
                    product.LilFree = ParseLiFree(fieldValue);
                    break;
                    
                case PropertyType.CountryOfOrigin:
                case PropertyType.Country:
                    if (!string.IsNullOrWhiteSpace(fieldValue))
                        product.CountryOfOrigin = CleanCountryName(fieldValue);
                    break;
                    
                case PropertyType.OriginalSource:
                case PropertyType.Source:
                    if (!string.IsNullOrWhiteSpace(fieldValue))
                        product.OriginalSource = fieldValue;
                    break;
                    
                case PropertyType.Remarks:
                case PropertyType.Notes:
                    if (!string.IsNullOrWhiteSpace(fieldValue))
                        product.Remarks = $"{product.Remarks}, {fieldValue}".TrimStart(", ".ToCharArray());
                    break;
                    
                case PropertyType.Confirmed:
                case PropertyType.TotalProducts:
                case PropertyType.Ignore:
                    // These fields are not mapped to Perfume properties
                    break;
                    
                default:
                    // Log unknown property type but don't fail
                    break;
            }
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Failed to map field '{fieldValue}' to property '{propertyType}' at row {rowNumber}: {ex.Message}");
        }
    }
    
    private Units ParseUnitsFromText(string unitsField)
    {
        if (string.IsNullOrWhiteSpace(unitsField))
            return Units.ml;
            
        var lower = unitsField.ToLowerInvariant();
        
        return lower switch
        {
            "oz" or "fl oz" or "fluid ounce" => Units.oz,
            "ml" or "milliliter" => Units.ml,
            "g" or "gram" or "grams" => Units.g,
            _ => Units.ml
        };
    }
    
    private string CleanProductName(string productName)
    {
        return productName
            .Replace("\"", "")
            .Replace("  ", " ")
            .Trim();
    }
    
    private string ParseSize(string sizeField)
    {
        if (string.IsNullOrWhiteSpace(sizeField) || sizeField.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            return "0";
            
        // Extract numeric part
        var cleanSize = System.Text.RegularExpressions.Regex.Match(sizeField, @"[\d.]+").Value;
        return string.IsNullOrEmpty(cleanSize) ? "0" : cleanSize;
    }
    
    private Units ParseUnits(string sizeField)
    {
        if (string.IsNullOrWhiteSpace(sizeField))
            return Units.ml;
            
        var lowerSize = sizeField.ToLowerInvariant();
        
        if (lowerSize.Contains("oz") || lowerSize.Contains("fl"))
            return Units.oz;
        else if (lowerSize.Contains("ml"))
            return Units.ml;
        else if (lowerSize.Contains("g") || lowerSize.Contains("gram"))
            return Units.g;
        else
            return Units.ml; // Default to ml
    }
    
    private Concentration ParseConcentration(string concentrationField)
    {
        if (string.IsNullOrWhiteSpace(concentrationField))
            return Concentration.Unknown;
            
        var lower = concentrationField.ToLowerInvariant().Trim();
        
        return lower switch
        {
            "eau de toilette" or "edt" => Concentration.EDT,
            "eau de parfum" or "edp" => Concentration.EDP,
            "parfum" or "parfum intense" or "elixir" => Concentration.Parfum,
            "eau de cologne" or "edc" or "cologne" => Concentration.EDC,
            "eau de fraiche" or "edf" => Concentration.EDF,
            _ => Concentration.Unknown
        };
    }
    
    private PerfumeType ParseType(string typeField)
    {
        if (string.IsNullOrWhiteSpace(typeField) || typeField.Equals("NA", StringComparison.OrdinalIgnoreCase))
            return PerfumeType.Spray;
            
        var lower = typeField.ToLowerInvariant().Trim();
        
        return lower switch
        {
            "sp" or "spray" => PerfumeType.Spray,
            "cologne" => PerfumeType.Cologne,
            "fl" or "splash" => PerfumeType.Splash,
            "oil" => PerfumeType.Oil,
            "solid" => PerfumeType.Solid,
            "rollette" => PerfumeType.Rollette,
            _ => PerfumeType.Spray // Default to spray
        };
    }
    
    private Gender ParseGender(string genderField)
    {
        if (string.IsNullOrWhiteSpace(genderField))
            return Gender.Unisex;
            
        var lower = genderField.ToLowerInvariant().Trim();
        
        return lower switch
        {
            "m" or "male" or "men" => Gender.Male,
            "w" or "f" or "female" or "women" => Gender.Female,
            "u" or "unisex" => Gender.Unisex,
            _ => Gender.Unisex // Default to unisex
        };
    }
    
    private bool ParseLiFree(string liFreeField)
    {
        if (string.IsNullOrWhiteSpace(liFreeField))
            return false;
            
        var lower = liFreeField.ToLowerInvariant().Trim();
        return lower.Contains("free") || lower.Equals("none", StringComparison.OrdinalIgnoreCase);
    }
    
    private string CleanCountryName(string countryField)
    {
        if (string.IsNullOrWhiteSpace(countryField))
            return "";
            
        return countryField.Trim();
    }

    /// <summary>
    /// Cleans a CSV field by removing quotes and trimming whitespace
    /// </summary>
    private string CleanField(string? field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return string.Empty;
        }

        // First trim whitespace, then remove surrounding quotes, then handle escaped quotes
        return field
            .Trim() // Remove leading/trailing whitespace
            .Trim('"') // Remove surrounding quotes
            .Replace("\"\"", "\"") // Handle escaped quotes (convert "" to ")
            .Trim(); // Final trim after processing
    }
}
