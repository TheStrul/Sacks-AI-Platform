using SacksAIPlatform.LogicLayer.MachineLearning.Models;
using System.Text.Json;

namespace SacksAIPlatform.LogicLayer.MachineLearning.Services;

/// <summary>
/// Training data collection service for generating ML datasets
/// Collects and manages training data for continuous model improvement
/// </summary>
public class TrainingDataService
{
    private readonly List<ProductTrainingData> _trainingData = new();
    private readonly string _trainingDataPath = "training_data.json";
    
    public TrainingDataService()
    {
        LoadTrainingData();
    }
    
    /// <summary>
    /// Create a training data point from processed product data
    /// </summary>
    public ProductTrainingData CreateTrainingData(
        ProductFeatures features, 
        ProductLabels labels, 
        Dictionary<string, double> confidenceScores,
        UserFeedback? userFeedback = null,
        string source = "csv_import")
    {
        var trainingData = new ProductTrainingData
        {
            Features = features,
            Labels = labels,
            QualityScore = CalculateQualityScore(confidenceScores),
            ValidationStatus = userFeedback?.Approved == true ? ValidationStatus.Approved : ValidationStatus.Pending,
            Metadata = new TrainingMetadata
            {
                Source = source,
                ConfidenceScores = confidenceScores,
                UserFeedback = userFeedback ?? new UserFeedback(),
                ProcessingTimeMs = 0 // Will be set by the calling service
            }
        };
        
        return trainingData;
    }
    
    /// <summary>
    /// Add training data point to the collection
    /// </summary>
    public void AddTrainingData(ProductTrainingData trainingData)
    {
        _trainingData.Add(trainingData);
        SaveTrainingData();
    }
    
    /// <summary>
    /// Batch add multiple training data points
    /// </summary>
    public void AddBatchTrainingData(IEnumerable<ProductTrainingData> trainingDataBatch)
    {
        _trainingData.AddRange(trainingDataBatch);
        SaveTrainingData();
    }
    
    /// <summary>
    /// Update existing training data with user feedback
    /// </summary>
    public void UpdateWithFeedback(string trainingDataId, UserFeedback feedback)
    {
        var trainingData = _trainingData.FirstOrDefault(td => td.Id == trainingDataId);
        if (trainingData == null) return;
        
        trainingData.Metadata.UserFeedback = feedback;
        trainingData.ValidationStatus = feedback.Approved ? ValidationStatus.Approved : ValidationStatus.Rejected;
        
        // Update quality score based on user rating
        if (feedback.QualityRating > 0)
        {
            trainingData.QualityScore = feedback.QualityRating / 5.0; // Convert 1-5 to 0-1
        }
        
        SaveTrainingData();
    }
    
    /// <summary>
    /// Get training data for ML model training (approved data only)
    /// </summary>
    public List<ProductTrainingData> GetApprovedTrainingData()
    {
        return _trainingData
            .Where(td => td.ValidationStatus == ValidationStatus.Approved)
            .Where(td => td.QualityScore >= 0.7) // High quality only
            .OrderByDescending(td => td.QualityScore)
            .ToList();
    }
    
    /// <summary>
    /// Get training data that needs review
    /// </summary>
    public List<ProductTrainingData> GetPendingReviewData()
    {
        return _trainingData
            .Where(td => td.ValidationStatus == ValidationStatus.Pending || 
                        td.ValidationStatus == ValidationStatus.NeedsReview)
            .OrderByDescending(td => td.Timestamp)
            .ToList();
    }
    
    /// <summary>
    /// Generate training dataset in ML-ready format
    /// </summary>
    public MLDataset GenerateMLDataset(string datasetType = "classification")
    {
        var approvedData = GetApprovedTrainingData();
        
        var dataset = new MLDataset
        {
            DatasetType = datasetType,
            Version = "1.0",
            CreatedAt = DateTime.UtcNow,
            TotalSamples = approvedData.Count,
            Features = ExtractFeatureMatrix(approvedData),
            Labels = ExtractLabelMatrix(approvedData),
            Metadata = GenerateDatasetMetadata(approvedData)
        };
        
        return dataset;
    }
    
    /// <summary>
    /// Export training data to various formats for external ML tools
    /// </summary>
    public void ExportTrainingData(string format, string outputPath)
    {
        var approvedData = GetApprovedTrainingData();
        
        switch (format.ToLowerInvariant())
        {
            case "json":
                ExportToJson(approvedData, outputPath);
                break;
            case "csv":
                ExportToCsv(approvedData, outputPath);
                break;
            case "ml-ready":
                ExportToMLReady(approvedData, outputPath);
                break;
            default:
                throw new ArgumentException($"Unsupported export format: {format}");
        }
    }
    
    /// <summary>
    /// Get training statistics and insights
    /// </summary>
    public TrainingStatistics GetTrainingStatistics()
    {
        var stats = new TrainingStatistics
        {
            TotalSamples = _trainingData.Count,
            ApprovedSamples = _trainingData.Count(td => td.ValidationStatus == ValidationStatus.Approved),
            PendingSamples = _trainingData.Count(td => td.ValidationStatus == ValidationStatus.Pending),
            RejectedSamples = _trainingData.Count(td => td.ValidationStatus == ValidationStatus.Rejected),
            AvgQualityScore = _trainingData.Average(td => td.QualityScore),
            
            // Distribution by source
            SourceDistribution = _trainingData
                .GroupBy(td => td.Metadata.Source)
                .ToDictionary(g => g.Key, g => g.Count()),
                
            // Quality distribution
            QualityDistribution = _trainingData
                .GroupBy(td => td.QualityScore switch
                {
                    >= 0.8 => "High",
                    >= 0.6 => "Medium",
                    >= 0.4 => "Low",
                    _ => "Very Low"
                })
                .ToDictionary(g => g.Key, g => g.Count()),
                
            // Label distribution (for concentration)
            ConcentrationDistribution = _trainingData
                .GroupBy(td => td.Labels.Concentration)
                .ToDictionary(g => g.Key, g => g.Count()),
                
            LastUpdated = DateTime.UtcNow
        };
        
        return stats;
    }
    
    private double CalculateQualityScore(Dictionary<string, double> confidenceScores)
    {
        if (confidenceScores.Count == 0) return 0.5;
        return confidenceScores.Values.Average();
    }
    
    private double[,] ExtractFeatureMatrix(List<ProductTrainingData> data)
    {
        var features = new double[data.Count, 15]; // 15 key numeric features
        
        for (int i = 0; i < data.Count; i++)
        {
            var f = data[i].Features;
            features[i, 0] = f.SizeNumeric;
            features[i, 1] = f.UpcLength;
            features[i, 2] = f.NameLength;
            features[i, 3] = f.NameWordCount;
            features[i, 4] = f.ContainsBrandInName ? 1 : 0;
            features[i, 5] = f.ContainsSizeInName ? 1 : 0;
            features[i, 6] = f.GenderIndicators.Length;
            features[i, 7] = f.LanguageIndicators.Length;
            features[i, 8] = EncodeConcentrationCategory(f.ConcentrationCategory);
            features[i, 9] = EncodeSizeCategory(f.SizeCategory);
            features[i, 10] = EncodePriceCategory(f.PriceCategory);
            features[i, 11] = f.GenderIndicators.Contains("male") ? 1 : 0;
            features[i, 12] = f.GenderIndicators.Contains("female") ? 1 : 0;
            features[i, 13] = f.LanguageIndicators.Contains("french") ? 1 : 0;
            features[i, 14] = f.LanguageIndicators.Contains("english") ? 1 : 0;
        }
        
        return features;
    }
    
    private string[,] ExtractLabelMatrix(List<ProductTrainingData> data)
    {
        var labels = new string[data.Count, 5]; // 5 main label categories
        
        for (int i = 0; i < data.Count; i++)
        {
            var l = data[i].Labels;
            labels[i, 0] = l.Concentration;
            labels[i, 1] = l.Gender;
            labels[i, 2] = l.Type;
            labels[i, 3] = l.Units;
            labels[i, 4] = l.CountryOfOrigin;
        }
        
        return labels;
    }
    
    private Dictionary<string, object> GenerateDatasetMetadata(List<ProductTrainingData> data)
    {
        return new Dictionary<string, object>
        {
            ["feature_names"] = new[] 
            { 
                "size_numeric", "upc_length", "name_length", "name_word_count",
                "contains_brand_in_name", "contains_size_in_name", "gender_indicators_count",
                "language_indicators_count", "concentration_category", "size_category",
                "price_category", "has_male_indicators", "has_female_indicators",
                "has_french_indicators", "has_english_indicators"
            },
            ["label_names"] = new[] { "concentration", "gender", "type", "units", "country" },
            ["data_sources"] = data.Select(d => d.Metadata.Source).Distinct().ToArray(),
            ["quality_range"] = new { min = data.Min(d => d.QualityScore), max = data.Max(d => d.QualityScore) },
            ["date_range"] = new { start = data.Min(d => d.Timestamp), end = data.Max(d => d.Timestamp) }
        };
    }
    
    private void ExportToJson(List<ProductTrainingData> data, string outputPath)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outputPath, json);
    }
    
    private void ExportToCsv(List<ProductTrainingData> data, string outputPath)
    {
        var lines = new List<string>();
        
        // Header
        lines.Add("id,timestamp,raw_product_name,raw_brand_name,size_numeric,concentration,gender,type,units,quality_score,validation_status");
        
        // Data rows
        foreach (var item in data)
        {
            var line = string.Join(",", 
                item.Id,
                item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                EscapeCsvField(item.Features.RawProductName),
                EscapeCsvField(item.Features.RawBrandName),
                item.Features.SizeNumeric,
                item.Labels.Concentration,
                item.Labels.Gender,
                item.Labels.Type,
                item.Labels.Units,
                item.QualityScore.ToString("F3"),
                item.ValidationStatus.ToString()
            );
            lines.Add(line);
        }
        
        File.WriteAllLines(outputPath, lines);
    }
    
    private void ExportToMLReady(List<ProductTrainingData> data, string outputPath)
    {
        var dataset = GenerateMLDataset();
        var json = JsonSerializer.Serialize(dataset, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outputPath, json);
    }
    
    private string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field)) return "";
        if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
        {
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        }
        return field;
    }
    
    private double EncodeConcentrationCategory(string category)
    {
        return category switch
        {
            "high" => 4,
            "medium-high" => 3,
            "medium" => 2,
            "light" => 1,
            _ => 0
        };
    }
    
    private double EncodeSizeCategory(string category)
    {
        return category switch
        {
            "extra-large" => 5,
            "large" => 4,
            "medium" => 3,
            "small" => 2,
            "mini" => 1,
            _ => 0
        };
    }
    
    private double EncodePriceCategory(string category)
    {
        return category switch
        {
            "luxury" => 3,
            "mid-range" => 2,
            "budget" => 1,
            _ => 0
        };
    }
    
    private void LoadTrainingData()
    {
        try
        {
            if (File.Exists(_trainingDataPath))
            {
                var json = File.ReadAllText(_trainingDataPath);
                var data = JsonSerializer.Deserialize<List<ProductTrainingData>>(json);
                if (data != null)
                {
                    _trainingData.AddRange(data);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not load training data: {ex.Message}");
        }
    }
    
    private void SaveTrainingData()
    {
        try
        {
            var json = JsonSerializer.Serialize(_trainingData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_trainingDataPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not save training data: {ex.Message}");
        }
    }
}

/// <summary>
/// ML-ready dataset structure
/// </summary>
public class MLDataset
{
    public string DatasetType { get; set; } = "";
    public string Version { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public int TotalSamples { get; set; }
    public double[,] Features { get; set; } = new double[0,0];
    public string[,] Labels { get; set; } = new string[0,0];
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Training statistics for monitoring and insights
/// </summary>
public class TrainingStatistics
{
    public int TotalSamples { get; set; }
    public int ApprovedSamples { get; set; }
    public int PendingSamples { get; set; }
    public int RejectedSamples { get; set; }
    public double AvgQualityScore { get; set; }
    public Dictionary<string, int> SourceDistribution { get; set; } = new();
    public Dictionary<string, int> QualityDistribution { get; set; } = new();
    public Dictionary<string, int> ConcentrationDistribution { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}
