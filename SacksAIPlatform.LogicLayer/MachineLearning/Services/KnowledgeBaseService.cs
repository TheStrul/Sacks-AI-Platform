using SacksAIPlatform.LogicLayer.MachineLearning.Models;
using System.Text.Json;

namespace SacksAIPlatform.LogicLayer.MachineLearning.Services;

/// <summary>
/// Knowledge base service for continuous learning and rule management
/// Maintains learned patterns and decision rules for AI agent improvement
/// </summary>
public class KnowledgeBaseService
{
    private readonly List<KnowledgeBaseEntry> _knowledgeBase = new();
    private readonly string _knowledgeBasePath = "knowledge_base.json";
    
    public KnowledgeBaseService()
    {
        LoadKnowledgeBase();
    }
    
    /// <summary>
    /// Add a new knowledge base entry from user feedback or successful pattern
    /// </summary>
    public void AddKnowledgeEntry(string ruleType, string pattern, string action, double confidence, string createdBy)
    {
        var entry = new KnowledgeBaseEntry
        {
            RuleType = ruleType,
            Pattern = pattern,
            Action = action,
            Confidence = confidence,
            CreatedBy = createdBy,
            UsageCount = 0,
            SuccessRate = 1.0
        };
        
        // Check for duplicate patterns
        var existing = _knowledgeBase.FirstOrDefault(e => 
            e.RuleType == ruleType && e.Pattern.Equals(pattern, StringComparison.OrdinalIgnoreCase));
            
        if (existing != null)
        {
            // Update existing entry with reinforcement
            existing.Confidence = Math.Min(1.0, existing.Confidence + 0.1);
            existing.UsageCount++;
        }
        else
        {
            _knowledgeBase.Add(entry);
        }
        
        SaveKnowledgeBase();
    }
    
    /// <summary>
    /// Find applicable knowledge base entries for a given context
    /// </summary>
    public List<KnowledgeBaseEntry> FindApplicableRules(string ruleType, string context)
    {
        return _knowledgeBase
            .Where(entry => entry.RuleType == ruleType)
            .Where(entry => IsPatternMatch(entry.Pattern, context))
            .OrderByDescending(entry => entry.Confidence * entry.SuccessRate)
            .ToList();
    }
    
    /// <summary>
    /// Update success rate for a knowledge base entry
    /// </summary>
    public void UpdateSuccessRate(string entryId, bool wasSuccessful)
    {
        var entry = _knowledgeBase.FirstOrDefault(e => e.Id == entryId);
        if (entry == null) return;
        
        entry.UsageCount++;
        entry.LastUsed = DateTime.UtcNow;
        
        // Update success rate using weighted average
        var totalAttempts = entry.UsageCount;
        var previousSuccesses = (totalAttempts - 1) * entry.SuccessRate;
        var newSuccesses = previousSuccesses + (wasSuccessful ? 1 : 0);
        entry.SuccessRate = newSuccesses / totalAttempts;
        
        SaveKnowledgeBase();
    }
    
    /// <summary>
    /// Get brand mapping suggestions based on learned patterns
    /// </summary>
    public List<string> GetBrandMappingSuggestions(string brandText)
    {
        var suggestions = new List<string>();
        var brandRules = FindApplicableRules("brand_mapping", brandText);
        
        foreach (var rule in brandRules.Take(3)) // Top 3 suggestions
        {
            suggestions.Add(rule.Action);
        }
        
        return suggestions;
    }
    
    /// <summary>
    /// Get concentration detection suggestions
    /// </summary>
    public string GetConcentrationSuggestion(string concentrationText)
    {
        var rules = FindApplicableRules("concentration_detection", concentrationText);
        return rules.FirstOrDefault()?.Action ?? "Unknown";
    }
    
    /// <summary>
    /// Learn from user correction during interactive import
    /// </summary>
    public void LearnFromCorrection(string fieldType, string originalValue, string correctedValue, string userId)
    {
        if (string.IsNullOrWhiteSpace(originalValue) || string.IsNullOrWhiteSpace(correctedValue))
            return;
            
        var ruleType = $"{fieldType}_correction";
        var pattern = originalValue.Trim().ToLowerInvariant();
        var action = correctedValue.Trim();
        
        AddKnowledgeEntry(ruleType, pattern, action, 0.8, userId);
        
        Console.WriteLine($"💡 Learned: '{originalValue}' → '{correctedValue}' for {fieldType}");
    }
    
    /// <summary>
    /// Generate training insights from knowledge base patterns
    /// </summary>
    public Dictionary<string, object> GetTrainingInsights()
    {
        var insights = new Dictionary<string, object>();
        
        // Rule type distribution
        var ruleTypeDistribution = _knowledgeBase
            .GroupBy(entry => entry.RuleType)
            .ToDictionary(g => g.Key, g => g.Count());
            
        // Success rate analysis
        var avgSuccessRateByType = _knowledgeBase
            .GroupBy(entry => entry.RuleType)
            .ToDictionary(g => g.Key, g => g.Average(e => e.SuccessRate));
            
        // Most used patterns
        var topPatterns = _knowledgeBase
            .OrderByDescending(entry => entry.UsageCount)
            .Take(10)
            .Select(entry => new { entry.Pattern, entry.UsageCount, entry.SuccessRate })
            .ToList();
            
        insights["rule_type_distribution"] = ruleTypeDistribution;
        insights["avg_success_rate_by_type"] = avgSuccessRateByType;
        insights["top_patterns"] = topPatterns;
        insights["total_entries"] = _knowledgeBase.Count;
        insights["last_updated"] = DateTime.UtcNow;
        
        return insights;
    }
    
    private bool IsPatternMatch(string pattern, string context)
    {
        if (string.IsNullOrWhiteSpace(pattern) || string.IsNullOrWhiteSpace(context))
            return false;
            
        var lowerPattern = pattern.ToLowerInvariant();
        var lowerContext = context.ToLowerInvariant();
        
        // Simple pattern matching - can be enhanced with regex or fuzzy matching
        return lowerContext.Contains(lowerPattern) || 
               lowerPattern.Contains(lowerContext) ||
               CalculateLevenshteinDistance(lowerPattern, lowerContext) <= 2;
    }
    
    private int CalculateLevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source)) return target?.Length ?? 0;
        if (string.IsNullOrEmpty(target)) return source.Length;
        
        var matrix = new int[source.Length + 1, target.Length + 1];
        
        for (int i = 0; i <= source.Length; i++) matrix[i, 0] = i;
        for (int j = 0; j <= target.Length; j++) matrix[0, j] = j;
        
        for (int i = 1; i <= source.Length; i++)
        {
            for (int j = 1; j <= target.Length; j++)
            {
                int cost = target[j - 1] == source[i - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(Math.Min(
                    matrix[i - 1, j] + 1,
                    matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }
        
        return matrix[source.Length, target.Length];
    }
    
    private void LoadKnowledgeBase()
    {
        try
        {
            if (File.Exists(_knowledgeBasePath))
            {
                var json = File.ReadAllText(_knowledgeBasePath);
                var entries = JsonSerializer.Deserialize<List<KnowledgeBaseEntry>>(json);
                if (entries != null)
                {
                    _knowledgeBase.AddRange(entries);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not load knowledge base: {ex.Message}");
        }
    }
    
    private void SaveKnowledgeBase()
    {
        try
        {
            var json = JsonSerializer.Serialize(_knowledgeBase, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            File.WriteAllText(_knowledgeBasePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not save knowledge base: {ex.Message}");
        }
    }
}
