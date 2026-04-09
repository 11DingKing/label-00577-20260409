using Microsoft.AspNetCore.Mvc;
using StockAnalyzer.Core.DTOs;
using StockAnalyzer.Core.Enums;
using StockAnalyzer.Core.Services;

namespace StockAnalyzer.Api.Controllers;

/// <summary>
/// AI 分析控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AnalysisController : ControllerBase
{
    private readonly IAnalysisService _analysisService;
    private readonly ILogger<AnalysisController> _logger;

    public AnalysisController(IAnalysisService analysisService, ILogger<AnalysisController> logger)
    {
        _analysisService = analysisService;
        _logger = logger;
    }

    /// <summary>
    /// 运行 AI 分析（分析全部或指定股票）
    /// </summary>
    [HttpPost("run")]
    public async Task<ActionResult<ApiResponse<RunAnalysisResponse>>> RunAnalysis(
        [FromBody] RunAnalysisRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new RunAnalysisRequest();
        
        _logger.LogInformation("开始执行 AI 分析, 指定股票: {Symbols}, 强制重新分析: {ForceRerun}",
            request.Symbols != null ? string.Join(",", request.Symbols) : "全部",
            request.ForceRerun);

        var result = await _analysisService.RunAnalysisAsync(request, cancellationToken);
        
        return Ok(ApiResponse<RunAnalysisResponse>.Ok(result, 
            $"分析完成: 成功 {result.SuccessCount}, 失败 {result.FailureCount}, 跳过 {result.SkippedCount}"));
    }

    /// <summary>
    /// 分析单个股票
    /// </summary>
    /// <param name="symbol">股票代码</param>
    /// <param name="cancellationToken">取消令牌</param>
    [HttpPost("run/{symbol}")]
    public async Task<ActionResult<ApiResponse<AnalysisResultResponse>>> RunSingleAnalysis(
        string symbol,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始单股分析: {Symbol}", symbol);
        
        var result = await _analysisService.RunSingleAnalysisAsync(symbol, cancellationToken);
        if (result == null)
        {
            return NotFound(new ErrorResponse
            {
                Code = "STOCK_NOT_FOUND",
                Message = $"股票 {symbol.ToUpperInvariant()} 不存在"
            });
        }

        return Ok(ApiResponse<AnalysisResultResponse>.Ok(result, "分析完成"));
    }

    /// <summary>
    /// 获取分析结果（支持分页和筛选）
    /// </summary>
    [HttpGet("results")]
    public async Task<ActionResult<ApiResponse<AnalysisResultListResponse>>> GetResults(
        [FromQuery] string? symbol,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] Recommendation? recommendation,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var queryParams = new AnalysisQueryParams
        {
            Symbol = symbol,
            StartDate = startDate,
            EndDate = endDate,
            Recommendation = recommendation,
            Page = page,
            PageSize = Math.Min(pageSize, 100)
        };

        var result = await _analysisService.GetResultsAsync(queryParams);
        return Ok(ApiResponse<AnalysisResultListResponse>.Ok(result));
    }

    /// <summary>
    /// 获取指定股票的分析历史
    /// </summary>
    /// <param name="symbol">股票代码</param>
    /// <param name="limit">返回记录数限制</param>
    [HttpGet("results/{symbol}")]
    public async Task<ActionResult<ApiResponse<List<AnalysisResultResponse>>>> GetResultsBySymbol(
        string symbol,
        [FromQuery] int limit = 30)
    {
        var results = await _analysisService.GetResultsBySymbolAsync(symbol, limit);
        return Ok(ApiResponse<List<AnalysisResultResponse>>.Ok(results));
    }

    /// <summary>
    /// 获取最新分析结果
    /// </summary>
    /// <param name="count">返回记录数</param>
    [HttpGet("latest")]
    public async Task<ActionResult<ApiResponse<List<AnalysisResultResponse>>>> GetLatest([FromQuery] int count = 50)
    {
        var results = await _analysisService.GetLatestResultsAsync(Math.Min(count, 100));
        return Ok(ApiResponse<List<AnalysisResultResponse>>.Ok(results));
    }
}
