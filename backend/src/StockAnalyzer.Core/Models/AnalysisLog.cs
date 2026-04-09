namespace StockAnalyzer.Core.Models;

/// <summary>
/// 分析执行日志
/// </summary>
public class AnalysisLog
{
    public int Id { get; set; }
    
    /// <summary>
    /// 执行时间
    /// </summary>
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 分析的股票总数
    /// </summary>
    public int TotalStocks { get; set; }
    
    /// <summary>
    /// 成功数量
    /// </summary>
    public int SuccessCount { get; set; }
    
    /// <summary>
    /// 失败数量
    /// </summary>
    public int FailureCount { get; set; }
    
    /// <summary>
    /// 错误详情（JSON格式）
    /// </summary>
    public string? ErrorDetails { get; set; }
    
    /// <summary>
    /// 执行耗时（毫秒）
    /// </summary>
    public long DurationMs { get; set; }
}
