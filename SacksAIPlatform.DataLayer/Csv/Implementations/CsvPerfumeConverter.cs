using SacksAIPlatform.DataLayer.Csv.Interfaces;
using SacksAIPlatform.DataLayer.Entities;
using SacksAIPlatform.DataLayer.Enums;
using SacksAIPlatform.InfrastructuresLayer.Csv.Interfaces;
using System.Globalization;

namespace SacksAIPlatform.DataLayer.Csv.Implementations;

public class CsvPerfumeConverter : ICsvPerfumeConverter
{
    private readonly ICsvFileReader _csvFileReader;

    public CsvPerfumeConverter(ICsvFileReader csvFileReader)
    {
        _csvFileReader = csvFileReader;
    }

    public async Task<CsvConversionResult> ConvertCsvToPerfumesAsync(string csvFilePath, bool skipFirstRow = true)
    {
        var result = new CsvConversionResult();
        var perfumes = new List<Perfume>();
        var errors = new List<CsvValidationError>();
        
        try
        {
            var lines = await _csvFileReader.ReadCsvFileAsync(csvFilePath);
            var startIndex = skipFirstRow ? 1 : 0;
            
            for (int i = startIndex; i < lines.Length; i++)
            {
                var line = lines[i];
                var rowNumber = i + 1;
                result.TotalRecordsProcessed++;
                
                try
                {
                    var perfume = ParseCsvLineToPerfume(line, rowNumber);
                    if (perfume != null)
                    {
                        perfumes.Add(perfume);
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(new CsvValidationError
                    {
                        RowNumber = rowNumber,
                        Field = "General",
                        Value = line,
                        ErrorMessage = ex.Message,
                        RawCsvLine = line
                    });
                }
            }
            
            result.ValidPerfumes = perfumes;
            result.ValidationErrors = errors;
            
            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to process CSV file: {ex.Message}", ex);
        }
    }
    
    private Perfume? ParseCsvLineToPerfume(string csvLine, int rowNumber)
    {
        // Split CSV line using the general CSV reader
        var fields = _csvFileReader.ParseCsvLine(csvLine);
        
        if (fields.Length < 11) // Minimum required fields
        {
            throw new ArgumentException($"Insufficient fields in CSV line. Expected at least 11, got {fields.Length}");
        }
        
        // CSV Structure: Confirmed,UPC,Brand,Product Name,Size,Type,Concentration,Gender,Country of Origin,Li (Free/None),,Total Products...
        // Fields: 0=Confirmed, 1=UPC, 2=Brand, 3=Product Name, 4=Size, 5=Type, 6=Concentration, 7=Gender, 8=Country, 9=LiFree
        
        var upc = _csvFileReader.CleanField(fields[1]);
        var brand = _csvFileReader.CleanField(fields[2]);
        var productName = _csvFileReader.CleanField(fields[3]);
        var size = _csvFileReader.CleanField(fields[4]);
        var type = _csvFileReader.CleanField(fields[5]);
        var concentration = _csvFileReader.CleanField(fields[6]);
        var gender = _csvFileReader.CleanField(fields[7]);
        var countryOfOrigin = _csvFileReader.CleanField(fields[8]);
        var liFree = _csvFileReader.CleanField(fields[9]);
        
        // Skip invalid records
        if (string.IsNullOrWhiteSpace(upc) || string.IsNullOrWhiteSpace(productName))
        {
            return null;
        }
        
        var perfume = new Perfume
        {
            PerfumeCode = upc, // Using UPC as unique code
            Name = CleanProductName(productName),
            Size = ParseSize(size),
            Units = ParseUnits(size),
            Concentration = ParseConcentration(concentration),
            Type = ParseType(type),
            Gender = ParseGender(gender),
            CountryOfOrigin = CleanCountryName(countryOfOrigin),
            LilFree = ParseLiFree(liFree),
            OriginalSource = "ComprehensiveStockAi.csv",
            Remarks = $"Brand: {brand}, Original Size: {size}, Original Type: {type}",
            BrandID = 0 // Will need to be resolved against existing brands
        };
        
        return perfume;
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
}
