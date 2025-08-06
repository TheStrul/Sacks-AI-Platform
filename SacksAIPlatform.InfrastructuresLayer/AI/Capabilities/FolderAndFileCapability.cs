using SacksAIPlatform.InfrastructuresLayer.AI.Models;

namespace SacksAIPlatform.InfrastructuresLayer.AI.Capabilities;

/// <summary>
/// File and folder operations capability for AI agents
/// Provides functionality to list, search, and select files and folders
/// </summary>
public class FolderAndFileCapability : AgentCapability
{
    private readonly string _baseDirectory;
    private readonly List<string> _allowedExtensions;
    private readonly long _maxFileSizeBytes;

    public FolderAndFileCapability(string baseDirectory = "", List<string>? allowedExtensions = null, long maxFileSizeBytes = 10 * 1024 * 1024)
    {
        _baseDirectory = string.IsNullOrEmpty(baseDirectory) ? Directory.GetCurrentDirectory() : baseDirectory;
        _allowedExtensions = allowedExtensions ?? new List<string> { ".csv", ".xlsx", ".xls", ".txt", ".json" };
        _maxFileSizeBytes = maxFileSizeBytes;

        // Initialize AgentCapability properties
        Name = "Folder and File Operations";
        Description = "Provides file and folder operations including listing files, searching for files, getting file information, and managing directories. Supports various file formats with size and extension filtering.";
        Examples = new List<string>
        {
            "List all Excel files in the current directory",
            "Search for files containing 'product' in their name",
            "Get detailed information about a specific file",
            "List all subdirectories in a folder",
            "Find CSV files in the Inputs folder"
        };
        Available = true;
    }

    /// <summary>
    /// Creates an error response
    /// </summary>
    private static AgentResponse CreateErrorResponse(string message)
    {
        return new AgentResponse
        {
            Message = message,
            Type = AgentResponseType.Error,
            Data = new Dictionary<string, object> { ["error"] = message }
        };
    }

    /// <summary>
    /// Lists all files in the specified directory matching the allowed extensions
    /// </summary>
    public AgentResponse ListFiles(string? directory = null, string? pattern = "*", bool includeSubdirectories = false)
    {
        try
        {
            var targetDirectory = string.IsNullOrEmpty(directory) ? _baseDirectory : Path.Combine(_baseDirectory, directory);
            
            if (!Directory.Exists(targetDirectory))
            {
                return CreateErrorResponse($"Directory not found: {targetDirectory}");
            }

            var searchOption = includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var allFiles = Directory.GetFiles(targetDirectory, pattern ?? "*", searchOption);
            
            var filteredFiles = allFiles
                .Where(file => _allowedExtensions.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                .Select(file => new FileSystemItem(file))
                .Where(item => item.SizeBytes <= _maxFileSizeBytes)
                .OrderByDescending(item => item.LastModified)
                .ToList();

            var response = new AgentResponse
            {
                Message = $"Found {filteredFiles.Count} files in {targetDirectory}",
                Type = AgentResponseType.DataPresentation,
                Data = new Dictionary<string, object>
                {
                    ["files"] = filteredFiles,
                    ["count"] = filteredFiles.Count,
                    ["directory"] = targetDirectory
                }
            };

            if (filteredFiles.Any())
            {
                response.Actions.Add(new AgentAction
                {
                    ActionName = "list_files",
                    Description = $"Found {filteredFiles.Count} file(s) ready for processing",
                    Parameters = filteredFiles.ToDictionary(f => f.Name, f => (object)f.FullPath)
                });
            }

            return response;
        }
        catch (Exception ex)
        {
            return CreateErrorResponse($"Error listing files: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets detailed information about a specific file
    /// </summary>
    public AgentResponse GetFileInfo(string filePath)
    {
        try
        {
            var fullPath = Path.IsPathRooted(filePath) ? filePath : Path.Combine(_baseDirectory, filePath);
            
            if (!File.Exists(fullPath))
            {
                return CreateErrorResponse($"File not found: {filePath}");
            }

            var fileInfo = new FileSystemItem(fullPath);
            
            // Additional details for supported file types
            var additionalInfo = new Dictionary<string, object>();
            
            if (fileInfo.Extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var lines = File.ReadAllLines(fullPath);
                    additionalInfo["recordCount"] = Math.Max(0, lines.Length - 1); // -1 for header
                    additionalInfo["hasHeader"] = lines.Length > 0;
                    if (lines.Length > 0)
                    {
                        additionalInfo["columns"] = lines[0].Split(',').Length;
                    }
                }
                catch
                {
                    additionalInfo["recordCount"] = "Unable to determine";
                }
            }

            return new AgentResponse
            {
                Message = $"File information for {fileInfo.Name}",
                Type = AgentResponseType.DataPresentation,
                Data = new Dictionary<string, object>
                {
                    ["fileInfo"] = fileInfo,
                    ["additionalInfo"] = additionalInfo
                },
                Actions = new List<AgentAction>
                {
                    new AgentAction
                    {
                        ActionName = "file_info",
                        Description = $"File: {fileInfo.Name} ({fileInfo.SizeFormatted})",
                        Parameters = additionalInfo
                    }
                }
            };
        }
        catch (Exception ex)
        {
            return CreateErrorResponse($"Error getting file info: {ex.Message}");
        }
    }

    /// <summary>
    /// Searches for files matching specific criteria
    /// </summary>
    public AgentResponse SearchFiles(string searchTerm, string? directory = null, bool includeSubdirectories = true)
    {
        try
        {
            var targetDirectory = string.IsNullOrEmpty(directory) ? _baseDirectory : Path.Combine(_baseDirectory, directory);
            
            if (!Directory.Exists(targetDirectory))
            {
                return CreateErrorResponse($"Directory not found: {targetDirectory}");
            }

            var searchOption = includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var allFiles = Directory.GetFiles(targetDirectory, "*", searchOption);
            
            var matchingFiles = allFiles
                .Where(file => _allowedExtensions.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                .Where(file => Path.GetFileName(file).Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .Select(file => new FileSystemItem(file))
                .Where(item => item.SizeBytes <= _maxFileSizeBytes)
                .OrderByDescending(item => item.LastModified)
                .ToList();

            return new AgentResponse
            {
                Message = $"Found {matchingFiles.Count} files matching '{searchTerm}'",
                Type = AgentResponseType.DataPresentation,
                Data = new Dictionary<string, object>
                {
                    ["files"] = matchingFiles,
                    ["count"] = matchingFiles.Count,
                    ["searchTerm"] = searchTerm
                },
                Actions = matchingFiles.Any() ? new List<AgentAction>
                {
                    new AgentAction
                    {
                        ActionName = "search_results",
                        Description = $"Search results for '{searchTerm}'",
                        Parameters = matchingFiles.ToDictionary(f => f.Name, f => (object)f.FullPath)
                    }
                } : new List<AgentAction>()
            };
        }
        catch (Exception ex)
        {
            return CreateErrorResponse($"Error searching files: {ex.Message}");
        }
    }

    /// <summary>
    /// Lists directories in the specified path
    /// </summary>
    public AgentResponse ListDirectories(string? directory = null)
    {
        try
        {
            var targetDirectory = string.IsNullOrEmpty(directory) ? _baseDirectory : Path.Combine(_baseDirectory, directory);
            
            if (!Directory.Exists(targetDirectory))
            {
                return CreateErrorResponse($"Directory not found: {targetDirectory}");
            }

            var directories = Directory.GetDirectories(targetDirectory)
                .Select(dir => new DirectoryItem(dir))
                .OrderBy(dir => dir.Name)
                .ToList();

            return new AgentResponse
            {
                Message = $"Found {directories.Count} directories in {targetDirectory}",
                Type = AgentResponseType.DataPresentation,
                Data = new Dictionary<string, object>
                {
                    ["directories"] = directories,
                    ["count"] = directories.Count,
                    ["path"] = targetDirectory
                }
            };
        }
        catch (Exception ex)
        {
            return CreateErrorResponse($"Error listing directories: {ex.Message}");
        }
    }
}

/// <summary>
/// Represents a file system item with metadata
/// </summary>
public class FileSystemItem
{
    public string Name { get; set; }
    public string FullPath { get; set; }
    public string Extension { get; set; }
    public long SizeBytes { get; set; }
    public string SizeFormatted { get; set; }
    public DateTime LastModified { get; set; }
    public string RelativePath { get; set; }

    public FileSystemItem(string fullPath)
    {
        var fileInfo = new FileInfo(fullPath);
        
        Name = fileInfo.Name;
        FullPath = fileInfo.FullName;
        Extension = fileInfo.Extension;
        SizeBytes = fileInfo.Length;
        SizeFormatted = FormatFileSize(fileInfo.Length);
        LastModified = fileInfo.LastWriteTime;
        RelativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), fullPath);
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
        return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
    }
}

/// <summary>
/// Represents a directory item
/// </summary>
public class DirectoryItem
{
    public string Name { get; set; }
    public string FullPath { get; set; }
    public DateTime LastModified { get; set; }
    public int FileCount { get; set; }
    public string RelativePath { get; set; }

    public DirectoryItem(string fullPath)
    {
        var dirInfo = new DirectoryInfo(fullPath);
        
        Name = dirInfo.Name;
        FullPath = dirInfo.FullName;
        LastModified = dirInfo.LastWriteTime;
        RelativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), fullPath);
        
        try
        {
            FileCount = dirInfo.GetFiles().Length;
        }
        catch
        {
            FileCount = 0; // If no access
        }
    }
}
