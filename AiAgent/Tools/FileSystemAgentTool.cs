using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using LangChain.Chains.StackableChains.Agents.Tools;
using AiAgent.Configuration;
using System.Text.Json;

namespace AiAgent.Tools;

public class FileSystemAgentTool : AgentTool
{
    private readonly ILogger<FileSystemAgentTool> _logger;
    private readonly FileSystemToolSettings _settings;

    public FileSystemAgentTool(ILogger<FileSystemAgentTool> logger, FileSystemToolSettings settings)
        : base("file_system", "Provides access to .NET System.IO Directory and Path operations. " +
               "Supported methods: GetFiles, GetDirectories, CreateDirectory, DeleteDirectory, " +
               "CombinePath, GetExtension, GetFileName, GetDirectoryName, Exists.")
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public override async Task<string> ToolTask(string input, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Executing file system operation: {Input}", input);
            
            // Parse the input as JSON to get operation and parameters
            var args = JsonSerializer.Deserialize<Dictionary<string, object>>(input);
            if (args == null)
            {
                return "Error: Invalid input format";
            }
            
            var result = await InvokeAsync(args);
            
            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing file system operation");
            return $"Error: {ex.Message}";
        }
    }

    public Task<object> InvokeAsync(IDictionary<string, object> args)
    {
        // Expect an "operation" key plus whatever parameters that op needs
        if (!args.TryGetValue("operation", out var opObj) || opObj is not string operation)
            throw new ArgumentException("Missing or invalid 'operation' parameter.");

        // Normalize operation name
        operation = operation.Trim().ToLowerInvariant();

        switch (operation)
        {
            case "getfiles":
                {
                    var path = Convert.ToString(args["path"]) ?? "";
                    var pattern = args.TryGetValue("searchPattern", out var p)
                        ? Convert.ToString(p) ?? "*"
                        : "*";
                    var option = args.TryGetValue("searchOption", out var so)
                        && Enum.TryParse(Convert.ToString(so), true, out SearchOption soEnum)
                            ? soEnum
                            : SearchOption.TopDirectoryOnly;
                    return Task.FromResult<object>(Directory.GetFiles(path, pattern, option));
                }

            case "getdirectories":
                {
                    var path = Convert.ToString(args["path"]) ?? "";
                    var pattern = args.TryGetValue("searchPattern", out var p)
                        ? Convert.ToString(p) ?? "*"
                        : "*";
                    var option = args.TryGetValue("searchOption", out var so)
                        && Enum.TryParse(Convert.ToString(so), true, out SearchOption soEnum)
                            ? soEnum
                            : SearchOption.TopDirectoryOnly;
                    return Task.FromResult<object>(Directory.GetDirectories(path, pattern, option));
                }

            case "createdirectory":
                {
                    var path = Convert.ToString(args["path"]) ?? "";
                    Directory.CreateDirectory(path);
                    return Task.FromResult<object>($"Directory '{path}' created.");
                }

            case "deletedirectory":
                {
                    var path = Convert.ToString(args["path"]) ?? "";
                    var recursive = args.TryGetValue("recursive", out var r)
                        && Convert.ToBoolean(r);
                    Directory.Delete(path, recursive);
                    return Task.FromResult<object>($"Directory '{path}' deleted.");
                }

            case "combinepath":
                {
                    var parts = args["paths"] as IEnumerable<object>;
                    var stringParts = new List<string>();
                    if (parts != null)
                    {
                        foreach (var o in parts) 
                        {
                            var part = Convert.ToString(o);
                            if (part != null)
                                stringParts.Add(part);
                        }
                    }
                    return Task.FromResult<object>(Path.Combine(stringParts.ToArray()));
                }

            case "getextension":
                {
                    var file = Convert.ToString(args["path"]) ?? "";
                    return Task.FromResult<object>(Path.GetExtension(file) ?? "");
                }

            case "getfilename":
                {
                    var file = Convert.ToString(args["path"]) ?? "";
                    return Task.FromResult<object>(Path.GetFileName(file) ?? "");
                }

            case "getdirectoryname":
                {
                    var file = Convert.ToString(args["path"]) ?? "";
                    return Task.FromResult<object>(Path.GetDirectoryName(file) ?? "");
                }

            case "exists":
                {
                    var path = Convert.ToString(args["path"]) ?? "";
                    // check both file and directory
                    return Task.FromResult<object>(File.Exists(path) || Directory.Exists(path));
                }

            default:
                throw new NotSupportedException($"Operation '{operation}' is not supported.");
        }
    }
}
