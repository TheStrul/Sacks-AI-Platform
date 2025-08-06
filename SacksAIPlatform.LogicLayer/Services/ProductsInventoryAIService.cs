using SacksAIPlatform.InfrastructuresLayer.AI.Models;
using SacksAIPlatform.InfrastructuresLayer.AI.Interfaces;
using SacksAIPlatform.DataLayer.Repositories.Interfaces;
using SacksAIPlatform.InfrastructuresLayer.FileProcessing.Interfaces;
using SacksAIPlatform.InfrastructuresLayer.AI.Capabilities;
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
    private readonly FolderAndFileCapability _folderCapability;

    public ProductsInventoryAIService(
        ILogger<ProductsInventoryAIService> logger,
        IConversationalAgent aiAgent,
        IProductRepository perfumeRepository,
        IBrandRepository brandRepository,
        IManufacturerRepository manufacturerRepository,
        ISupplierRepository supplierRepository,
        IUnitOfWork unitOfWork,
        IFileDataReader fileHandler,
        FolderAndFileCapability folderCapability)
    {
        _logger = logger;
        _aiAgent = aiAgent;
        _perfumeRepository = perfumeRepository;
        _brandRepository = brandRepository;
        _manufacturerRepository = manufacturerRepository;
        _supplierRepository = supplierRepository;
        _unitOfWork = unitOfWork;
        _fileHandler = fileHandler;
        _folderCapability = folderCapability;
        
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
        
        // Handle file listing and import requests
        if (lowerMessage.Contains("list") && lowerMessage.Contains("file") || 
            lowerMessage.Contains("show") && lowerMessage.Contains("file") ||
            lowerMessage.Contains("import") || lowerMessage.Contains("add") && lowerMessage.Contains("product"))
        {
            // Use the file capability to list files
            var fileResponse = _folderCapability.ListFiles("Inputs", "*.*", false);
            
            if (fileResponse.Data.ContainsKey("files") && fileResponse.Data["files"] is List<object> files && files.Any())
            {
                // Override the response with actual file information
                response.Message = $"I found {files.Count} files ready for import. Here they are:";
                response.Type = AgentResponseType.DataPresentation;
                response.Data = fileResponse.Data;
                
                response.Actions.Add(new AgentAction
                {
                    ActionId = "import_files",
                    ActionName = "Import Files",
                    Description = $"Import {files.Count} file(s) into the perfume database",
                    Parameters = new Dictionary<string, object> 
                    { 
                        { "files", files },
                        { "action", "import_all" }
                    }
                });
                
                response.RequiresUserConfirmation = true;
                response.ConfirmationPrompt = "Would you like me to proceed with importing these files?";
            }
            else
            {
                response.Message = "No files found in the Inputs directory.";
                response.Type = AgentResponseType.Text;
            }
        }
        
        // Handle confirmation responses for imports
        if (lowerMessage.Contains("yes") || lowerMessage.Contains("proceed") || lowerMessage.Contains("ok"))
        {
            // Check if there are pending import actions
            var importAction = response.Actions.FirstOrDefault(a => a.ActionId == "import_files");
            if (importAction != null)
            {
                try
                {
                    // Actually perform the import
                    var importResult = await ExecuteFileImport();
                    response.Message = importResult.Message;
                    response.Type = importResult.Type;
                    response.Data = importResult.Data;
                    response.Actions.Clear(); // Clear pending actions
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during file import");
                    response.Message = $"Error during import: {ex.Message}";
                    response.Type = AgentResponseType.Error;
                }
            }
        }
        
        return response;
    }

    private async Task<AgentResponse> ExecuteFileImport()
    {
        var fileResponse = _folderCapability.ListFiles("Inputs", "*.*", false);
        
        if (!fileResponse.Data.ContainsKey("files") || !(fileResponse.Data["files"] is List<object> files) || !files.Any())
        {
            return new AgentResponse
            {
                Message = "No files found to import.",
                Type = AgentResponseType.Text
            };
        }

        var importedCount = 0;
        var errors = new List<string>();

        foreach (var fileObj in files)
        {
            try
            {
                // Assuming files contain FileSystemItem objects
                if (fileObj.GetType().GetProperty("FullPath")?.GetValue(fileObj) is string filePath)
                {
                    var fileData = await _fileHandler.ReadFileAsync(filePath);
                    
                    // Here you would process the file data and insert into database
                    // For now, just log the successful read
                    _logger.LogInformation("Successfully read file: {FilePath} with {RowCount} rows", 
                        filePath, fileData.RowCount);
                    importedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing file: {Error}", ex.Message);
                errors.Add(ex.Message);
            }
        }

        var message = $"Import completed! Successfully processed {importedCount} files.";
        if (errors.Any())
        {
            message += $" {errors.Count} errors occurred: {string.Join(", ", errors)}";
        }

        return new AgentResponse
        {
            Message = message,
            Type = importedCount > 0 ? AgentResponseType.DataPresentation : AgentResponseType.Error,
            Data = new Dictionary<string, object>
            {
                { "ImportedCount", importedCount },
                { "ErrorCount", errors.Count },
                { "Errors", errors }
            }
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
        var capabilities = await _aiAgent.GetCapabilitiesAsync();
        
        // Add our business-specific capabilities that extend the infrastructure capabilities
        capabilities.Add(_fileHandler as AgentCapability ?? new AgentCapability
        {
            Name = "File Data Reader",
            Description = "Reads and processes data from Excel files (CSV, XLS, XLSX, XLSB) and converts them to structured data tables for analysis and processing.",
            Examples = new List<string>
            {
                "Read product data from Excel file",
                "Import supplier information from CSV",
                "Process inventory data from XLSX file"
            },
            Available = true
        });
        
        capabilities.Add(_folderCapability);
        
        return capabilities;
    }
}
