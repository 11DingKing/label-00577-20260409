using StockAnalyzer.Core.Enums;

namespace StockAnalyzer.Core.Models;

/// <summary>
/// 股票分析结果实体
/// </summary>
public class AnalysisResult
{
    public int Id { get; set; }
    
    /// <summary>
    /// 关联的股票ID
    /// </summary>
    public int StockId { get; set; }
    
    /// <summary>
    /// 分析日期
    /// </summary>
    public DateOnly AnalysisDate { get; set; }
    
    /// <summary>
    /// 投资建议
    /// </summary>
    public Recommendation Recommendation { get; set; }
    
    /// <summary>
    /// 置信度（0-100）
    /// </summary>
    public decimal Confidence { get; set; }
    
    /// <summary>
    /// 分析理由
    /// </summary>
    public string Reasoning { get; set; } = string.Empty;
    
    /// <summary>
    /// AI 原始响应（用于调试）
    /// </summary>
    public string? RawAiResponse { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 关联的股票
    /// </summary>
    public virtual Stock Stock { get; set; } = null!;
}
