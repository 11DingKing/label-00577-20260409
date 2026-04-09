using StockAnalyzer.Core.Enums;

namespace StockAnalyzer.Core.DTOs;

/// <summary>
/// 连续天数查询请求
/// </summary>
public record ConsecutiveQueryRequest
{
    /// <summary>
    /// 连续天数
    /// </summary>
    public int Days { get; init; } = 3;
    
    /// <summary>
    /// 建议类型
    /// </summary>
    public Recommendation Recommendation { get; init; } = Recommendation.Buy;
    
    /// <summary>
    /// 截止日期，默认为今天
    /// </summary>
    public DateOnly? EndDate { get; init; }
}

/// <summary>
/// 连续建议股票响应
/// </summary>
public record ConsecutiveStockResponse
{
    public string Symbol { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int ConsecutiveDays { get; init; }
    public Recommendation Recommendation { get; init; }
    public decimal AverageConfidence { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public List<AnalysisResultResponse> RecentAnalysis { get; init; } = new();
}

/// <summary>
/// 连续建议查询结果
/// </summary>
public record ConsecutiveQueryResponse
{
    public int Days { get; init; }
    public string Recommendation { get; init; } = string.Empty;
    public List<ConsecutiveStockResponse> Stocks { get; init; } = new();
    public int TotalFound { get; init; }
}

/// <summary>
/// 统计汇总响应
/// </summary>
public record StatisticsSummaryResponse
{
    public int TotalStocks { get; init; }
    public int TotalAnalysis { get; init; }
    public DateOnly? FirstAnalysisDate { get; init; }
    public DateOnly? LastAnalysisDate { get; init; }
    public RecommendationSummary BuySummary { get; init; } = new();
    public RecommendationSummary HoldSummary { get; init; } = new();
    public RecommendationSummary SellSummary { get; init; } = new();
}

/// <summary>
/// 建议类型统计
/// </summary>
public record RecommendationSummary
{
    public int Count { get; init; }
    public decimal Percentage { get; init; }
    public decimal AverageConfidence { get; init; }
}

/// <summary>
/// 股票趋势响应
/// </summary>
public record StockTrendResponse
{
    public string Symbol { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public List<TrendDataPoint> TrendData { get; init; } = new();
    public TrendSummary Summary { get; init; } = new();
}

/// <summary>
/// 趋势数据点
/// </summary>
public record TrendDataPoint
{
    public DateOnly Date { get; init; }
    public Recommendation Recommendation { get; init; }
    public decimal Confidence { get; init; }
}

/// <summary>
/// 趋势汇总
/// </summary>
public record TrendSummary
{
    public int TotalDays { get; init; }
    public int BuyDays { get; init; }
    public int HoldDays { get; init; }
    public int SellDays { get; init; }
    public string DominantRecommendation { get; init; } = string.Empty;
    public decimal AverageConfidence { get; init; }
}
