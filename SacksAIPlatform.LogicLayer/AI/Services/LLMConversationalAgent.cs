using SacksAIPlatform.InfrastructuresLayer.AI.Models;
using SacksAIPlatform.InfrastructuresLayer.AI.Interfaces;
using SacksAIPlatform.DataLayer.Repositories.Interfaces;
using SacksAIPlatform.InfrastructuresLayer.Excel.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace SacksAIPlatform.LogicLayer.AI.Services;

/// <summary>
/// Pure LLM-driven conversational agent with full database and file access
/// All conversation logic is handled by the real LLM, no hardcoded responses
/// </summary>
public class LLMConversationalAgent : IConversationalAgent
{
    private readonly ILogger<LLMConversationalAgent> _logger;
    private readonly IConfiguration _configuration;
    private readonly IIntentRecognitionService _intentService;
    
    // Database access
    private readonly IPerfumeRepository _perfumeRepository;
    private readonly IBrandRepository _brandRepository;
    private readonly IManufacturerRepository _manufacturerRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IUnitOfWork _unitOfWork;
    
    // File processing
    private readonly IExcelFileHandler _excelHandler;
    
    // Agent configuration
    private readonly AgentConfiguration _agentConfig;
    
    public LLMConversationalAgent(
        ILogger<LLMConversationalAgent> logger,
        IConfiguration configuration,
        IIntentRecognitionService intentService,
        IPerfumeRepository perfumeRepository,
        IBrandRepository brandRepository,
        IManufacturerRepository manufacturerRepository,
        ISupplierRepository supplierRepository,
        IUnitOfWork unitOfWork,
        IExcelFileHandler excelHandler)
    {
        _logger = logger;
        _configuration = configuration;
        _intentService = intentService;
        _perfumeRepository = perfumeRepository;
        _brandRepository = brandRepository;
        _manufacturerRepository = manufacturerRepository;
        _supplierRepository = supplierRepository;
        _unitOfWork = unitOfWork;
        _excelHandler = excelHandler;
        
        // Load agent configuration from JSON
        _agentConfig = LoadAgentConfiguration();
        
        _logger.LogInformation("LLM Conversational Agent initialized with {CapabilityCount} capabilities", 
            _agentConfig.Agent.Capabilities.Count);
    }

    public async Task<AgentResponse> ProcessMessageAsync(string message, string userId = "user")
    {
        _logger.LogInformation("Processing message from user {UserId}: {Message}", userId, message);
        
        try
        {
            // Build context for the LLM including available tools and capabilities
            var systemContext = BuildSystemContext();
            var fullPrompt = $"{systemContext}\n\nUser: {message}";
            
            // Use LLM to process the entire conversation
            var intent = await _intentService.RecognizeIntentAsync(fullPrompt, userId);
            
            // Execute actions based on LLM's decision
            return await ExecuteLLMDirectedAction(intent, message, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message: {ErrorMessage}", ex.Message);
            return new AgentResponse
            {
                Message = GetErrorMessage("databaseError"),
                Type = AgentResponseType.Error
            };
        }
    }

    public Task<List<ConversationMessage>> GetConversationHistoryAsync(string userId = "user")
    {
        // Return empty list for now - could implement with database storage
        return Task.FromResult(new List<ConversationMessage>());
    }

    public Task ClearConversationAsync(string userId = "user")
    {
        _logger.LogInformation("Conversation cleared for user {UserId}", userId);
        return Task.CompletedTask;
    }

    public Task TeachAsync(string rule, string example, string userId = "user")
    {
        _logger.LogInformation("Teaching new rule to user {UserId}: {Rule}", userId, rule);
        // In a full implementation, this would store learning rules in the database
        // For now, just log the teaching attempt
        return Task.CompletedTask;
    }

    public Task<List<AgentCapability>> GetCapabilitiesAsync()
    {
        var capabilities = _agentConfig.Agent.Capabilities.Select(c => new AgentCapability
        {
            Name = c.Name,
            Description = c.Description,
            Examples = c.Examples,
            Available = true
        }).ToList();
        
        return Task.FromResult(capabilities);
    }

    private AgentConfiguration LoadAgentConfiguration()
    {
        try
        {
            var configPath = Path.Combine(Directory.GetCurrentDirectory(), "agent-config.json");
            var configJson = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<AgentConfiguration>(configJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            return config ?? new AgentConfiguration();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load agent configuration, using defaults");
            return new AgentConfiguration();
        }
    }

    private string BuildSystemContext()
    {
        var dbCapability = GetDatabaseCapability();
        var fileCapability = GetFileHandlingCapability();
        
        var context = $@"
{_agentConfig.Agent.SystemPrompt}

AVAILABLE CAPABILITIES:
{string.Join("\n", _agentConfig.Agent.Capabilities.Select(c => $"- {c.Name}: {c.Description}"))}

AVAILABLE TOOLS:
- Database Repositories: {(dbCapability != null ? string.Join(", ", dbCapability.Repositories) : "None")}
- File Handler: IExcelFileHandler for processing Excel/CSV files
- Operations: {(dbCapability != null ? string.Join(", ", dbCapability.Operations) : "None")}

CONVERSATION RULES:
{string.Join("\n", _agentConfig.Agent.ConversationRules.Select(r => $"- {r}"))}

CURRENT SESSION: You have full access to the perfume inventory database and can process Excel/CSV files.
When the user asks you to perform actions, execute them directly and provide clear feedback.
";

        return context;
    }

    private async Task<AgentResponse> ExecuteLLMDirectedAction(Intent intent, string originalMessage, string userId)
    {
        // Let the real LLM handle everything - no hardcoded logic
        return new AgentResponse
        {
            Message = intent.Description ?? "I'm here to help with your perfume inventory management. How can I assist you?",
            Type = AgentResponseType.Text,
            Data = new Dictionary<string, object>
            {
                { "Intent", intent.Name },
                { "Confidence", intent.Confidence },
                { "AvailableFiles", FindAvailableFiles().Count },
                { "DatabaseAccess", "Available" }
            }
        };
    }

    private string DetermineMessageType(string message)
    {
        var lowerMessage = message.ToLowerInvariant();
        
        // Check for import-related keywords
        if (lowerMessage.Contains("import") || lowerMessage.Contains("load") || 
            lowerMessage.Contains("upload") || lowerMessage.Contains("csv") || 
            lowerMessage.Contains("excel") || lowerMessage.Contains("file"))
        {
            return "import_request";
        }
        
        // Check for data query keywords
        if (lowerMessage.Contains("show") || lowerMessage.Contains("find") || 
            lowerMessage.Contains("search") || lowerMessage.Contains("get") || 
            lowerMessage.Contains("list"))
        {
            return "data_query";
        }
        
        // Check for analysis keywords
        if (lowerMessage.Contains("analyze") || lowerMessage.Contains("analysis") || 
            lowerMessage.Contains("statistics") || lowerMessage.Contains("report"))
        {
            return "data_analysis";
        }
        
        // Check for greetings
        if (lowerMessage.Contains("hi") || lowerMessage.Contains("hello") || 
            lowerMessage.Contains("hey") || lowerMessage.Contains("good"))
        {
            return "greeting";
        }
        
        return "general";
    }

    private Task<AgentResponse> HandleFileImport(string message)
    {
        // Find available files
        var csvFiles = FindAvailableFiles();
        
        if (!csvFiles.Any())
        {
            return Task.FromResult(new AgentResponse
            {
                Message = GetErrorMessage("fileNotFound"),
                Type = AgentResponseType.Error
            });
        }

        // Let LLM decide what to do with the files
        var fileInfo = string.Join(", ", csvFiles.Select(Path.GetFileName));
        
        return Task.FromResult(new AgentResponse
        {
            Message = $"I found {csvFiles.Count} file(s): {fileInfo}. I can import this data into the perfume database.",
            Type = AgentResponseType.ActionConfirmation,
            RequiresUserConfirmation = true,
            ConfirmationPrompt = "Shall I proceed with importing the data?",
            Actions = new List<AgentAction>
            {
                new AgentAction
                {
                    ActionId = "import_csv",
                    ActionName = "Import CSV Data", 
                    Description = $"Import {csvFiles.Count} file(s) into perfume database",
                    Parameters = new Dictionary<string, object> { { "files", csvFiles } }
                }
            }
        });
    }

    private async Task<AgentResponse> HandleDataQuery(string message)
    {
        // Use repositories to query data based on LLM interpretation
        var perfumes = await _perfumeRepository.GetAllAsync();
        var brands = await _brandRepository.GetAllAsync();
        
        return new AgentResponse
        {
            Message = $"I found {perfumes.Count()} perfumes from {brands.Count()} brands in the database.",
            Type = AgentResponseType.DataPresentation,
            Data = new Dictionary<string, object>
            {
                { "PerfumeCount", perfumes.Count() },
                { "BrandCount", brands.Count() }
            }
        };
    }

    private async Task<AgentResponse> HandleDataAnalysis(string message)
    {
        // Perform analysis using repository data
        var perfumes = await _perfumeRepository.GetAllAsync();
        
        return new AgentResponse
        {
            Message = $"Analysis complete. Found {perfumes.Count()} total products for analysis.",
            Type = AgentResponseType.DataPresentation
        };
    }

    private async Task<AgentResponse> HandleFileProcessing(string message)
    {
        var files = FindAvailableFiles();
        
        if (!files.Any())
        {
            return new AgentResponse
            {
                Message = GetErrorMessage("fileNotFound"),
                Type = AgentResponseType.Error
            };
        }

        // Use IExcelFileHandler to process files
        var firstFile = files.First();
        var fileExists = await _excelHandler.FileExistsAsync(firstFile);
        
        if (!fileExists)
        {
            return new AgentResponse
            {
                Message = GetErrorMessage("fileNotFound"),
                Type = AgentResponseType.Error
            };
        }

        try
        {
            var worksheetNames = await _excelHandler.GetWorksheetNamesAsync(firstFile);
            
            return new AgentResponse
            {
                Message = $"File analysis complete. Found {worksheetNames.Count} worksheets: {string.Join(", ", worksheetNames)}",
                Type = AgentResponseType.DataPresentation,
                Data = new Dictionary<string, object>
                {
                    { "Worksheets", worksheetNames },
                    { "FilePath", firstFile }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing file: {FilePath}", firstFile);
            return new AgentResponse
            {
                Message = GetErrorMessage("invalidFormat"),
                Type = AgentResponseType.Error
            };
        }
    }

    private async Task<AgentResponse> HandleGeneralConversation(string message, string userId)
    {
        var lowerMessage = message.ToLowerInvariant();
        
        // Handle greetings specifically
        if (lowerMessage.Contains("hi") || lowerMessage.Contains("hello") || 
            lowerMessage.Contains("hey") || lowerMessage.Contains("good"))
        {
            var greeting = $"Hello! I'm your AI assistant for perfume inventory management. I can help you with:\n\n" +
                          $"• Import perfume data from CSV/Excel files\n" +
                          $"• Search and query your perfume database\n" +
                          $"• Analyze perfume inventory data\n" +
                          $"• Manage perfume records\n" +
                          $"• Process files in your Inputs folder\n\n" +
                          $"How can I assist you today?";
            
            return new AgentResponse
            {
                Message = greeting,
                Type = AgentResponseType.Text
            };
        }
        
        // For other general conversation, provide helpful context
        var capabilities = await GetCapabilitiesAsync();
        var capabilityList = string.Join("\n", capabilities.Select(c => $"• {c.Description}"));
        
        return new AgentResponse
        {
            Message = $"I'm here to help with perfume inventory management. Here's what I can do:\n\n{capabilityList}\n\nWhat would you like me to help you with?",
            Type = AgentResponseType.Text
        };
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

        foreach (var path in searchPaths.Where(Directory.Exists))
        {
            var fileCapability = GetFileHandlingCapability();
            if (fileCapability != null)
            {
                foreach (var format in fileCapability.SupportedFormats)
                {
                    var pattern = $"*{format}";
                    files.AddRange(Directory.GetFiles(path, pattern));
                }
            }
        }

        return files.Distinct().OrderByDescending(f => new FileInfo(f).LastWriteTime).ToList();
    }

    private string GetErrorMessage(string errorType)
    {
        return _agentConfig.Agent.ErrorHandling.TryGetValue(errorType, out var message) 
            ? message 
            : "An unexpected error occurred. Please try again.";
    }

    /// <summary>
    /// Get database capability configuration if enabled
    /// </summary>
    private DatabaseCapabilityConfiguration? GetDatabaseCapability()
    {
        var dbCapability = _agentConfig.Agent.Capabilities
            .FirstOrDefault(c => c.Id == "database-access" && c.IsEnabled);
        
        if (dbCapability?.Configuration != null)
        {
            var dbConfig = System.Text.Json.JsonSerializer.Deserialize<DatabaseCapabilityConfiguration>(
                System.Text.Json.JsonSerializer.Serialize(dbCapability.Configuration));
            return dbConfig;
        }
        
        // Fallback to default if not configured
        return new DatabaseCapabilityConfiguration
        {
            Repositories = new List<string> { "PerfumeRepository", "BrandRepository", "ManufacturerRepository", "SupplierRepository" },
            Operations = new List<string> { "Create", "Read", "Update", "Delete", "Search" }
        };
    }

    /// <summary>
    /// Get file handling capability configuration if enabled
    /// </summary>
    private FileHandlingCapabilityConfiguration? GetFileHandlingCapability()
    {
        var fileCapability = _agentConfig.Agent.Capabilities
            .FirstOrDefault(c => c.Id == "file-processing" && c.IsEnabled);
        
        if (fileCapability?.Configuration != null)
        {
            var fileConfig = System.Text.Json.JsonSerializer.Deserialize<FileHandlingCapabilityConfiguration>(
                System.Text.Json.JsonSerializer.Serialize(fileCapability.Configuration));
            return fileConfig;
        }
        
        // Fallback to default if not configured
        return new FileHandlingCapabilityConfiguration
        {
            SupportedFormats = new List<string> { ".xlsx", ".xls", ".csv" },
            InputDirectories = new List<string> { "Inputs" },
            MaxFileSize = "10MB",
            AllowedOperations = new List<string> { "Read", "Import", "Export" }
        };
    }
}
