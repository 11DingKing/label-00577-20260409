namespace StockAnalyzer.Infrastructure.External;

/// <summary>
/// AI 服务配置
/// </summary>
public class AiSettings
{
    public const string SectionName = "AiSettings";
    
    /// <summary>
    /// AI 提供商 (OpenAI, Azure, Mock)
    /// </summary>
    public string Provider { get; set; } = "Mock";
    
    /// <summary>
    /// API Key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// API 基础 URL
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    
    /// <summary>
    /// 模型名称
    /// </summary>
    public string Model { get; set; } = "gpt-4";
    
    /// <summary>
    /// 最大 Token 数
    /// </summary>
    public int MaxTokens { get; set; } = 500;
    
    /// <summary>
    /// Temperature 参数
    /// </summary>
    public double Temperature { get; set; } = 0.3;
    
    /// <summary>
    /// 超时时间（秒）
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// 重试次数
    /// </summary>
    public int RetryCount { get; set; } = 3;
}
