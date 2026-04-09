using Microsoft.AspNetCore.Mvc;
using StockAnalyzer.Core.DTOs;
using StockAnalyzer.Core.Enums;
using StockAnalyzer.Core.Services;

namespace StockAnalyzer.Api.Controllers;

/// <summary>
/// 统计分析控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;
    private readonly ILogger<StatisticsController> _logger;

    public StatisticsController(IStatisticsService statisticsService, ILogger<StatisticsController> logger)
    {
        _statisticsService = statisticsService;
        _logger = logger;
    }

    /// <summary>
    /// 查询连续 N 天相同建议的股票
    /// </summary>
    /// <param name="days">连续天数（默认3天）</param>
    /// <param name="recommendation">建议类型（Buy=1, Hold=2, Sell=3）</param>
    /// <param name="endDate">截止日期（默认今天）</param>
    [HttpGet("consecutive")]
    public async Task<ActionResult<ApiResponse<ConsecutiveQueryResponse>>> GetConsecutive(
        [FromQuery] int days = 3,
        [FromQuery] Recommendation recommendation = Recommendation.Buy,
        [FromQuery] DateOnly? endDate = null)
    {
        if (days < 2 || days > 30)
        {
            return BadRequest(new ErrorResponse
            {
                Code = "INVALID_DAYS",
                Message = "连续天数需在 2-30 之间"
            });
        }

        var request = new ConsecutiveQueryRequest
        {
            Days = days,
            Recommendation = recommendation,
            EndDate = endDate
        };

        _logger.LogInformation("查询连续建议: {Days} 天 {Recommendation}", days, recommendation);
        
        var result = await _statisticsService.GetConsecutiveStocksAsync(request);
        return Ok(ApiResponse<ConsecutiveQueryResponse>.Ok(result, 
            $"找到 {result.TotalFound} 只符合条件的股票"));
    }

    /// <summary>
    /// 获取统计汇总
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<StatisticsSummaryResponse>>> GetSummary()
    {
        var result = await _statisticsService.GetSummaryAsync();
        return Ok(ApiResponse<StatisticsSummaryResponse>.Ok(result));
    }

    /// <summary>
    /// 获取单个股票的趋势分析
    /// </summary>
    /// <param name="symbol">股票代码</param>
    /// <param name="days">分析天数（默认30天）</param>
    [HttpGet("trend/{symbol}")]
    public async Task<ActionResult<ApiResponse<StockTrendResponse>>> GetTrend(
        string symbol,
        [FromQuery] int days = 30)
    {
        if (days < 1 || days > 365)
        {
            return BadRequest(new ErrorResponse
            {
                Code = "INVALID_DAYS",
                Message = "分析天数需在 1-365 之间"
            });
        }

        var result = await _statisticsService.GetStockTrendAsync(symbol, days);
        if (result == null)
        {
            return NotFound(new ErrorResponse
            {
                Code = "STOCK_NOT_FOUND",
                Message = $"股票 {symbol.ToUpperInvariant()} 不存在"
            });
        }

        return Ok(ApiResponse<StockTrendResponse>.Ok(result));
    }
}
