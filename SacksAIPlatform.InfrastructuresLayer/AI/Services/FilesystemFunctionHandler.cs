#pragma warning disable OPENAI001 // OpenAI API is in preview and subject to change

using SacksAIPlatform.InfrastructuresLayer.AI.Models;
using System.Text.Json;
using OpenAI.Assistants;

namespace SacksAIPlatform.InfrastructuresLayer.AI.Services;

/// <summary>
/// Handles filesystem operations for the AI Assistant Function Calling
/// Provides static methods to execute filesystem functions and Tool definitions for OpenAI Assistant
/// </summary>
public static class FilesystemFunctionHandler
{
    private static readonly string _baseDirectory = Directory.GetCurrentDirectory();
    private static readonly List<string> _allowedExtensions = new() { ".csv", ".xlsx", ".xls", ".txt", ".json" };
    private static readonly long _maxFileSizeBytes = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// Get all filesystem function tools for OpenAI Assistant registration
    /// </summary>
    public static List<FunctionToolDefinition> GetFunctionTools()
    {
        return new List<FunctionToolDefinition>
        {
            CreateListFilesTool(),
            CreateListDirectoriesTool(),
            CreateGetFileInfoTool(),
            CreateSearchFilesTool()
        };
    }

    /// <summary>
    /// Execute a filesystem function based on the function name and arguments (async version)
    /// </summary>
    public static Task<string> ExecuteFunctionAsync(string functionName, string argumentsJson)
    {
        // For now, wrap the synchronous method in a Task
        // In the future, individual handlers could be made truly async if needed
        return Task.FromResult(ExecuteFunction(functionName, argumentsJson));
    }

    /// <summary>
    /// Execute a filesystem function based on the function name and arguments
    /// </summary>
    public static string ExecuteFunction(string functionName, string argumentsJson)
    {
        try
        {
            return functionName switch
            {
                "list_files" => HandleListFiles(argumentsJson),
                "list_directories" => HandleListDirectories(argumentsJson),
                "get_file_info" => HandleGetFileInfo(argumentsJson),
                "search_files" => HandleSearchFiles(argumentsJson),
                _ => JsonSerializer.Serialize(new { error = $"Unknown function: {functionName}" })
            };
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Error executing {functionName}: {ex.Message}" });
        }
    }

    #region Tool Definitions

    private static FunctionToolDefinition CreateListFilesTool()
    {
        var parameters = new
        {
            type = "object",
            properties = new
            {
                directory = new
                {
                    type = "string",
                    description = "The directory path to list files from. Leave empty for current directory."
                },
                pattern = new
                {
                    type = "string",
                    description = "File pattern to match (e.g., '*.xlsx', '*.csv'). Defaults to '*' for all files.",
                    @default = "*"
                },
                include_subdirectories = new
                {
                    type = "boolean",
                    description = "Whether to include files from subdirectories",
                    @default = false
                }
            },
            required = new string[] { }
        };

        return new FunctionToolDefinition
        {
            FunctionName = "list_files",
            Description = "List all files in a specified directory with optional filtering by file pattern. Supports CSV, Excel, text, and JSON files.",
            Parameters = BinaryData.FromObjectAsJson(parameters)
        };
    }

    private static FunctionToolDefinition CreateListDirectoriesTool()
    {
        var parameters = new
        {
            type = "object",
            properties = new
            {
                directory = new
                {
                    type = "string",
                    description = "The directory path to list subdirectories from. Leave empty for current directory."
                }
            },
            required = new string[] { }
        };

        return new FunctionToolDefinition
        {
            FunctionName = "list_directories",
            Description = "List all subdirectories in a specified directory with metadata information.",
            Parameters = BinaryData.FromObjectAsJson(parameters)
        };
    }

    private static FunctionToolDefinition CreateGetFileInfoTool()
    {
        var parameters = new
        {
            type = "object",
            properties = new
            {
                file_path = new
                {
                    type = "string",
                    description = "The path to the file to get information about (relative or absolute path)"
                }
            },
            required = new string[] { "file_path" }
        };

        return new FunctionToolDefinition
        {
            FunctionName = "get_file_info",
            Description = "Get detailed information about a specific file including size, modification date, and metadata. For CSV files, also provides row count and column information.",
            Parameters = BinaryData.FromObjectAsJson(parameters)
        };
    }

    private static FunctionToolDefinition CreateSearchFilesTool()
    {
        var parameters = new
        {
            type = "object",
            properties = new
            {
                search_term = new
                {
                    type = "string",
                    description = "The search term to look for in file names"
                },
                directory = new
                {
                    type = "string",
                    description = "The directory to search in. Leave empty for current directory."
                },
                include_subdirectories = new
                {
                    type = "boolean",
                    description = "Whether to search recursively in subdirectories",
                    @default = true
                }
            },
            required = new string[] { "search_term" }
        };

        return new FunctionToolDefinition
        {
            FunctionName = "search_files",
            Description = "Search for files in a directory tree by name pattern. Searches through file names for the specified term.",
            Parameters = BinaryData.FromObjectAsJson(parameters)
        };
    }

    #endregion

    #region Function Handlers

    private static string HandleListFiles(string argumentsJson)
    {
        var args = JsonSerializer.Deserialize<ListFilesArgs>(argumentsJson) ?? new ListFilesArgs();
        
        var targetDirectory = string.IsNullOrEmpty(args.Directory) 
            ? _baseDirectory 
            : Path.Combine(_baseDirectory, args.Directory);

        if (!Directory.Exists(targetDirectory))
        {
            return JsonSerializer.Serialize(new { error = $"Directory not found: {targetDirectory}" });
        }

        var searchOption = args.IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var pattern = string.IsNullOrEmpty(args.Pattern) ? "*" : args.Pattern;
        
        var allFiles = Directory.GetFiles(targetDirectory, pattern, searchOption);
        
        var filteredFiles = allFiles
            .Where(file => _allowedExtensions.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            .Select(file => new FileSystemItem(file))
            .Where(item => item.SizeBytes <= _maxFileSizeBytes)
            .OrderByDescending(item => item.LastModified)
            .ToList();

        return JsonSerializer.Serialize(new 
        { 
            files = filteredFiles,
            count = filteredFiles.Count,
            directory = targetDirectory,
            pattern = pattern,
            success = true
        });
    }

    private static string HandleListDirectories(string argumentsJson)
    {
        var args = JsonSerializer.Deserialize<ListDirectoriesArgs>(argumentsJson) ?? new ListDirectoriesArgs();
        
        var targetDirectory = string.IsNullOrEmpty(args.Directory) 
            ? _baseDirectory 
            : Path.Combine(_baseDirectory, args.Directory);

        if (!Directory.Exists(targetDirectory))
        {
            return JsonSerializer.Serialize(new { error = $"Directory not found: {targetDirectory}" });
        }

        var directories = Directory.GetDirectories(targetDirectory)
            .Select(dir => new DirectoryItem(dir))
            .OrderBy(dir => dir.Name)
            .ToList();

        return JsonSerializer.Serialize(new 
        { 
            directories = directories,
            count = directories.Count,
            path = targetDirectory,
            success = true
        });
    }

    private static string HandleGetFileInfo(string argumentsJson)
    {
        var args = JsonSerializer.Deserialize<GetFileInfoArgs>(argumentsJson);
        if (args?.FilePath == null)
        {
            return JsonSerializer.Serialize(new { error = "file_path parameter is required" });
        }

        var fullPath = Path.IsPathRooted(args.FilePath) 
            ? args.FilePath 
            : Path.Combine(_baseDirectory, args.FilePath);

        if (!File.Exists(fullPath))
        {
            return JsonSerializer.Serialize(new { error = $"File not found: {args.FilePath}" });
        }

        var fileInfo = new FileSystemItem(fullPath);
        var additionalInfo = new Dictionary<string, object>();

        // Additional details for CSV files
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
                    additionalInfo["firstRowSample"] = lines[0];
                }
            }
            catch
            {
                additionalInfo["recordCount"] = "Unable to determine";
            }
        }

        return JsonSerializer.Serialize(new 
        { 
            fileInfo = fileInfo,
            additionalInfo = additionalInfo,
            success = true
        });
    }

    private static string HandleSearchFiles(string argumentsJson)
    {
        var args = JsonSerializer.Deserialize<SearchFilesArgs>(argumentsJson);
        if (args?.SearchTerm == null)
        {
            return JsonSerializer.Serialize(new { error = "search_term parameter is required" });
        }

        var targetDirectory = string.IsNullOrEmpty(args.Directory) 
            ? _baseDirectory 
            : Path.Combine(_baseDirectory, args.Directory);

        if (!Directory.Exists(targetDirectory))
        {
            return JsonSerializer.Serialize(new { error = $"Directory not found: {targetDirectory}" });
        }

        var searchOption = args.IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var allFiles = Directory.GetFiles(targetDirectory, "*", searchOption);

        var matchingFiles = allFiles
            .Where(file => _allowedExtensions.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            .Where(file => Path.GetFileName(file).Contains(args.SearchTerm, StringComparison.OrdinalIgnoreCase))
            .Select(file => new FileSystemItem(file))
            .Where(item => item.SizeBytes <= _maxFileSizeBytes)
            .OrderByDescending(item => item.LastModified)
            .ToList();

        return JsonSerializer.Serialize(new 
        { 
            files = matchingFiles,
            count = matchingFiles.Count,
            searchTerm = args.SearchTerm,
            directory = targetDirectory,
            success = true
        });
    }

    #endregion

    #region Argument Classes

    private class ListFilesArgs
    {
        public string? Directory { get; set; }
        public string? Pattern { get; set; }
        public bool IncludeSubdirectories { get; set; }
    }

    private class ListDirectoriesArgs
    {
        public string? Directory { get; set; }
    }

    private class GetFileInfoArgs
    {
        public string? FilePath { get; set; }
    }

    private class SearchFilesArgs
    {
        public string? SearchTerm { get; set; }
        public string? Directory { get; set; }
        public bool IncludeSubdirectories { get; set; } = true;
    }

    #endregion
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
