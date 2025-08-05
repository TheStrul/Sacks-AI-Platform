using SacksAIPlatform.InfrastructuresLayer.AI.Models;
using SacksAIPlatform.InfrastructuresLayer.AI.Interfaces;
using SacksAIPlatform.DataLayer.Repositories.Interfaces;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing.Interfaces;
using Microsoft.Extensions.Logging;

namespace SacksAIPlatform.LogicLayer.Services;

/// <summary>
/// Business-specific AI agent that extends the generic infrastructure AI
/// Adds perfume inventory management capabilities
/// </summary>
public class ProductsInventoryAIService
{
    private readonly ILogger<ProductsInventoryAIService> _logger;
    private readonly IConversationalAgent _aiAgent;
    
    // Business-specific dependencies
    private readonly IProductRepository _perfumeRepository;
    private readonly IBrandRepository _brandRepository;
    private readonly IManufacturerRepository _manufacturerRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileDataReader _fileHandler;

    public ProductsInventoryAIService(
        ILogger<ProductsInventoryAIService> logger,
        IConversationalAgent aiAgent,
        IProductRepository perfumeRepository,
        IBrandRepository brandRepository,
        IManufacturerRepository manufacturerRepository,
        ISupplierRepository supplierRepository,
        IUnitOfWork unitOfWork,
        IFileDataReader fileHandler)
    {
        _logger = logger;
        _aiAgent = aiAgent;
        _perfumeRepository = perfumeRepository;
        _brandRepository = brandRepository;
        _manufacturerRepository = manufacturerRepository;
        _supplierRepository = supplierRepository;
        _unitOfWork = unitOfWork;
        _fileHandler = fileHandler;
        
        _logger.LogInformation("Perfume Inventory AI Service initialized with business capabilities");
    }

    public async Task<AgentResponse> ProcessMessageAsync(string message, string userId = "user")
    {
        _logger.LogInformation("Processing business-specific message: {Message}", message);
        
        try
        {
            // Add business context to the message
            var businessContext = await BuildBusinessContext();
            var enhancedMessage = $"{businessContext}\n\nUser: {message}";
            
            // Use the generic AI agent for conversation
            var response = await _aiAgent.ProcessMessageAsync(enhancedMessage, userId);
            
            // Post-process response to add business actions if needed
            return await EnhanceResponseWithBusinessActions(response, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in business AI processing: {ErrorMessage}", ex.Message);
            return new AgentResponse
            {
                Message = "I'm sorry, I encountered an error while processing your request. Please try again.",
                Type = AgentResponseType.Error
            };
        }
    }

    private async Task<string> BuildBusinessContext()
    {
        var perfumeCount = (await _perfumeRepository.GetAllAsync()).Count();
        var brandCount = (await _brandRepository.GetAllAsync()).Count();
        var availableFiles = FindAvailableFiles();
        
        return $@"
BUSINESS CONTEXT: Perfume Inventory Management System
- Current Database: {perfumeCount} perfumes, {brandCount} brands
- Available Files: {availableFiles.Count} files ready for import
- Capabilities: Database operations, Excel/CSV processing, data analysis
";
    }

    private async Task<AgentResponse> EnhanceResponseWithBusinessActions(AgentResponse response, string originalMessage)
    {
        var lowerMessage = originalMessage.ToLowerInvariant();
        
        // Add import actions if user mentions files
        if (lowerMessage.Contains("import") || lowerMessage.Contains("file") || lowerMessage.Contains("csv"))
        {
            var files = FindAvailableFiles();
            if (files.Any())
            {
                response.Actions.Add(new AgentAction
                {
                    ActionId = "import_files",
                    ActionName = "Import Available Files",
                    Description = $"Import {files.Count} file(s) into perfume database",
                    Parameters = new Dictionary<string, object> { { "files", files } }
                });
                response.RequiresUserConfirmation = true;
                response.ConfirmationPrompt = "Shall I proceed with importing the available files?";
            }
        }
        
        // Add query actions if user asks for data
        if (lowerMessage.Contains("show") || lowerMessage.Contains("list") || lowerMessage.Contains("find"))
        {
            response.Actions.Add(new AgentAction
            {
                ActionId = "query_data",
                ActionName = "Query Database",
                Description = "Search perfume database for requested information",
                Parameters = new Dictionary<string, object> { { "query", originalMessage } }
            });
        }
        
        return response;
    }

    private List<string> FindAvailableFiles()
    {
        var files = new List<string>();
        var currentDir = Environment.CurrentDirectory;
        var projectRoot = Path.GetDirectoryName(currentDir) ?? currentDir;
        
        var searchPaths = new[]
        {
            Path.Combine(projectRoot, "Inputs"),
            Path.Combine(currentDir, "Inputs")
        };

        var supportedFormats = new[] { "*.csv", "*.xlsx", "*.xls" };
        
        foreach (var path in searchPaths.Where(Directory.Exists))
        {
            foreach (var format in supportedFormats)
            {
                files.AddRange(Directory.GetFiles(path, format));
            }
        }

        return files.Distinct().OrderByDescending(f => new FileInfo(f).LastWriteTime).ToList();
    }

    public async Task<List<AgentCapability>> GetCapabilitiesAsync()
    {
        return await _aiAgent.GetCapabilitiesAsync();
    }
}
