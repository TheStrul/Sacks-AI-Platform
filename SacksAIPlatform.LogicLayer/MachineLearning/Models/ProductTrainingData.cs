using System.Text.Json.Serialization;

namespace SacksAIPlatform.LogicLayer.MachineLearning.Models;

/// <summary>
/// Training data point for the AI product agent
/// Follows ML standard data structure for supervised learning
/// </summary>
public class ProductTrainingData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [JsonPropertyName("features")]
    public ProductFeatures Features { get; set; } = new();
    
    [JsonPropertyName("labels")]
    public ProductLabels Labels { get; set; } = new();
    
    [JsonPropertyName("metadata")]
    public TrainingMetadata Metadata { get; set; } = new();
    
    [JsonPropertyName("quality_score")]
    public double QualityScore { get; set; }
    
    [JsonPropertyName("validation_status")]
    public ValidationStatus ValidationStatus { get; set; }
}

/// <summary>
/// Feature vector for machine learning model
/// Input features for prediction and classification
/// </summary>
public class ProductFeatures
{
    // Raw text features
    [JsonPropertyName("raw_product_name")]
    public string RawProductName { get; set; } = string.Empty;
    
    [JsonPropertyName("raw_brand_name")]
    public string RawBrandName { get; set; } = string.Empty;
    
    [JsonPropertyName("raw_size_text")]
    public string RawSizeText { get; set; } = string.Empty;
    
    [JsonPropertyName("raw_concentration_text")]
    public string RawConcentrationText { get; set; } = string.Empty;
    
    [JsonPropertyName("raw_type_text")]
    public string RawTypeText { get; set; } = string.Empty;
    
    // Extracted numeric features
    [JsonPropertyName("size_numeric")]
    public double SizeNumeric { get; set; }
    
    [JsonPropertyName("upc_length")]
    public int UpcLength { get; set; }
    
    [JsonPropertyName("name_length")]
    public int NameLength { get; set; }
    
    [JsonPropertyName("name_word_count")]
    public int NameWordCount { get; set; }
    
    // Text analysis features
    [JsonPropertyName("contains_brand_in_name")]
    public bool ContainsBrandInName { get; set; }
    
    [JsonPropertyName("contains_size_in_name")]
    public bool ContainsSizeInName { get; set; }
    
    [JsonPropertyName("contains_gender_indicators")]
    public string[] GenderIndicators { get; set; } = Array.Empty<string>();
    
    [JsonPropertyName("language_indicators")]
    public string[] LanguageIndicators { get; set; } = Array.Empty<string>();
    
    // Categorical features (one-hot encoded)
    [JsonPropertyName("concentration_category")]
    public string ConcentrationCategory { get; set; } = string.Empty;
    
    [JsonPropertyName("size_category")]
    public string SizeCategory { get; set; } = string.Empty; // small, medium, large
    
    [JsonPropertyName("price_category")]
    public string PriceCategory { get; set; } = string.Empty; // luxury, mid, budget
}

/// <summary>
/// Labels/targets for supervised learning
/// Ground truth data for training the AI agent
/// </summary>
public class ProductLabels
{
    [JsonPropertyName("concentration")]
    public string Concentration { get; set; } = string.Empty;
    
    [JsonPropertyName("gender")]
    public string Gender { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("units")]
    public string Units { get; set; } = string.Empty;
    
    [JsonPropertyName("brand_id")]
    public int? BrandId { get; set; }
    
    [JsonPropertyName("country_of_origin")]
    public string CountryOfOrigin { get; set; } = string.Empty;
    
    [JsonPropertyName("is_lilac_free")]
    public bool IsLilacFree { get; set; }
    
    [JsonPropertyName("predicted_price_range")]
    public string PredictedPriceRange { get; set; } = string.Empty;
    
    [JsonPropertyName("market_segment")]
    public string MarketSegment { get; set; } = string.Empty;
}

/// <summary>
/// Metadata for training data management and model versioning
/// </summary>
public class TrainingMetadata
{
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;
    
    [JsonPropertyName("data_version")]
    public string DataVersion { get; set; } = "1.0";
    
    [JsonPropertyName("model_version")]
    public string ModelVersion { get; set; } = "1.0";
    
    [JsonPropertyName("user_feedback")]
    public UserFeedback UserFeedback { get; set; } = new();
    
    [JsonPropertyName("processing_time_ms")]
    public long ProcessingTimeMs { get; set; }
    
    [JsonPropertyName("confidence_scores")]
    public Dictionary<string, double> ConfidenceScores { get; set; } = new();
    
    [JsonPropertyName("feature_importance")]
    public Dictionary<string, double> FeatureImportance { get; set; } = new();
}

/// <summary>
/// User feedback for reinforcement learning and model improvement
/// </summary>
public class UserFeedback
{
    [JsonPropertyName("approved")]
    public bool Approved { get; set; }
    
    [JsonPropertyName("corrections")]
    public Dictionary<string, string> Corrections { get; set; } = new();
    
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;
    
    [JsonPropertyName("feedback_timestamp")]
    public DateTime FeedbackTimestamp { get; set; } = DateTime.UtcNow;
    
    [JsonPropertyName("quality_rating")]
    public int QualityRating { get; set; } // 1-5 scale
    
    [JsonPropertyName("comments")]
    public string Comments { get; set; } = string.Empty;
}

/// <summary>
/// Validation status for quality control
/// </summary>
public enum ValidationStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    NeedsReview = 3,
    AutoApproved = 4
}

/// <summary>
/// Knowledge base entry for continuous learning
/// </summary>
public class KnowledgeBaseEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [JsonPropertyName("rule_type")]
    public string RuleType { get; set; } = string.Empty; // brand_mapping, concentration_detection, etc.
    
    [JsonPropertyName("pattern")]
    public string Pattern { get; set; } = string.Empty;
    
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;
    
    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }
    
    [JsonPropertyName("usage_count")]
    public int UsageCount { get; set; }
    
    [JsonPropertyName("success_rate")]
    public double SuccessRate { get; set; }
    
    [JsonPropertyName("created_timestamp")]
    public DateTime CreatedTimestamp { get; set; } = DateTime.UtcNow;
    
    [JsonPropertyName("last_used")]
    public DateTime? LastUsed { get; set; }
    
    [JsonPropertyName("created_by")]
    public string CreatedBy { get; set; } = string.Empty;
}
