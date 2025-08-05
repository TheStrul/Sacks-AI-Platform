using SacksAIPlatform.LogicLayer.MachineLearning.Models;
using SacksAIPlatform.DataLayer.Entities;

namespace SacksAIPlatform.LogicLayer.MachineLearning.Interfaces;

/// <summary>
/// Machine Learning pipeline for AI product agent training
/// Follows MLOps best practices for data processing and model training
/// </summary>
public interface IAIPerfumeAgent
{
    /// <summary>
    /// Processes raw CSV data and generates training dataset
    /// </summary>
    Task<MLProcessingResult> ProcessTrainingDataAsync(string csvFilePath, string userId = "system");
    
    /// <summary>
    /// Extracts features from raw product data for ML model
    /// </summary>
    ProductFeatures ExtractFeatures(string csvLine, Dictionary<string, object> context);
    
    /// <summary>
    /// Predicts product properties using trained model
    /// </summary>
    Task<ProductPrediction> PredictProductPropertiesAsync(ProductFeatures features);
    
    /// <summary>
    /// Updates knowledge base with user feedback (reinforcement learning)
    /// </summary>
    Task UpdateKnowledgeBaseAsync(string trainingDataId, UserFeedback feedback);
    
    /// <summary>
    /// Generates training dataset in ML-standard format
    /// </summary>
    Task<string> ExportTrainingDataAsync(string outputPath, TrainingDataFormat format);
    
    /// <summary>
    /// Validates and scores product data quality
    /// </summary>
    Task<ValidationResult> ValidateProductDataAsync(Perfume perfume, ProductTrainingData trainingData);
}

/// <summary>
/// Knowledge base management for continuous learning
/// </summary>
public interface IKnowledgeBaseManager
{
    /// <summary>
    /// Adds new knowledge from user interactions
    /// </summary>
    Task AddKnowledgeAsync(KnowledgeBaseEntry entry);
    
    /// <summary>
    /// Retrieves applicable rules for processing
    /// </summary>
    Task<List<KnowledgeBaseEntry>> GetApplicableRulesAsync(string ruleType, string pattern);
    
    /// <summary>
    /// Updates rule performance metrics
    /// </summary>
    Task UpdateRulePerformanceAsync(string ruleId, bool success);
    
    /// <summary>
    /// Exports knowledge base for model training
    /// </summary>
    Task<string> ExportKnowledgeBaseAsync(string outputPath);
}

/// <summary>
/// Result of ML processing pipeline
/// </summary>
public class MLProcessingResult
{
    public List<Perfume> ValidatedProducts { get; set; } = new();
    public List<ProductTrainingData> TrainingDataset { get; set; } = new();
    public List<KnowledgeBaseEntry> NewKnowledgeEntries { get; set; } = new();
    public MLMetrics Metrics { get; set; } = new();
    public string TrainingDataPath { get; set; } = string.Empty;
    public string KnowledgeBasePath { get; set; } = string.Empty;
}

/// <summary>
/// ML model prediction result
/// </summary>
public class ProductPrediction
{
    public ProductLabels PredictedLabels { get; set; } = new();
    public Dictionary<string, double> ConfidenceScores { get; set; } = new();
    public string ModelVersion { get; set; } = string.Empty;
    public DateTime PredictionTimestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Validation result for quality assurance
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public double QualityScore { get; set; }
    public List<string> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public ValidationStatus Status { get; set; }
}

/// <summary>
/// ML performance metrics
/// </summary>
public class MLMetrics
{
    public int TotalRecordsProcessed { get; set; }
    public int ValidRecordsCount { get; set; }
    public int TrainingRecordsGenerated { get; set; }
    public int KnowledgeEntriesCreated { get; set; }
    public double AverageQualityScore { get; set; }
    public double ProcessingTimeSeconds { get; set; }
    public Dictionary<string, int> FeatureDistribution { get; set; } = new();
    public Dictionary<string, double> ConfidenceMetrics { get; set; } = new();
}

/// <summary>
/// Training data export formats
/// </summary>
public enum TrainingDataFormat
{
    Json,
    Csv,
    Parquet,
    TensorFlow,
    PyTorch
}
