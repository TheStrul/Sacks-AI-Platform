using LangChain.Chains.StackableChains.Agents.Tools;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using AiAgent.Configuration;

namespace AiAgent.Tools;

/// <summary>
/// File system operations tool for LangChain agent
/// </summary>
public class FileSystemAgentTool : AgentTool
{
    private readonly ILogger<FileSystemAgentTool> _logger;
    private readonly FileSystemToolSettings _settings;

    public FileSystemAgentTool(ILogger<FileSystemAgentTool> logger, FileSystemToolSettings settings) 
        : base("file_system", "Read, write, list, delete files and directories. Navigate directories and manage directory operations. Input should be JSON with 'operation' and parameters.")
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public override async Task<string> ToolTask(string input, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Executing file system operation: {Input}", input);

            // Create JSON options that handle escaped characters properly
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            };

            FileOperation? operation;
            try
            {
                operation = JsonSerializer.Deserialize<FileOperation>(input, jsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning("JSON deserialization failed, attempting to fix path escaping: {Error}", ex.Message);
                
                // Try to fix common JSON escaping issues with Windows paths
                var fixedInput = input.Replace(@"\U", @"\\U")
                                      .Replace(@"\u", @"\\u")
                                      .Replace("C:\\", "C:\\\\")
                                      .Replace("\\", "\\\\");
                
                try
                {
                    operation = JsonSerializer.Deserialize<FileOperation>(fixedInput, jsonOptions);
                    _logger.LogInformation("Successfully parsed JSON after fixing path escaping");
                }
                catch (JsonException)
                {
                    return $"Error: Invalid JSON input. Please ensure paths are properly escaped. Original error: {ex.Message}";
                }
            }

            if (operation == null)
            {
                return "Error: Invalid JSON input. Expected format: {\"operation\":\"list|read|write|delete|create_directory|change_directory|get_current_directory|rename_directory\", \"path\":\"...\", \"content\":\"...\"}";
            }

            return operation.Operation?.ToLower() switch
            {
                "list" => await ListFilesAsync(operation.Path ?? "."),
                "read" => await ReadFileAsync(operation.Path ?? throw new ArgumentException("Path required for read operation")),
                "write" => await WriteFileAsync(operation.Path ?? throw new ArgumentException("Path required for write operation"), 
                                              operation.Content ?? throw new ArgumentException("Content required for write operation")),
                "delete" => await DeleteFileAsync(operation.Path ?? throw new ArgumentException("Path required for delete operation")),
                "create_directory" => await CreateDirectoryAsync(operation.Path ?? throw new ArgumentException("Path required for create_directory operation")),
                "change_directory" => await ChangeDirectoryAsync(operation.Path ?? throw new ArgumentException("Path required for change_directory operation")),
                "get_current_directory" => await GetCurrentDirectoryAsync(),
                "rename_directory" => await RenameDirectoryAsync(operation.Path ?? throw new ArgumentException("Path required for rename_directory operation"),
                                                                operation.Content ?? throw new ArgumentException("New name required for rename_directory operation")),
                _ => "Error: Unknown operation. Supported operations: list, read, write, delete, create_directory, change_directory, get_current_directory, rename_directory"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing file system operation");
            return $"Error: {ex.Message}";
        }
    }

    private async Task<string> ListFilesAsync(string path)
    {
        try
        {
            // Check if directory exists
            if (!Directory.Exists(path))
            {
                return $"Error: Directory '{path}' does not exist";
            }

            // Validate path restrictions if configured
            if (_settings.AllowedRootDirectories.Count > 0)
            {
                var fullPath = Path.GetFullPath(path);
                var isAllowed = _settings.AllowedRootDirectories.Any(allowedRoot => 
                    fullPath.StartsWith(Path.GetFullPath(allowedRoot), StringComparison.OrdinalIgnoreCase));
                
                if (!isAllowed)
                {
                    return $"Error: Access to directory '{path}' is not allowed";
                }
            }

            var files = Directory.GetFiles(path).Take(_settings.MaxFilesToList);
            var directories = Directory.GetDirectories(path).Take(_settings.MaxDirectoriesToList);

            var result = new
            {
                Path = path,
                Files = files.Select(Path.GetFileName).ToArray(),
                Directories = directories.Select(Path.GetFileName).ToArray()
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return $"Error listing files: {ex.Message}";
        }
    }

    private async Task<string> ReadFileAsync(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return $"Error: File '{path}' does not exist";
            }

            // Check blocked extensions
            var extension = Path.GetExtension(path);
            if (_settings.BlockedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                return $"Error: Reading files with extension '{extension}' is not allowed";
            }

            // Validate path restrictions if configured
            if (_settings.AllowedRootDirectories.Count > 0)
            {
                var fullPath = Path.GetFullPath(path);
                var isAllowed = _settings.AllowedRootDirectories.Any(allowedRoot => 
                    fullPath.StartsWith(Path.GetFullPath(allowedRoot), StringComparison.OrdinalIgnoreCase));
                
                if (!isAllowed)
                {
                    return $"Error: Access to file '{path}' is not allowed";
                }
            }

            var content = await File.ReadAllTextAsync(path);
            
            // Limit content size to prevent overwhelming the model
            if (content.Length > _settings.MaxFileContentSize)
            {
                content = content.Substring(0, _settings.MaxFileContentSize) + "... (truncated)";
            }

            return $"File content of '{path}':\n{content}";
        }
        catch (Exception ex)
        {
            return $"Error reading file: {ex.Message}";
        }
    }

    private async Task<string> WriteFileAsync(string path, string content)
    {
        try
        {
            // Check blocked extensions
            var extension = Path.GetExtension(path);
            if (_settings.BlockedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                return $"Error: Writing files with extension '{extension}' is not allowed";
            }

            // Validate path restrictions if configured
            if (_settings.AllowedRootDirectories.Count > 0)
            {
                var fullPath = Path.GetFullPath(path);
                var isAllowed = _settings.AllowedRootDirectories.Any(allowedRoot => 
                    fullPath.StartsWith(Path.GetFullPath(allowedRoot), StringComparison.OrdinalIgnoreCase));
                
                if (!isAllowed)
                {
                    return $"Error: Access to path '{path}' is not allowed";
                }
            }

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(path, content);
            return $"Successfully wrote content to '{path}'";
        }
        catch (Exception ex)
        {
            return $"Error writing file: {ex.Message}";
        }
    }

    private async Task<string> DeleteFileAsync(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                return $"Successfully deleted file '{path}'";
            }
            else if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
                return $"Successfully deleted directory '{path}'";
            }
            else
            {
                return $"Error: Path '{path}' does not exist";
            }
        }
        catch (Exception ex)
        {
            return $"Error deleting: {ex.Message}";
        }
    }

    private async Task<string> CreateDirectoryAsync(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                return $"Directory '{path}' already exists";
            }

            Directory.CreateDirectory(path);
            return $"Successfully created directory '{path}'";
        }
        catch (Exception ex)
        {
            return $"Error creating directory: {ex.Message}";
        }
    }

    private async Task<string> ChangeDirectoryAsync(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                return $"Error: Directory '{path}' does not exist";
            }

            var fullPath = Path.GetFullPath(path);
            Directory.SetCurrentDirectory(fullPath);
            return $"Successfully changed directory to '{fullPath}'";
        }
        catch (Exception ex)
        {
            return $"Error changing directory: {ex.Message}";
        }
    }

    private async Task<string> GetCurrentDirectoryAsync()
    {
        try
        {
            var currentDir = Directory.GetCurrentDirectory();
            return $"Current directory: '{currentDir}'";
        }
        catch (Exception ex)
        {
            return $"Error getting current directory: {ex.Message}";
        }
    }

    private async Task<string> RenameDirectoryAsync(string oldPath, string newName)
    {
        try
        {
            if (!Directory.Exists(oldPath))
            {
                return $"Error: Directory '{oldPath}' does not exist";
            }

            var parentDir = Path.GetDirectoryName(oldPath);
            if (string.IsNullOrEmpty(parentDir))
            {
                return "Error: Cannot rename root directory";
            }

            var newPath = Path.Combine(parentDir, newName);
            
            if (Directory.Exists(newPath))
            {
                return $"Error: Directory '{newPath}' already exists";
            }

            Directory.Move(oldPath, newPath);
            return $"Successfully renamed directory from '{oldPath}' to '{newPath}'";
        }
        catch (Exception ex)
        {
            return $"Error renaming directory: {ex.Message}";
        }
    }

    private class FileOperation
    {
        public string? Operation { get; set; }
        public string? Path { get; set; }
        public string? Content { get; set; }
    }
}
