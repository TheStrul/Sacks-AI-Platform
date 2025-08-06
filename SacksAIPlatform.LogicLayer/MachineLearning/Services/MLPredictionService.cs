using SacksAIPlatform.LogicLayer.MachineLearning.Models;
using SacksAIPlatform.DataLayer.Enums;

namespace SacksAIPlatform.LogicLayer.MachineLearning.Services;

/// <summary>
/// ML prediction service that uses trained models and knowledge base
/// to make intelligent predictions about product attributes
/// </summary>
public class MLPredictionService
{
    private readonly KnowledgeBaseService _knowledgeBase;
    private readonly FeatureEngineeringService _featureEngineering;
    
    public MLPredictionService(KnowledgeBaseService knowledgeBase, FeatureEngineeringService featureEngineering)
    {
        _knowledgeBase = knowledgeBase;
        _featureEngineering = featureEngineering;
    }
    
    /// <summary>
    /// Predict product labels based on extracted features
    /// </summary>
    public ProductLabels PredictLabels(ProductFeatures features)
    {
        var labels = new ProductLabels();
        var confidenceScores = new Dictionary<string, double>();
        
        // Predict concentration
        var (concentration, concConfidence) = PredictConcentration(features);
        labels.Concentration = concentration;
        confidenceScores["concentration"] = concConfidence;
        
        // Predict gender
        var (gender, genderConfidence) = PredictGender(features);
        labels.Gender = gender;
        confidenceScores["gender"] = genderConfidence;
        
        // Predict type
        var (type, typeConfidence) = PredictType(features);
        labels.Type = type;
        confidenceScores["type"] = typeConfidence;
        
        // Predict units
        var (units, unitsConfidence) = PredictUnits(features);
        labels.Units = units;
        confidenceScores["units"] = unitsConfidence;
        
        // Predict country (based on language indicators and brand patterns)
        var (country, countryConfidence) = PredictCountry(features);
        labels.CountryOfOrigin = country;
        confidenceScores["country"] = countryConfidence;
        
        // Predict lilac-free status (conservative approach)
        labels.IsLilacFree = PredictIsLilacFree(features);
        confidenceScores["lilac_free"] = 0.5; // Low confidence without specific data
        
        // Predict price range and market segment
        labels.PredictedPriceRange = features.PriceCategory;
        labels.MarketSegment = DetermineMarketSegment(features);
        
        return labels;
    }
    
    private (string concentration, double confidence) PredictConcentration(ProductFeatures features)
    {
        // First check knowledge base for learned patterns
        var kbSuggestion = _knowledgeBase.GetConcentrationSuggestion(features.RawConcentrationText);
        if (kbSuggestion != "Unknown")
        {
            return (MapToConcentrationEnum(kbSuggestion), 0.9);
        }
        
        // Rule-based prediction with confidence scoring
        var text = features.RawConcentrationText.ToLowerInvariant();
        var productName = features.RawProductName.ToLowerInvariant();
        
        if (text.Contains("parfum") && !text.Contains("eau"))
            return (Concentration.Parfum.ToString(), 0.95);
        else if (text.Contains("eau de parfum") || text.Contains("edp"))
            return (Concentration.EDP.ToString(), 0.9);
        else if (text.Contains("eau de toilette") || text.Contains("edt"))
            return (Concentration.EDT.ToString(), 0.9);
        else if (text.Contains("eau de cologne") || text.Contains("edc") || text.Contains("cologne"))
            return (Concentration.EDC.ToString(), 0.85);
        else if (text.Contains("eau fraiche") || text.Contains("fresh"))
            return (Concentration.EDF.ToString(), 0.8);
        
        // Check product name for concentration hints
        if (productName.Contains("parfum") && !productName.Contains("eau"))
            return (Concentration.Parfum.ToString(), 0.7);
        else if (productName.Contains("cologne"))
            return (Concentration.EDC.ToString(), 0.6);
        
        return (Concentration.EDT.ToString(), 0.3); // Default with low confidence
    }
    
    private (string gender, double confidence) PredictGender(ProductFeatures features)
    {
        var genderIndicators = features.GenderIndicators;
        var productName = features.RawProductName.ToLowerInvariant();
        
        bool hasMaleIndicators = genderIndicators.Contains("male");
        bool hasFemaleIndicators = genderIndicators.Contains("female");
        
        if (hasMaleIndicators && !hasFemaleIndicators)
            return (Gender.Male.ToString(), 0.8);
        else if (hasFemaleIndicators && !hasMaleIndicators)
            return (Gender.Female.ToString(), 0.8);
        else if (hasMaleIndicators && hasFemaleIndicators)
            return (Gender.Unisex.ToString(), 0.7);
        
        // Secondary analysis based on brand positioning and size
        if (features.SizeNumeric > 100 && features.PriceCategory == "budget")
            return (Gender.Male.ToString(), 0.4); // Large budget fragrances often masculine
        else if (features.SizeNumeric <= 50 && features.PriceCategory == "luxury")
            return (Gender.Female.ToString(), 0.4); // Small luxury often feminine
        
        return (Gender.Unisex.ToString(), 0.5); // Default to unisex with medium confidence
    }
    
    private (string type, double confidence) PredictType(ProductFeatures features)
    {
        var productName = features.RawProductName.ToLowerInvariant();
        var typeText = features.RawTypeText.ToLowerInvariant();
        
        if (typeText.Contains("perfume") || typeText.Contains("parfum"))
            return (PerfumeType.Spray.ToString(), 0.9); // Most perfumes are sprays
        else if (typeText.Contains("cologne") || productName.Contains("cologne"))
            return (PerfumeType.Cologne.ToString(), 0.8);
        else if (typeText.Contains("spray") || productName.Contains("spray"))
            return (PerfumeType.Spray.ToString(), 0.7);
        else if (typeText.Contains("splash") || productName.Contains("splash"))
            return (PerfumeType.Splash.ToString(), 0.7);
        
        // Default to spray for most modern fragrances
        return (PerfumeType.Spray.ToString(), 0.4);
    }
    
    private (string units, double confidence) PredictUnits(ProductFeatures features)
    {
        var sizeText = features.RawSizeText.ToLowerInvariant();
        var productName = features.RawProductName.ToLowerInvariant();
        
        if (sizeText.Contains("ml") || productName.Contains("ml"))
            return (Units.ml.ToString(), 0.9);
        else if (sizeText.Contains("oz") || productName.Contains("oz"))
            return (Units.oz.ToString(), 0.9);
        
        // Default to milliliters (more common globally)
        return (Units.ml.ToString(), 0.6);
    }
    
    private (string country, double confidence) PredictCountry(ProductFeatures features)
    {
        var languageIndicators = features.LanguageIndicators;
        var brandName = features.RawBrandName.ToLowerInvariant();
        
        if (languageIndicators.Contains("french"))
            return (Country.France.ToString(), 0.7);
        else if (languageIndicators.Contains("italian"))
            return (Country.Italy.ToString(), 0.7);
        
        // Brand-based country prediction
        var frenchBrands = new[] { "chanel", "dior", "givenchy", "lancome", "guerlain" };
        var italianBrands = new[] { "versace", "dolce", "prada", "armani" };
        var germanBrands = new[] { "hugo boss", "jil sander" };
        var usaBrands = new[] { "calvin klein", "tommy hilfiger", "ralph lauren", "tom ford" };
        
        if (frenchBrands.Any(brand => brandName.Contains(brand)))
            return (Country.France.ToString(), 0.8);
        else if (italianBrands.Any(brand => brandName.Contains(brand)))
            return (Country.Italy.ToString(), 0.8);
        else if (germanBrands.Any(brand => brandName.Contains(brand)))
            return (Country.Germany.ToString(), 0.8);
        else if (usaBrands.Any(brand => brandName.Contains(brand)))
            return (Country.USA.ToString(), 0.8);
        
        return (Country.USA.ToString(), 0.2);
    }
    
    private bool PredictIsLilacFree(ProductFeatures features)
    {
        // Conservative approach - assume not lilac-free unless explicitly stated
        var productName = features.RawProductName.ToLowerInvariant();
        return productName.Contains("lilac free") || productName.Contains("lilac-free");
    }
    
    private string DetermineMarketSegment(ProductFeatures features)
    {
        var priceCategory = features.PriceCategory;
        var sizeCategory = features.SizeCategory;
        
        return priceCategory switch
        {
            "luxury" => "Premium",
            "mid-range" => "Mainstream",
            "budget" => "Mass Market",
            _ => sizeCategory switch
            {
                "mini" => "Travel/Sample",
                "extra-large" => "Value",
                _ => "General"
            }
        };
    }
    
    private string MapToConcentrationEnum(string concentration)
    {
        return concentration.ToLowerInvariant() switch
        {
            "high" => Concentration.Parfum.ToString(),
            "medium-high" => Concentration.EDP.ToString(),
            "medium" => Concentration.EDT.ToString(),
            "light" => Concentration.EDC.ToString(),
            _ => Concentration.EDT.ToString()
        };
    }
    
    /// <summary>
    /// Calculate overall quality score for the prediction
    /// </summary>
    public double CalculateQualityScore(ProductFeatures features, ProductLabels labels, Dictionary<string, double> confidenceScores)
    {
        // Weighted average of confidence scores
        var weights = new Dictionary<string, double>
        {
            ["concentration"] = 0.25,
            ["gender"] = 0.20,
            ["type"] = 0.15,
            ["units"] = 0.10,
            ["country"] = 0.15,
            ["completeness"] = 0.15
        };
        
        double totalScore = 0;
        double totalWeight = 0;
        
        foreach (var (key, weight) in weights)
        {
            if (key == "completeness")
            {
                // Completeness score based on how many fields are filled
                var completeness = CalculateCompletenessScore(features, labels);
                totalScore += completeness * weight;
            }
            else if (confidenceScores.ContainsKey(key))
            {
                totalScore += confidenceScores[key] * weight;
            }
            
            totalWeight += weight;
        }
        
        return totalWeight > 0 ? totalScore / totalWeight : 0.5;
    }
    
    private double CalculateCompletenessScore(ProductFeatures features, ProductLabels labels)
    {
        int filledFields = 0;
        int totalFields = 8; // Key fields we expect to be filled
        
        if (!string.IsNullOrWhiteSpace(features.RawProductName)) filledFields++;
        if (!string.IsNullOrWhiteSpace(features.RawBrandName)) filledFields++;
        if (features.SizeNumeric > 0) filledFields++;
        if (!string.IsNullOrWhiteSpace(labels.Concentration)) filledFields++;
        if (!string.IsNullOrWhiteSpace(labels.Gender)) filledFields++;
        if (!string.IsNullOrWhiteSpace(labels.Type)) filledFields++;
        if (!string.IsNullOrWhiteSpace(labels.Units)) filledFields++;
        if (!string.IsNullOrWhiteSpace(labels.CountryOfOrigin)) filledFields++;
        
        return (double)filledFields / totalFields;
    }
}
