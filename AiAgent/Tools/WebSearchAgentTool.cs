using LangChain.Chains.StackableChains.Agents.Tools;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AiAgent.Tools;

/// <summary>
/// Web search tool for LangChain agent using DuckDuckGo
/// </summary>
public class WebSearchAgentTool : AgentTool
{
    private readonly ILogger<WebSearchAgentTool> _logger;
    private readonly HttpClient _httpClient;

    public WebSearchAgentTool(ILogger<WebSearchAgentTool> logger) 
        : base("web_search", "Search the web using DuckDuckGo. Input should be the search query string.")
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
    }

    public override async Task<string> ToolTask(string input, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Performing web search: {Query}", input);

            var searchResults = await PerformWebSearchAsync(input, cancellationToken);
            
            if (searchResults.Count == 0)
            {
                return "No search results found.";
            }

            var formattedResults = searchResults.Take(5).Select((result, index) => 
                $"{index + 1}. {result.Title}\n   {result.Snippet}\n   URL: {result.Url}");

            return $"Search results for '{input}':\n\n" + string.Join("\n\n", formattedResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing web search");
            return $"Error performing web search: {ex.Message}";
        }
    }

    private async Task<List<SearchResult>> PerformWebSearchAsync(string query, CancellationToken cancellationToken)
    {
        try
        {
            // Use DuckDuckGo Instant Answer API
            var encodedQuery = Uri.EscapeDataString(query);
            var url = $"https://api.duckduckgo.com/?q={encodedQuery}&format=json&no_html=1&skip_disambig=1";

            var response = await _httpClient.GetStringAsync(url, cancellationToken);
            var duckDuckGoResponse = JsonSerializer.Deserialize<DuckDuckGoResponse>(response);

            var results = new List<SearchResult>();

            // Add abstract if available
            if (!string.IsNullOrEmpty(duckDuckGoResponse?.Abstract))
            {
                results.Add(new SearchResult
                {
                    Title = duckDuckGoResponse.AbstractSource ?? "DuckDuckGo",
                    Snippet = duckDuckGoResponse.Abstract,
                    Url = duckDuckGoResponse.AbstractURL ?? ""
                });
            }

            // Add definition if available
            if (!string.IsNullOrEmpty(duckDuckGoResponse?.Definition))
            {
                results.Add(new SearchResult
                {
                    Title = duckDuckGoResponse.DefinitionSource ?? "Definition",
                    Snippet = duckDuckGoResponse.Definition,
                    Url = duckDuckGoResponse.DefinitionURL ?? ""
                });
            }

            // Add instant answer if available
            if (!string.IsNullOrEmpty(duckDuckGoResponse?.Answer))
            {
                results.Add(new SearchResult
                {
                    Title = "Instant Answer",
                    Snippet = duckDuckGoResponse.Answer,
                    Url = ""
                });
            }

            // Add related topics
            if (duckDuckGoResponse?.RelatedTopics != null)
            {
                foreach (var topic in duckDuckGoResponse.RelatedTopics.Take(3))
                {
                    if (!string.IsNullOrEmpty(topic.Text))
                    {
                        results.Add(new SearchResult
                        {
                            Title = "Related Topic",
                            Snippet = topic.Text,
                            Url = topic.FirstURL ?? ""
                        });
                    }
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DuckDuckGo API call");
            return new List<SearchResult>();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _httpClient?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private class SearchResult
    {
        public string Title { get; set; } = string.Empty;
        public string Snippet { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    private class DuckDuckGoResponse
    {
        public string? Abstract { get; set; }
        public string? AbstractText { get; set; }
        public string? AbstractSource { get; set; }
        public string? AbstractURL { get; set; }
        public string? Answer { get; set; }
        public string? AnswerType { get; set; }
        public string? Definition { get; set; }
        public string? DefinitionSource { get; set; }
        public string? DefinitionURL { get; set; }
        public RelatedTopic[]? RelatedTopics { get; set; }
    }

    private class RelatedTopic
    {
        public string? FirstURL { get; set; }
        public string? Result { get; set; }
        public string? Text { get; set; }
    }
}
