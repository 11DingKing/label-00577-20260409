using System.ComponentModel.DataAnnotations;
using StockAnalyzer.Core.Enums;

namespace StockAnalyzer.Core.DTOs;

/// <summary>
/// 运行分析请求
/// </summary>
public record RunAnalysisRequest
{
    /// <summary>
    /// 指定要分析的股票代码列表，为空则分析全部活跃股票
    /// </summary>
    public List<string>? Symbols { get; init; }
    
    /// <summary>
    /// 是否强制重新分析今日已分析的股票
    /// </summary>
    public bool ForceRerun { get; init; } = false;
}

/// <summary>
/// 分析结果响应
/// </summary>
public record AnalysisResultResponse
{
    public int Id { get; init; }
    public string Symbol { get; init; } = string.Empty;
    public string StockName { get; init; } = string.Empty;
    public DateOnly AnalysisDate { get; init; }
    public string Recommendation { get; init; } = string.Empty;
    public decimal Confidence { get; init; }
    public string Reasoning { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// 分析结果列表响应
/// </summary>
public record AnalysisResultListResponse
{
    public List<AnalysisResultResponse> Results { get; init; } = new();
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

/// <summary>
/// 分析执行结果响应
/// </summary>
public record RunAnalysisResponse
{
    public int TotalStocks { get; init; }
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public int SkippedCount { get; init; }
    public long DurationMs { get; init; }
    public List<AnalysisResultResponse> Results { get; init; } = new();
    public List<AnalysisErrorDetail>? Errors { get; init; }
}

/// <summary>
/// 分析错误详情
/// </summary>
public record AnalysisErrorDetail
{
    public string Symbol { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
}

/// <summary>
/// 分析结果查询参数
/// </summary>
public record AnalysisQueryParams
{
    public string? Symbol { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public Recommendation? Recommendation { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// AI分析响应（内部使用）
/// </summary>
public record AiAnalysisResponse
{
    public Recommendation Recommendation { get; init; }
    public decimal Confidence { get; init; }
    public string Reasoning { get; init; } = string.Empty;
    public string RawResponse { get; init; } = string.Empty;
}
