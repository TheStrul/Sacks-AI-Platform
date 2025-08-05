using SacksAIPlatform.LogicLayer.MachineLearning.Models;
using SacksAIPlatform.LogicLayer.MachineLearning.Services;
using SacksAIPlatform.LogicLayer.Services;
using SacksAIPlatform.DataLayer.Entities;
using SacksAIPlatform.DataLayer.Enums;
using SacksAIPlatform.DataLayer.Repositories.Interfaces;
using System.Diagnostics;

namespace SacksAIPlatform.LogicLayer.MachineLearning.Pipeline;

/// <summary>
/// Complete ML pipeline orchestrator for AI product agent
/// Integrates all ML services to provide end-to-end product processing with dual outputs:
/// 1. Valid product collections for database
/// 2. Training data for continuous ML improvement
/// </summary>
public class ProductMLPipeline
{
    private readonly FeatureEngineeringService _featureEngineering;
    private readonly MLPredictionService _mlPrediction;
    private readonly KnowledgeBaseService _knowledgeBase;
    private readonly TrainingDataService _trainingData;
    private readonly IBrandRepository _brandRepository;
    
    public ProductMLPipeline(
        FeatureEngineeringService featureEngineering,
        MLPredictionService mlPrediction,
        KnowledgeBaseService knowledgeBase,
        TrainingDataService trainingData,
        IBrandRepository brandRepository)
    {
        _featureEngineering = featureEngineering;
        _mlPrediction = mlPrediction;
        _knowledgeBase = knowledgeBase;
        _trainingData = trainingData;
        _brandRepository = brandRepository;
    }
    
    /// <summary>
    /// Process CSV data through complete ML pipeline
    /// Returns both valid products and training data
    /// </summary>
    public async Task<MLPipelineResult> ProcessCsvDataAsync(string csvFilePath, bool interactiveMode = false)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new MLPipelineResult();
        
        Console.WriteLine($"🚀 Starting ML Pipeline for {csvFilePath}");
        Console.WriteLine($"📊 Mode: {(interactiveMode ? "Interactive" : "Automated")}");
        Console.WriteLine();
        
        try
        {
            // Step 1: Read and parse CSV data
            Console.WriteLine("📖 Step 1: Reading CSV data...");
            var csvLines = await File.ReadAllLinesAsync(csvFilePath);
            var dataLines = csvLines.Skip(1).ToArray(); // Skip header
            
            Console.WriteLine($"   Found {dataLines.Length} records to process");
            
            // Step 2: Process each record through ML pipeline
            Console.WriteLine("🔬 Step 2: Processing through ML pipeline...");
            
            int processedCount = 0;
            int validProductCount = 0;
            int trainingDataCount = 0;
            
            foreach (var line in dataLines)
            {
                try
                {
                    var pipelineItem = await ProcessSingleRecordAsync(line, processedCount + 1, interactiveMode);
                    
                    if (pipelineItem.IsValidProduct)
                    {
                        result.ValidProducts.Add(pipelineItem.Product!);
                        validProductCount++;
                    }
                    
                    if (pipelineItem.TrainingData != null)
                    {
                        result.TrainingDataPoints.Add(pipelineItem.TrainingData);
                        trainingDataCount++;
                    }
                    
                    processedCount++;
                    
                    // Progress update every 100 records
                    if (processedCount % 100 == 0)
                    {
                        Console.WriteLine($"   Processed: {processedCount}/{dataLines.Length} | Valid: {validProductCount} | Training: {trainingDataCount}");
                    }
                    
                    // Interactive mode - limit processing for demonstration
                    if (interactiveMode && processedCount >= 20)
                    {
                        Console.WriteLine($"   🎯 Interactive mode: Limited to {processedCount} records for demonstration");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ⚠️  Error processing record {processedCount + 1}: {ex.Message}");
                    processedCount++;
                }
            }
            
            // Step 3: Save training data to persistent storage
            Console.WriteLine("💾 Step 3: Saving training data...");
            if (result.TrainingDataPoints.Count > 0)
            {
                _trainingData.AddBatchTrainingData(result.TrainingDataPoints);
                Console.WriteLine($"   Saved {result.TrainingDataPoints.Count} training data points");
            }
            
            // Step 4: Generate insights and statistics
            Console.WriteLine("📈 Step 4: Generating insights...");
            result.Statistics = GeneratePipelineStatistics(result);
            result.KnowledgeBaseInsights = _knowledgeBase.GetTrainingInsights();
            result.TrainingStats = _trainingData.GetTrainingStatistics();
            
            stopwatch.Stop();
            result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
            
            // Final summary
            Console.WriteLine();
            Console.WriteLine("✅ ML Pipeline Complete!");
            Console.WriteLine($"⏱️  Processing Time: {stopwatch.ElapsedMilliseconds:N0} ms");
            Console.WriteLine($"📦 Valid Products: {result.ValidProducts.Count}");
            Console.WriteLine($"🎯 Training Data Points: {result.TrainingDataPoints.Count}");
            Console.WriteLine($"🧠 Knowledge Base Entries: {result.KnowledgeBaseInsights["total_entries"]}");
            Console.WriteLine();
            
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Pipeline Error: {ex.Message}");
            result.ErrorMessage = ex.Message;
            return result;
        }
    }
    
    /// <summary>
    /// Process a single CSV record through the complete ML pipeline
    /// </summary>
    private async Task<MLPipelineItem> ProcessSingleRecordAsync(string csvLine, int recordNumber, bool interactive)
    {
        var fields = csvLine.Split(',').Select(f => f.Trim('"')).ToArray();
        var processingStart = Stopwatch.StartNew();
        
        // Step 1: Feature Engineering
        var features = _featureEngineering.ExtractFeatures(csvLine, fields);
        
        // Step 2: ML Prediction
        var labels = _mlPrediction.PredictLabels(features);
        var confidenceScores = new Dictionary<string, double>(); // Would be populated by prediction service
        var qualityScore = _mlPrediction.CalculateQualityScore(features, labels, confidenceScores);
        
        // Step 3: Brand matching and validation
        var brands = await _brandRepository.GetAllAsync();
        var brandMatch = brands.FirstOrDefault(b => 
            b.Name.Equals(features.RawBrandName, StringComparison.OrdinalIgnoreCase));
        labels.BrandId = brandMatch?.BrandID;
        
        // Step 4: Create training data
        var trainingData = _trainingData.CreateTrainingData(
            features, 
            labels, 
            confidenceScores,
            source: "ml_pipeline");
        trainingData.Metadata.ProcessingTimeMs = processingStart.ElapsedMilliseconds;
        
        // Step 5: Determine if this creates a valid product
        bool isValidProduct = IsValidForDatabase(features, labels, qualityScore);
        Perfume? product = null;
        
        if (isValidProduct)
        {
            product = CreateProductFromPrediction(features, labels, fields);
        }
        
        // Step 6: Interactive feedback (if enabled)
        UserFeedback? userFeedback = null;
        if (interactive && recordNumber <= 10) // First 10 records in interactive mode
        {
            userFeedback = await CollectUserFeedbackAsync(csvLine, features, labels, product, recordNumber);
            
            if (userFeedback != null)
            {
                trainingData.Metadata.UserFeedback = userFeedback;
                trainingData.ValidationStatus = userFeedback.Approved ? ValidationStatus.Approved : ValidationStatus.NeedsReview;
                
                // Learn from corrections
                LearnFromUserFeedback(features, labels, userFeedback);
            }
        }
        
        return new MLPipelineItem
        {
            Features = features,
            Labels = labels,
            TrainingData = trainingData,
            Product = product,
            IsValidProduct = isValidProduct,
            QualityScore = qualityScore,
            UserFeedback = userFeedback
        };
    }
    
    /// <summary>
    /// Determine if the ML prediction is good enough for database insertion
    /// </summary>
    private bool IsValidForDatabase(ProductFeatures features, ProductLabels labels, double qualityScore)
    {
        // Quality thresholds
        if (qualityScore < 0.6) return false;
        
        // Required fields validation
        if (string.IsNullOrWhiteSpace(features.RawProductName)) return false;
        if (string.IsNullOrWhiteSpace(features.RawBrandName)) return false;
        if (labels.BrandId == null || labels.BrandId == 0) return false;
        if (features.SizeNumeric <= 0) return false;
        
        // Enum validation
        if (!IsValidEnum<Concentration>(labels.Concentration)) return false;
        if (!IsValidEnum<Gender>(labels.Gender)) return false;
        if (!IsValidEnum<PerfumeType>(labels.Type)) return false;
        if (!IsValidEnum<Units>(labels.Units)) return false;
        
        return true;
    }
    
    /// <summary>
    /// Create a Perfume entity from ML predictions
    /// </summary>
    private Perfume CreateProductFromPrediction(ProductFeatures features, ProductLabels labels, string[] csvFields)
    {
        return new Perfume
        {
            Name = features.RawProductName,
            BrandID = labels.BrandId!.Value,
            Size = features.SizeNumeric.ToString(),
            Concentration = Enum.Parse<Concentration>(labels.Concentration),
            Gender = Enum.Parse<Gender>(labels.Gender),
            Type = Enum.Parse<PerfumeType>(labels.Type),
            Units = Enum.Parse<Units>(labels.Units),
            CountryOfOrigin = labels.CountryOfOrigin,
            LilFree = labels.IsLilacFree,
            Remarks = "Generated by ML Pipeline"
        };
    }
    
    /// <summary>
    /// Collect user feedback in interactive mode
    /// </summary>
    private async Task<UserFeedback?> CollectUserFeedbackAsync(string csvLine, ProductFeatures features, ProductLabels labels, Perfume? product, int recordNumber)
    {
        Console.WriteLine($"\n🔍 Record {recordNumber} - ML Prediction Review");
        Console.WriteLine("📋 ORIGINAL CSV DATA:");
        Console.WriteLine($"   {csvLine}");
        Console.WriteLine("");
        Console.WriteLine("🔄 ML PREDICTIONS:");
        Console.WriteLine($"📝 Product: {features.RawProductName}");
        Console.WriteLine($"🏷️  Brand: {features.RawBrandName} (Matched ID: {labels.BrandId})");
        Console.WriteLine($"💧 Concentration: {labels.Concentration}");
        Console.WriteLine($"👤 Gender: {labels.Gender}");
        Console.WriteLine($"📏 Size: {features.SizeNumeric} {labels.Units}");
        Console.WriteLine($"✅ Valid for DB: {(product != null ? "Yes" : "No")}");
        
        Console.Write("Do you approve this prediction? (y/n/s to skip): ");
        var input = Console.ReadLine()?.ToLowerInvariant();
        
        if (input == "s") return null;
        
        var feedback = new UserFeedback
        {
            Approved = input == "y",
            UserId = "interactive_user",
            QualityRating = input == "y" ? 5 : 2
        };
        
        if (!feedback.Approved)
        {
            Console.WriteLine("What corrections would you like to make? (Press Enter to skip a field)");
            
            Console.Write($"Concentration (current: {labels.Concentration}): ");
            var concCorrection = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(concCorrection))
                feedback.Corrections["concentration"] = concCorrection;
                
            Console.Write($"Gender (current: {labels.Gender}): ");
            var genderCorrection = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(genderCorrection))
                feedback.Corrections["gender"] = genderCorrection;
        }
        
        return feedback;
    }
    
    /// <summary>
    /// Learn from user feedback and update knowledge base
    /// </summary>
    private void LearnFromUserFeedback(ProductFeatures features, ProductLabels labels, UserFeedback feedback)
    {
        foreach (var correction in feedback.Corrections)
        {
            _knowledgeBase.LearnFromCorrection(
                correction.Key,
                GetOriginalValue(correction.Key, features, labels),
                correction.Value,
                feedback.UserId);
        }
    }
    
    private string GetOriginalValue(string fieldType, ProductFeatures features, ProductLabels labels)
    {
        return fieldType switch
        {
            "concentration" => labels.Concentration,
            "gender" => labels.Gender,
            "type" => labels.Type,
            "units" => labels.Units,
            _ => ""
        };
    }
    
    private bool IsValidEnum<T>(string value) where T : struct, Enum
    {
        return Enum.TryParse<T>(value, out _);
    }
    
    private MLPipelineStatistics GeneratePipelineStatistics(MLPipelineResult result)
    {
        return new MLPipelineStatistics
        {
            TotalRecordsProcessed = result.TrainingDataPoints.Count,
            ValidProductsGenerated = result.ValidProducts.Count,
            TrainingDataPointsCreated = result.TrainingDataPoints.Count,
            SuccessRate = result.ValidProducts.Count / (double)Math.Max(1, result.TrainingDataPoints.Count),
            AvgQualityScore = result.TrainingDataPoints.Average(td => td.QualityScore),
            AvgProcessingTimeMs = result.TrainingDataPoints.Average(td => td.Metadata.ProcessingTimeMs),
            ConcentrationAccuracy = CalculateFieldAccuracy(result.TrainingDataPoints, "concentration"),
            GenderAccuracy = CalculateFieldAccuracy(result.TrainingDataPoints, "gender"),
            BrandMatchRate = result.TrainingDataPoints.Count(td => td.Labels.BrandId.HasValue) / (double)Math.Max(1, result.TrainingDataPoints.Count)
        };
    }
    
    private double CalculateFieldAccuracy(List<ProductTrainingData> trainingData, string field)
    {
        var approvedData = trainingData.Where(td => td.ValidationStatus == ValidationStatus.Approved).ToList();
        if (approvedData.Count == 0) return 0.0;
        
        var accurateCount = approvedData.Count(td => 
            !td.Metadata.UserFeedback.Corrections.ContainsKey(field));
            
        return accurateCount / (double)approvedData.Count;
    }
}

/// <summary>
/// Result container for complete ML pipeline processing
/// </summary>
public class MLPipelineResult
{
    public List<Perfume> ValidProducts { get; set; } = new();
    public List<ProductTrainingData> TrainingDataPoints { get; set; } = new();
    public MLPipelineStatistics Statistics { get; set; } = new();
    public Dictionary<string, object> KnowledgeBaseInsights { get; set; } = new();
    public TrainingStatistics TrainingStats { get; set; } = new();
    public long ProcessingTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Individual pipeline item for a single record
/// </summary>
public class MLPipelineItem
{
    public ProductFeatures Features { get; set; } = new();
    public ProductLabels Labels { get; set; } = new();
    public ProductTrainingData? TrainingData { get; set; }
    public Perfume? Product { get; set; }
    public bool IsValidProduct { get; set; }
    public double QualityScore { get; set; }
    public UserFeedback? UserFeedback { get; set; }
}

/// <summary>
/// Pipeline performance statistics
/// </summary>
public class MLPipelineStatistics
{
    public int TotalRecordsProcessed { get; set; }
    public int ValidProductsGenerated { get; set; }
    public int TrainingDataPointsCreated { get; set; }
    public double SuccessRate { get; set; }
    public double AvgQualityScore { get; set; }
    public double AvgProcessingTimeMs { get; set; }
    public double ConcentrationAccuracy { get; set; }
    public double GenderAccuracy { get; set; }
    public double BrandMatchRate { get; set; }
}
