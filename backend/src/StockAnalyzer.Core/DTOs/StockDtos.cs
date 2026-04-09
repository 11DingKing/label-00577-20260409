using System.ComponentModel.DataAnnotations;

namespace StockAnalyzer.Core.DTOs;

/// <summary>
/// 添加股票请求
/// </summary>
public record AddStockRequest
{
    /// <summary>
    /// 股票代码
    /// </summary>
    [Required(ErrorMessage = "股票代码不能为空")]
    [StringLength(20, MinimumLength = 1, ErrorMessage = "股票代码长度需在1-20之间")]
    public string Symbol { get; init; } = string.Empty;
    
    /// <summary>
    /// 股票名称
    /// </summary>
    [Required(ErrorMessage = "股票名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "股票名称长度需在1-100之间")]
    public string Name { get; init; } = string.Empty;
}

/// <summary>
/// 批量添加股票请求
/// </summary>
public record BatchAddStocksRequest
{
    [Required(ErrorMessage = "股票列表不能为空")]
    [MinLength(1, ErrorMessage = "至少需要一只股票")]
    public List<AddStockRequest> Stocks { get; init; } = new();
}

/// <summary>
/// 更新股票请求
/// </summary>
public record UpdateStockRequest
{
    [StringLength(100, ErrorMessage = "股票名称长度不能超过100")]
    public string? Name { get; init; }
    
    public bool? IsActive { get; init; }
}

/// <summary>
/// 股票响应
/// </summary>
public record StockResponse
{
    public int Id { get; init; }
    public string Symbol { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public int AnalysisCount { get; init; }
    public AnalysisResultResponse? LatestAnalysis { get; init; }
}

/// <summary>
/// 股票列表响应
/// </summary>
public record StockListResponse
{
    public List<StockResponse> Stocks { get; init; } = new();
    public int Total { get; init; }
}
