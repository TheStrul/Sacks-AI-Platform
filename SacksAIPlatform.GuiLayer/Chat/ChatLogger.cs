using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace SacksAIPlatform.GuiLayer.Chat;

/// <summary>
/// Custom logger wrapper that can filter out Info logs from chat display based on configuration
/// </summary>
public class ChatLogger
{
    private readonly ILogger _logger;
    private readonly bool _showInfoLogs;
    private readonly StringBuilder _logBuffer;

    public ChatLogger(ILogger logger, IConfiguration configuration)
    {
        _logger = logger;
        _showInfoLogs = configuration.GetValue<bool>("Chat:ShowInfoLogs", false);
        _logBuffer = new StringBuilder();
    }

    public void LogInformation(string message, params object[] args)
    {
        _logger.LogInformation(message, args);
        
        if (_showInfoLogs)
        {
            var formattedMessage = string.Format(message, args);
            _logBuffer.AppendLine($"ℹ️  Info: {formattedMessage}");
        }
    }

    public void LogError(Exception ex, string message, params object[] args)
    {
        _logger.LogError(ex, message, args);
        
        var formattedMessage = string.Format(message, args);
        _logBuffer.AppendLine($"❌ Error: {formattedMessage}");
    }

    public void LogWarning(string message, params object[] args)
    {
        _logger.LogWarning(message, args);
        
        var formattedMessage = string.Format(message, args);
        _logBuffer.AppendLine($"⚠️  Warning: {formattedMessage}");
    }

    public string GetAndClearLogBuffer()
    {
        var logs = _logBuffer.ToString();
        _logBuffer.Clear();
        return logs;
    }

    public bool HasLogs()
    {
        return _logBuffer.Length > 0;
    }
}
