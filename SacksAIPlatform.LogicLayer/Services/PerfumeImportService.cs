using SacksAIPlatform.DataLayer.Context;
using SacksAIPlatform.DataLayer.Entities;
using SacksAIPlatform.DataLayer.Csv.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SacksAIPlatform.LogicLayer.Services;

public class PerfumeImportService
{
    private readonly ICsvPerfumeConverter _csvConverter;
    private readonly PerfumeDbContext _context;
    private readonly ILogger<PerfumeImportService> _logger;

    public PerfumeImportService(
        ICsvPerfumeConverter csvConverter,
        PerfumeDbContext context,
        ILogger<PerfumeImportService> logger)
    {
        _csvConverter = csvConverter;
        _context = context;
        _logger = logger;
    }

    public async Task<PerfumeImportResult> ImportPerfumesFromCsvAsync(string csvFilePath)
    {
        _logger.LogInformation("Starting perfume import from CSV: {CsvFilePath}", csvFilePath);
        
        // Convert CSV to perfumes
        var conversionResult = await _csvConverter.ConvertCsvToPerfumesAsync(csvFilePath);
        
        _logger.LogInformation("CSV conversion completed. Valid records: {ValidCount}, Errors: {ErrorCount}", 
            conversionResult.ValidRecordsCount, conversionResult.ErrorRecordsCount);
        
        // Get all existing brands for matching
        var existingBrands = await _context.Brands
            .Include(b => b.Manufacturer)
            .ToListAsync();
        
        var importResult = new PerfumeImportResult
        {
            TotalRecordsProcessed = conversionResult.TotalRecordsProcessed,
            CsvValidationErrors = conversionResult.ValidationErrors,
            UnmatchedBrands = new List<UnmatchedBrand>(),
            SuccessfullyImported = new List<Perfume>(),
            BrandMappingErrors = new List<BrandMappingError>()
        };
        
        // Match brands and prepare perfumes for import
        foreach (var perfume in conversionResult.ValidPerfumes)
        {
            var brandInfo = ExtractBrandFromRemarks(perfume.Remarks);
            var matchedBrand = FindMatchingBrand(brandInfo, existingBrands);
            
            if (matchedBrand != null)
            {
                perfume.BrandID = matchedBrand.BrandID;
                importResult.SuccessfullyImported.Add(perfume);
            }
            else
            {
                importResult.UnmatchedBrands.Add(new UnmatchedBrand
                {
                    BrandName = brandInfo,
                    PerfumeName = perfume.Name,
                    UPC = perfume.PerfumeCode
                });
                
                importResult.BrandMappingErrors.Add(new BrandMappingError
                {
                    PerfumeName = perfume.Name,
                    ExtractedBrandName = brandInfo,
                    UPC = perfume.PerfumeCode,
                    ErrorMessage = $"No matching brand found for '{brandInfo}'"
                });
            }
        }
        
        _logger.LogInformation("Brand matching completed. Matched: {MatchedCount}, Unmatched: {UnmatchedCount}", 
            importResult.SuccessfullyImported.Count, importResult.UnmatchedBrands.Count);
        
        return importResult;
    }
    
    public async Task<int> SaveMatchedPerfumesToDatabaseAsync(List<Perfume> perfumes)
    {
        if (!perfumes.Any())
            return 0;
            
        _logger.LogInformation("Saving {Count} perfumes to database", perfumes.Count);
        
        await _context.Perfumes.AddRangeAsync(perfumes);
        var savedCount = await _context.SaveChangesAsync();
        
        _logger.LogInformation("Successfully saved {SavedCount} perfumes to database", savedCount);
        
        return savedCount;
    }
    
    private string ExtractBrandFromRemarks(string remarks)
    {
        // Extract brand from remarks field: "Brand: [BrandName], Original Size: ..."
        if (string.IsNullOrWhiteSpace(remarks))
            return "";
            
        var brandPrefix = "Brand: ";
        var startIndex = remarks.IndexOf(brandPrefix);
        if (startIndex == -1)
            return "";
            
        startIndex += brandPrefix.Length;
        var endIndex = remarks.IndexOf(',', startIndex);
        if (endIndex == -1)
            endIndex = remarks.Length;
            
        return remarks.Substring(startIndex, endIndex - startIndex).Trim();
    }
    
    private Brand? FindMatchingBrand(string brandName, List<Brand> existingBrands)
    {
        if (string.IsNullOrWhiteSpace(brandName))
            return null;
            
        // Try exact match first
        var exactMatch = existingBrands.FirstOrDefault(b => 
            string.Equals(b.Name, brandName, StringComparison.OrdinalIgnoreCase));
        if (exactMatch != null)
            return exactMatch;
            
        // Try partial match
        var partialMatch = existingBrands.FirstOrDefault(b => 
            b.Name.Contains(brandName, StringComparison.OrdinalIgnoreCase) ||
            brandName.Contains(b.Name, StringComparison.OrdinalIgnoreCase));
        if (partialMatch != null)
            return partialMatch;
            
        // Try without common suffixes/prefixes
        var cleanBrandName = CleanBrandName(brandName);
        var cleanMatch = existingBrands.FirstOrDefault(b => 
            string.Equals(CleanBrandName(b.Name), cleanBrandName, StringComparison.OrdinalIgnoreCase));
            
        return cleanMatch;
    }
    
    private string CleanBrandName(string brandName)
    {
        return brandName
            .Replace("(brand)", "", StringComparison.OrdinalIgnoreCase)
            .Replace("perfumes", "", StringComparison.OrdinalIgnoreCase)
            .Replace("parfums", "", StringComparison.OrdinalIgnoreCase)
            .Replace("fragrance", "", StringComparison.OrdinalIgnoreCase)
            .Replace("ltd", "", StringComparison.OrdinalIgnoreCase)
            .Replace("inc", "", StringComparison.OrdinalIgnoreCase)
            .Replace(".", "")
            .Replace(",", "")
            .Trim();
    }
}

public class PerfumeImportResult
{
    public int TotalRecordsProcessed { get; set; }
    public List<Perfume> SuccessfullyImported { get; set; } = new();
    public List<UnmatchedBrand> UnmatchedBrands { get; set; } = new();
    public List<BrandMappingError> BrandMappingErrors { get; set; } = new();
    public List<SacksAIPlatform.DataLayer.Csv.Interfaces.CsvValidationError> CsvValidationErrors { get; set; } = new();
    
    public int SuccessfullyImportedCount => SuccessfullyImported.Count;
    public int UnmatchedBrandsCount => UnmatchedBrands.Count;
    public int CsvErrorsCount => CsvValidationErrors.Count;
    public int BrandMappingErrorsCount => BrandMappingErrors.Count;
}

public class UnmatchedBrand
{
    public string BrandName { get; set; } = string.Empty;
    public string PerfumeName { get; set; } = string.Empty;
    public string UPC { get; set; } = string.Empty;
}

public class BrandMappingError
{
    public string PerfumeName { get; set; } = string.Empty;
    public string ExtractedBrandName { get; set; } = string.Empty;
    public string UPC { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
