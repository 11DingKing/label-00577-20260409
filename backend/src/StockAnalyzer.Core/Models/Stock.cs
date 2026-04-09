namespace StockAnalyzer.Core.Models;

/// <summary>
/// 股票实体
/// </summary>
public class Stock
{
    public int Id { get; set; }
    
    /// <summary>
    /// 股票代码（如 AAPL, GOOGL）
    /// </summary>
    public string Symbol { get; set; } = string.Empty;
    
    /// <summary>
    /// 股票名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否激活（在观察列表中）
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// 分析结果集合
    /// </summary>
    public virtual ICollection<AnalysisResult> AnalysisResults { get; set; } = new List<AnalysisResult>();
}
