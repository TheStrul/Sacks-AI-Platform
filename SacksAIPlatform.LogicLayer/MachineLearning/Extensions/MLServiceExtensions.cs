using Microsoft.Extensions.DependencyInjection;
using SacksAIPlatform.LogicLayer.MachineLearning.Services;
using SacksAIPlatform.LogicLayer.MachineLearning.Pipeline;

namespace SacksAIPlatform.LogicLayer.MachineLearning.Extensions;

/// <summary>
/// Dependency injection configuration for ML services
/// Registers all ML-related services with proper lifetimes
/// </summary>
public static class MLServiceExtensions
{
    /// <summary>
    /// Add all ML services to the dependency injection container
    /// </summary>
    public static IServiceCollection AddMLServices(this IServiceCollection services)
    {
        // Core ML services
        services.AddSingleton<FeatureEngineeringService>();
        services.AddSingleton<KnowledgeBaseService>();
        services.AddSingleton<TrainingDataService>();
        services.AddScoped<MLPredictionService>();
        
        // Pipeline orchestrator - note: dependencies like IBrandRepository should be registered separately
        services.AddScoped<ProductMLPipeline>();
        
        return services;
    }
}
