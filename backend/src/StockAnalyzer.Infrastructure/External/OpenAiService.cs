using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockAnalyzer.Core.DTOs;
using StockAnalyzer.Core.Enums;
using StockAnalyzer.Core.Interfaces;
using StockAnalyzer.Core.Models;

namespace StockAnalyzer.Infrastructure.External;

/// <summary>
/// OpenAI 服务实现
/// </summary>
public class OpenAiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly AiSettings _settings;
    private readonly ILogger<OpenAiService> _logger;

    public OpenAiService(
        HttpClient httpClient,
        IOptions<AiSettings> settings,
        ILogger<OpenAiService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<AiAnalysisResponse> AnalyzeStockAsync(Stock stock, CancellationToken cancellationToken = default)
    {
        var prompt = BuildPrompt(stock);
        
        _logger.LogDebug("开始分析股票 {Symbol}, Prompt 长度: {Length}", stock.Symbol, prompt.Length);

        var request = new OpenAiRequest
        {
            Model = _settings.Model,
            Messages = new[]
            {
                new OpenAiMessage { Role = "system", Content = "You are a professional stock analyst. Always respond with valid JSON." },
                new OpenAiMessage { Role = "user", Content = prompt }
            },
            MaxTokens = _settings.MaxTokens,
            Temperature = _settings.Temperature
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("chat/completions", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OpenAiResponse>(cancellationToken: cancellationToken);
            var content = result?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;

            _logger.LogDebug("AI 响应: {Content}", content);

            return ParseAiResponse(content);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "调用 OpenAI API 失败: {Symbol}", stock.Symbol);
            throw new InvalidOperationException($"AI 服务调用失败: {ex.Message}", ex);
        }
    }

    public async Task<Dictionary<string, AiAnalysisResponse>> AnalyzeStocksAsync(
        IEnumerable<Stock> stocks,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, AiAnalysisResponse>();
        
        foreach (var stock in stocks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                var response = await AnalyzeStockAsync(stock, cancellationToken);
                results[stock.Symbol] = response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分析股票 {Symbol} 失败", stock.Symbol);
            }
        }

        return results;
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("models");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private string BuildPrompt(Stock stock)
    {
        return $@"Analyze the stock {stock.Symbol} ({stock.Name}) and provide an investment recommendation for the next month.

Consider factors like:
- Current market trends
- Company fundamentals
- Technical indicators
- Recent news and events

Respond ONLY with a JSON object in this exact format:
{{
  ""recommendation"": ""Buy"" or ""Hold"" or ""Sell"",
  ""confidence"": a number between 0 and 100,
  ""reasoning"": ""Your analysis in 2-3 sentences""
}}";
    }

    private AiAnalysisResponse ParseAiResponse(string content)
    {
        try
        {
            // 尝试提取 JSON 部分
            var jsonStart = content.IndexOf('{');
            var jsonEnd = content.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = content.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var parsed = JsonSerializer.Deserialize<AiJsonResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (parsed != null)
                {
                    var recommendation = parsed.Recommendation?.ToLowerInvariant() switch
                    {
                        "buy" => Recommendation.Buy,
                        "hold" => Recommendation.Hold,
                        "sell" => Recommendation.Sell,
                        _ => Recommendation.Hold
                    };

                    return new AiAnalysisResponse
                    {
                        Recommendation = recommendation,
                        Confidence = Math.Clamp(parsed.Confidence, 0, 100),
                        Reasoning = parsed.Reasoning ?? "No reasoning provided",
                        RawResponse = content
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解析 AI 响应失败，使用默认值");
        }

        // 解析失败时返回默认值
        return new AiAnalysisResponse
        {
            Recommendation = Recommendation.Hold,
            Confidence = 50,
            Reasoning = "Unable to parse AI response, defaulting to Hold",
            RawResponse = content
        };
    }

    private class AiJsonResponse
    {
        public string? Recommendation { get; set; }
        public decimal Confidence { get; set; }
        public string? Reasoning { get; set; }
    }

    private class OpenAiRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;
        
        [JsonPropertyName("messages")]
        public OpenAiMessage[] Messages { get; set; } = Array.Empty<OpenAiMessage>();
        
        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }
        
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }
    }

    private class OpenAiMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;
        
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private class OpenAiResponse
    {
        [JsonPropertyName("choices")]
        public OpenAiChoice[]? Choices { get; set; }
    }

    private class OpenAiChoice
    {
        [JsonPropertyName("message")]
        public OpenAiMessage? Message { get; set; }
    }
}
