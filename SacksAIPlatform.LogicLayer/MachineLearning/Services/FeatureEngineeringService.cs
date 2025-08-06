using SacksAIPlatform.LogicLayer.MachineLearning.Models;

namespace SacksAIPlatform.LogicLayer.MachineLearning.Services;

/// <summary>
/// Feature engineering service for ML pipeline
/// Extracts and transforms raw data into ML-ready features
/// </summary>
public class FeatureEngineeringService
{
    public ProductFeatures ExtractFeatures(string csvLine, string[] fields)
    {
        var features = new ProductFeatures();
        
        if (fields.Length < 10) return features;
        
        // Raw text features (input from CSV)
        features.RawProductName = fields[3]?.Trim() ?? "";
        features.RawBrandName = fields[2]?.Trim() ?? "";
        features.RawSizeText = fields[4]?.Trim() ?? "";
        features.RawConcentrationText = fields[6]?.Trim() ?? "";
        features.RawTypeText = fields[5]?.Trim() ?? "";
        
        // Numeric features
        features.SizeNumeric = ExtractNumericSize(features.RawSizeText);
        features.UpcLength = fields[1]?.Length ?? 0;
        features.NameLength = features.RawProductName.Length;
        features.NameWordCount = features.RawProductName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        
        // Text analysis features
        features.ContainsBrandInName = ContainsBrandInName(features.RawProductName, features.RawBrandName);
        features.ContainsSizeInName = ContainsSizeInName(features.RawProductName);
        features.GenderIndicators = ExtractGenderIndicators(features.RawProductName);
        features.LanguageIndicators = ExtractLanguageIndicators(features.RawProductName);
        
        // Categorical features
        features.ConcentrationCategory = CategorizeConcentration(features.RawConcentrationText);
        features.SizeCategory = CategorizeSize(features.SizeNumeric);
        features.PriceCategory = EstimatePriceCategory(features.RawBrandName, features.SizeNumeric);
        
        return features;
    }
    
    private double ExtractNumericSize(string sizeText)
    {
        if (string.IsNullOrWhiteSpace(sizeText)) return 0;
        
        var match = System.Text.RegularExpressions.Regex.Match(sizeText, @"[\d.]+");
        return match.Success ? double.Parse(match.Value) : 0;
    }
    
    private bool ContainsBrandInName(string productName, string brandName)
    {
        if (string.IsNullOrWhiteSpace(productName) || string.IsNullOrWhiteSpace(brandName))
            return false;
            
        return productName.Contains(brandName, StringComparison.OrdinalIgnoreCase);
    }
    
    private bool ContainsSizeInName(string productName)
    {
        var sizePatterns = new[] { @"\d+ml", @"\d+oz", @"\d+\s*ml", @"\d+\s*oz" };
        return sizePatterns.Any(pattern => 
            System.Text.RegularExpressions.Regex.IsMatch(productName, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }
    
    private string[] ExtractGenderIndicators(string productName)
    {
        var indicators = new List<string>();
        var lowerName = productName.ToLowerInvariant();
        
        var maleIndicators = new[] { "men", "male", "homme", "masculin", "guy", "gentleman" };
        var femaleIndicators = new[] { "women", "female", "femme", "feminine", "lady", "girl" };
        
        if (maleIndicators.Any(indicator => lowerName.Contains(indicator)))
            indicators.Add("male");
            
        if (femaleIndicators.Any(indicator => lowerName.Contains(indicator)))
            indicators.Add("female");
            
        return indicators.ToArray();
    }
    
    private string[] ExtractLanguageIndicators(string productName)
    {
        var indicators = new List<string>();
        var lowerName = productName.ToLowerInvariant();
        
        var frenchWords = new[] { "eau", "de", "pour", "homme", "femme", "nuit", "jour" };
        var englishWords = new[] { "spray", "cologne", "perfume", "fragrance", "scent" };
        var italianWords = new[] { "uomo", "donna", "acqua", "profumo" };
        
        if (frenchWords.Any(word => lowerName.Contains(word)))
            indicators.Add("french");
            
        if (englishWords.Any(word => lowerName.Contains(word)))
            indicators.Add("english");
            
        if (italianWords.Any(word => lowerName.Contains(word)))
            indicators.Add("italian");
            
        return indicators.ToArray();
    }
    
    private string CategorizeConcentration(string concentrationText)
    {
        if (string.IsNullOrWhiteSpace(concentrationText))
            return "unknown";
            
        var lower = concentrationText.ToLowerInvariant();
        
        if (lower.Contains("parfum") || lower.Contains("extrait"))
            return "high";
        else if (lower.Contains("eau de parfum") || lower.Contains("edp"))
            return "medium-high";
        else if (lower.Contains("eau de toilette") || lower.Contains("edt"))
            return "medium";
        else if (lower.Contains("eau de cologne") || lower.Contains("edc"))
            return "light";
        else
            return "unknown";
    }
    
    private string CategorizeSize(double sizeNumeric)
    {
        return sizeNumeric switch
        {
            <= 0 => "unknown",
            <= 30 => "mini",
            <= 50 => "small",
            <= 100 => "medium",
            <= 150 => "large",
            _ => "extra-large"
        };
    }
    
    private string EstimatePriceCategory(string brandName, double size)
    {
        var luxuryBrands = new[] { "chanel", "dior", "guerlain", "tom ford", "creed", "clive christian" };
        var midBrands = new[] { "calvin klein", "hugo boss", "armani", "burberry", "prada" };
        
        var lowerBrand = brandName.ToLowerInvariant();
        
        if (luxuryBrands.Any(brand => lowerBrand.Contains(brand)))
            return "luxury";
        else if (midBrands.Any(brand => lowerBrand.Contains(brand)))
            return "mid-range";
        else if (size > 100) // Large sizes often indicate budget/mass market
            return "budget";
        else
            return "unknown";
    }
}
